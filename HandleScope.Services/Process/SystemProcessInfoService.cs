using HandleScope.Services.Constants;
using HandleScope.Logger.Constants;
using HandleScope.Services.Abstract;
using HandleScope.Services.Models;
using Microsoft.Win32.SafeHandles;
using Serilog;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

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
                TryGetUserName(process));
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

    private string? TryGetUserName(System.Diagnostics.Process process)
    {
        try
        {
            using var processHandle = OpenProcess(ServiceConstants.ProcessQueryLimitedInformationAccess, false, process.Id);
            if (processHandle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (!OpenProcessToken(processHandle, ServiceConstants.TokenQueryAccess, out var token))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            using (token)
            using (var identity = new WindowsIdentity(token.DangerousGetHandle()))
            {
                return identity.Name;
            }
        }
        catch (Exception ex)
        {
            logger.Debug(ex, LogConstants.ProcessUserResolveFailedLogTemplate, process.Id);
            return null;
        }
    }

    [DllImport(ServiceConstants.WindowsKernelDllName, SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport(ServiceConstants.WindowsSecurityDllName, SetLastError = true)]
    private static extern bool OpenProcessToken(SafeProcessHandle processHandle, uint desiredAccess, out SafeAccessTokenHandle tokenHandle);
}
