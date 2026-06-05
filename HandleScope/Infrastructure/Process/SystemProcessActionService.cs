using HandleScope.Application.Services;
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
            return new ProcessActionResult(false, "HandleScope cannot terminate itself.");
        }

        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);
            var processName = process.ProcessName;

            _logger.Warning("Process termination requested for PID {ProcessId} ({ProcessName})", processId, processName);

            if (process.HasExited)
            {
                return new ProcessActionResult(true, $"Process {processName} has already exited.");
            }

            if (process.CloseMainWindow())
            {
                _logger.Information("Sent close request to PID {ProcessId} ({ProcessName})", processId, processName);

                if (await WaitForExitAsync(process, TimeSpan.FromSeconds(3), cancellationToken))
                {
                    return new ProcessActionResult(true, $"Process {processName} was closed.");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.Warning("Force terminating PID {ProcessId} ({ProcessName})", processId, processName);
            process.Kill(entireProcessTree: true);

            if (await WaitForExitAsync(process, TimeSpan.FromSeconds(5), cancellationToken))
            {
                return new ProcessActionResult(true, $"Process {processName} was terminated.");
            }

            return new ProcessActionResult(false, $"Process {processName} did not exit in time.");
        }
        catch (ArgumentException)
        {
            return new ProcessActionResult(true, $"PID {processId} is no longer running.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to terminate PID {ProcessId}", processId);
            return new ProcessActionResult(false, $"Failed to terminate PID {processId}: {ex.Message}");
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
