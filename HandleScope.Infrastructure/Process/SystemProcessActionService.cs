using HandleScope.Application.Services;
using HandleScope.Domain;
using HandleScope.Domain.Results;
using Serilog;

namespace HandleScope.Infrastructure.Process;

public sealed class SystemProcessActionService : IProcessActionService
{
    private readonly ILogger _logger;
    private readonly IUiText _uiText;

    public SystemProcessActionService(ILogger logger, IUiText uiText)
    {
        _logger = logger.ForContext<SystemProcessActionService>();
        _uiText = uiText;
    }

    public async Task<ProcessActionResult> CloseProcessAsync(int processId, CancellationToken cancellationToken)
    {
        if (processId == Environment.ProcessId)
        {
            return new ProcessActionResult(false, _uiText.Get(UiTextKey.CannotTerminateSelfMessage));
        }

        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);
            var processName = process.ProcessName;

            _logger.Warning(AppStrings.ProcessTerminationRequestedLogTemplate, processId, processName);

            if (process.HasExited)
            {
                return new ProcessActionResult(true, string.Format(_uiText.Get(UiTextKey.ProcessAlreadyExitedFormat), processName));
            }

            if (process.CloseMainWindow())
            {
                _logger.Information(AppStrings.ProcessCloseRequestedLogTemplate, processId, processName);

                if (await WaitForExitAsync(process, TimeSpan.FromSeconds(3), cancellationToken))
                {
                    return new ProcessActionResult(true, string.Format(_uiText.Get(UiTextKey.ProcessClosedFormat), processName));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.Warning(AppStrings.ProcessForceTerminatingLogTemplate, processId, processName);
            process.Kill(entireProcessTree: true);

            if (await WaitForExitAsync(process, TimeSpan.FromSeconds(5), cancellationToken))
            {
                return new ProcessActionResult(true, string.Format(_uiText.Get(UiTextKey.ProcessTerminatedFormat), processName));
            }

            return new ProcessActionResult(false, string.Format(_uiText.Get(UiTextKey.ProcessDidNotExitFormat), processName));
        }
        catch (ArgumentException)
        {
            return new ProcessActionResult(true, string.Format(_uiText.Get(UiTextKey.ProcessNoLongerRunningFormat), processId));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, AppStrings.ProcessTerminationFailedLogTemplate, processId);
            return new ProcessActionResult(false, string.Format(_uiText.Get(UiTextKey.ProcessTerminationFailedFormat), processId, ex.Message));
        }
    }

    private static async Task<bool> WaitForExitAsync(
        System.Diagnostics.Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
