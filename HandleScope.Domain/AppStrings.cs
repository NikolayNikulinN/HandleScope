namespace HandleScope.Domain;

public static class AppStrings
{
    public const string LogFilePath = "logs/handlescope-.log";
    public const string ApplicationStartingLogMessage = "HandleScope starting";
    public const string ApplicationShuttingDownLogMessage = "HandleScope shutting down";


    public const string StartingHandleSearchLogTemplate = "Starting handle search for {TargetPath} with mode {SearchMode} and includeChildren={IncludeChildren}";
    public const string SearchFailedLogTemplate = "Search failed for {TargetPath}";
    public const string EndProcessFailedLogTemplate = "Failed to end process {ProcessId}";

    public const string RestartManagerDllName = "rstrtmgr.dll";
    public const string RestartManagerSearchCompletedLogTemplate = "Restart Manager search completed for {TargetPath}. Registered {ResourceCount} resources and found {ResultCount} processes.";

    public const string ProcessTerminationRequestedLogTemplate = "Process termination requested for PID {ProcessId} ({ProcessName})";
    public const string ProcessCloseRequestedLogTemplate = "Sent close request to PID {ProcessId} ({ProcessName})";
    public const string ProcessForceTerminatingLogTemplate = "Force terminating PID {ProcessId} ({ProcessName})";
    public const string ProcessTerminationFailedLogTemplate = "Failed to terminate PID {ProcessId}";

    public const string ProcessDetailsResolveFailedLogTemplate = "Failed to resolve process details for PID {ProcessId}";
    public const string ExecutablePathResolveFailedLogTemplate = "Failed to resolve executable path for PID {ProcessId}";
}
