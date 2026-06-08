using HandleScope.Application.Services;
using HandleScope.Domain;
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
            _logger.Warning(ex, AppStrings.ProcessDetailsResolveFailedLogTemplate, processId);
            return new ProcessDetails(processId, string.Format(AppStrings.ProcessDetailsFallbackNameFormat, processId), null, null);
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
            _logger.Debug(ex, AppStrings.ExecutablePathResolveFailedLogTemplate, process.Id);
            return null;
        }
    }
}
