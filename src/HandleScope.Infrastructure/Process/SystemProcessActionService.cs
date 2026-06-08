using HandleScope.Application.Services;
using HandleScope.Domain;
using HandleScope.Domain.Results;
using Serilog;

namespace HandleScope.Infrastructure.Process;

public sealed class SystemProcessActionService : IProcessActionService
{
    private readonly ILogger _logger;

    public SystemProcessActionService(ILogger logger)
    {
        _logger = logger.ForContext<SystemProcessActionService>();
    }

    public async Task<ProcessActionResult> CloseProcessAsync(int processId, CancellationToken cancellationToken)
    {
        if (processId == Environment.ProcessId)
        {
            return new ProcessActionResult(false, AppStrings.CannotTerminateSelfMessage);
        }

        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);
            var processName = process.ProcessName;

            _logger.Warning(AppStrings.ProcessTerminationRequestedLogTemplate, processId, processName);

            if (process.HasExited)
            {
                return new ProcessActionResult(true, string.Format(AppStrings.ProcessAlreadyExitedFormat, processName));
            }

            if (process.CloseMainWindow())
            {
                _logger.Information(AppStrings.ProcessCloseRequestedLogTemplate, processId, processName);

                if (await WaitForExitAsync(process, TimeSpan.FromSeconds(3), cancellationToken))
                {
                    return new ProcessActionResult(true, string.Format(AppStrings.ProcessClosedFormat, processName));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.Warning(AppStrings.ProcessForceTerminatingLogTemplate, processId, processName);
            process.Kill(entireProcessTree: true);

            if (await WaitForExitAsync(process, TimeSpan.FromSeconds(5), cancellationToken))
            {
                return new ProcessActionResult(true, string.Format(AppStrings.ProcessTerminatedFormat, processName));
            }

            return new ProcessActionResult(false, string.Format(AppStrings.ProcessDidNotExitFormat, processName));
        }
        catch (ArgumentException)
        {
            return new ProcessActionResult(true, string.Format(AppStrings.ProcessNoLongerRunningFormat, processId));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, AppStrings.ProcessTerminationFailedLogTemplate, processId);
            return new ProcessActionResult(false, string.Format(AppStrings.ProcessTerminationFailedFormat, processId, ex.Message));
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
