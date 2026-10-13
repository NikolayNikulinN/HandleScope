using System.ComponentModel;
using System.Diagnostics;
using HandleScope.Logger.Constants;
using HandleScope.Services.Abstract;
using HandleScope.Services.Constants;
using HandleScope.Services.Results;
using Serilog;

namespace HandleScope.Services.Process;

/// <summary>Requests elevation only when the current user cannot terminate a process.</summary>
public sealed class ElevatingProcessActionService : IProcessActionService
{
    private readonly SystemProcessActionService localActionService;
    private readonly IUiText uiText;
    private readonly ILogger logger;

    /// <summary>Initializes the process action service.</summary>
    public ElevatingProcessActionService(SystemProcessActionService localActionService, IUiText uiText, ILogger logger)
    {
        this.localActionService = localActionService;
        this.uiText = uiText;
        this.logger = logger.ForContext<ElevatingProcessActionService>();
    }

    /// <inheritdoc />
    public async Task<ProcessActionResult> CloseProcessAsync(int processId, CancellationToken cancellationToken)
    {
        var result = await localActionService.CloseProcessAsync(processId, cancellationToken);
        if (!result.RequiresElevation)
        {
            return result;
        }

        try
        {
            var processName = GetProcessName(processId);
            var executablePath = Path.Combine(AppContext.BaseDirectory, ServiceConstants.ApplicationExecutableName);
            var startInfo = new ProcessStartInfo(executablePath)
            {
                Arguments = $"{ServiceConstants.ProcessTerminationArgument} {processId}",
                UseShellExecute = true,
                Verb = ServiceConstants.ElevationVerb,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var elevatedProcess = System.Diagnostics.Process.Start(startInfo)
                ?? throw new InvalidOperationException(uiText.Get(UiTextKey.ElevationStartFailedMessage));
            await elevatedProcess.WaitForExitAsync(cancellationToken);

            if (elevatedProcess.ExitCode != ServiceConstants.ErrorSuccess)
            {
                return result;
            }

            return new ProcessActionResult(true, string.Format(uiText.Get(UiTextKey.ProcessTerminatedFormat), processName));
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ServiceConstants.ErrorCancelled)
        {
            return new ProcessActionResult(false, uiText.Get(UiTextKey.ElevationCancelledMessage));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.Error(ex, LogConstants.ProcessTerminationFailedLogTemplate, processId);
            return new ProcessActionResult(false, string.Format(uiText.Get(UiTextKey.ProcessTerminationFailedFormat), processId, ex.Message));
        }
    }

    private string GetProcessName(int processId)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch (Exception ex) when (ex is ArgumentException or Win32Exception or InvalidOperationException)
        {
            return string.Format(uiText.Get(UiTextKey.ProcessDetailsFallbackNameFormat), processId);
        }
    }
}
