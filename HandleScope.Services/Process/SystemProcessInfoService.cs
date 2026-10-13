using HandleScope.Logger.Constants;
using HandleScope.Services.Abstract;
using HandleScope.Services.Models;
using Serilog;

namespace HandleScope.Services.Process;

/// <summary>Reads details about running Windows processes.</summary>
public sealed class SystemProcessInfoService : IProcessInfoService
{
    private readonly ILogger logger;
    private readonly IUiText uiText;

    /// <summary>Initializes the process information service.</summary>
    public SystemProcessInfoService(ILogger logger, IUiText uiText)
    {
        this.logger = logger.ForContext<SystemProcessInfoService>();
        this.uiText = uiText;
    }

    /// <inheritdoc />
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
            logger.Warning(ex, LogConstants.ProcessDetailsResolveFailedLogTemplate, processId);
            return new ProcessDetails(processId, string.Format(uiText.Get(UiTextKey.ProcessDetailsFallbackNameFormat), processId), null, null);
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
            logger.Debug(ex, LogConstants.ExecutablePathResolveFailedLogTemplate, process.Id);
            return null;
        }
    }
}
