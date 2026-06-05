using HandleScope.Application.Services;
using HandleScope.Domain.Models;
using Serilog;

namespace HandleScope.Infrastructure.Process;

public sealed class SystemProcessInfoService : IProcessInfoService
{
    private readonly ILogger _logger;

    public SystemProcessInfoService(ILogger logger)
    {
        _logger = logger.ForContext<SystemProcessInfoService>();
    }

    public ProcessDetails GetProcessDetails(int processId)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);

            return new ProcessDetails(
                process.Id,
                process.ProcessName,
                TryGetExecutablePath(process),
                null);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to resolve process details for PID {ProcessId}", processId);
            return new ProcessDetails(processId, $"PID {processId}", null, null);
        }
    }

    private string? TryGetExecutablePath(System.Diagnostics.Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Failed to resolve executable path for PID {ProcessId}", process.Id);
            return null;
        }
    }
}
