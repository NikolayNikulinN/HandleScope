using HandleScope.Services.Abstract;
using HandleScope.Services.Constants;
using HandleScope.Logger.Constants;
using HandleScope.Services.Results;
using Serilog;
using System.ComponentModel;

namespace HandleScope.Services.Process;

/// <summary>Closes running Windows processes on request.</summary>
public sealed class SystemProcessActionService : IProcessActionService
{
    private readonly ILogger logger;
    private readonly IUiText uiText;

    /// <summary>Initializes the process action service.</summary>
    public SystemProcessActionService(ILogger logger, IUiText uiText)
    {
        this.logger = logger.ForContext<SystemProcessActionService>();
        this.uiText = uiText;
    }

    /// <inheritdoc />
    public async Task<ProcessActionResult> CloseProcessAsync(int processId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (processId == Environment.ProcessId)
        {
            return new ProcessActionResult(false, uiText.Get(UiTextKey.CannotTerminateSelfMessage));
        }

        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);
            var processName = process.ProcessName;

            logger.Warning(LogConstants.ProcessTerminationRequestedLogTemplate, processId, processName);

            if (process.HasExited)
            {
                return new ProcessActionResult(true, string.Format(uiText.Get(UiTextKey.ProcessAlreadyExitedFormat), processName));
            }

            if (process.CloseMainWindow())
            {
                logger.Information(LogConstants.ProcessCloseRequestedLogTemplate, processId, processName);

                if (await WaitForExitAsync(process, ServiceConstants.GracefulCloseTimeout, cancellationToken))
                {
                    return new ProcessActionResult(true, string.Format(uiText.Get(UiTextKey.ProcessClosedFormat), processName));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            logger.Warning(LogConstants.ProcessForceTerminatingLogTemplate, processId, processName);
            process.Kill(entireProcessTree: true);

            if (await WaitForExitAsync(process, ServiceConstants.ForcedTerminationTimeout, cancellationToken))
            {
                return new ProcessActionResult(true, string.Format(uiText.Get(UiTextKey.ProcessTerminatedFormat), processName));
            }

            return new ProcessActionResult(false, string.Format(uiText.Get(UiTextKey.ProcessDidNotExitFormat), processName));
        }
        catch (ArgumentException)
        {
            return new ProcessActionResult(true, string.Format(uiText.Get(UiTextKey.ProcessNoLongerRunningFormat), processId));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ServiceConstants.ErrorAccessDenied)
        {
            logger.Warning(ex, LogConstants.ProcessTerminationFailedLogTemplate, processId);
            return new ProcessActionResult(false, string.Format(uiText.Get(UiTextKey.ProcessTerminationFailedFormat), processId, ex.Message), true);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.Warning(ex, LogConstants.ProcessTerminationFailedLogTemplate, processId);
            return new ProcessActionResult(false, string.Format(uiText.Get(UiTextKey.ProcessTerminationFailedFormat), processId, ex.Message), true);
        }
        catch (Exception ex)
        {
            logger.Error(ex, LogConstants.ProcessTerminationFailedLogTemplate, processId);
            return new ProcessActionResult(false, string.Format(uiText.Get(UiTextKey.ProcessTerminationFailedFormat), processId, ex.Message));
        }
    }

    private static async Task<bool> WaitForExitAsync(
        System.Diagnostics.Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(timeoutCancellation.Token);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
