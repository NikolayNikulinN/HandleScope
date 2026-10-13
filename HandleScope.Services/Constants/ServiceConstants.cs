
namespace HandleScope.Services.Constants;

/// <summary>Defines limits and native values used by process services.</summary>
public static class ServiceConstants
{
    public const string RestartManagerDllName = "rstrtmgr.dll";
    public const string WindowsSecurityDllName = "advapi32.dll";
    public const string WindowsKernelDllName = "kernel32.dll";
    public const uint ProcessQueryLimitedInformationAccess = 0x1000;
    public const uint TokenQueryAccess = 0x0008;
    public const int MaxFolderResources = 512;
    public const int SessionKeyCapacity = 64;
    public const int ApplicationNameCapacity = 256;
    public const int ServiceNameCapacity = 64;
    public const int ErrorSuccess = 0;
    public const int ErrorAccessDenied = 5;
    public const int ErrorMoreData = 234;
    public const int ErrorCancelled = 1223;
    public const int ProcessActionFailedExitCode = 1;
    public const string ApplicationExecutableName = "HandleScope.exe";
    public const string ProcessTerminationArgument = "--terminate-process";
    public const string ElevationVerb = "runas";
    public static readonly TimeSpan GracefulCloseTimeout = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan ForcedTerminationTimeout = TimeSpan.FromSeconds(5);
}
