
namespace HandleScope.Services.Constants;

/// <summary>Defines limits and native values used by process services.</summary>
public static class ServiceConstants
{
    public const string RestartManagerDllName = "rstrtmgr.dll";
    public const int MaxFolderResources = 512;
    public const int SessionKeyCapacity = 64;
    public const int ApplicationNameCapacity = 256;
    public const int ServiceNameCapacity = 64;
    public const int ErrorSuccess = 0;
    public const int ErrorMoreData = 234;
    public static readonly TimeSpan GracefulCloseTimeout = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan ForcedTerminationTimeout = TimeSpan.FromSeconds(5);
}
