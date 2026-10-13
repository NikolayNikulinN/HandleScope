namespace HandleScope.Logger.Constants;

/// <summary>
/// Provides logging configuration and message templates.
/// </summary>
public static class LogConstants
{
    public const string ApplicationStartingLogMessage = "HandleScope starting";

    public const string ApplicationShuttingDownLogMessage = "HandleScope shutting down";

    public const string StartingHandleSearchLogTemplate = "Starting handle search for {TargetPath} with mode {SearchMode} and includeChildren={IncludeChildren}";

    public const string SearchFailedLogTemplate = "Search failed for {TargetPath}";

    public const string EndProcessFailedLogTemplate = "Failed to end process {ProcessId}";

    public const string RestartManagerSearchCompletedLogTemplate = "Restart Manager search completed for {TargetPath}. " +
                                                                   "Registered {ResourceCount} resources and found {ResultCount} processes.";

    public const string ProcessTerminationRequestedLogTemplate = "Process termination requested for PID {ProcessId} ({ProcessName})";

    public const string ProcessCloseRequestedLogTemplate = "Sent close request to PID {ProcessId} ({ProcessName})";

    public const string ProcessForceTerminatingLogTemplate = "Force terminating PID {ProcessId} ({ProcessName})";

    public const string ProcessTerminationFailedLogTemplate = "Failed to terminate PID {ProcessId}";

    public const string ProcessDetailsResolveFailedLogTemplate = "Failed to resolve process details for PID {ProcessId}";

    public const string ExecutablePathResolveFailedLogTemplate = "Failed to resolve executable path for PID {ProcessId}";

    public const int RetainedFileCountLimit = 14;

    public const string ApplicationFolderName = "HandleScope";

    public const string LogDirectoryName = "Logs";

    public const string SolutionFileName = "HandleScope.sln";

    public const string LogFileName = "HandleScope-.log";

    public const string WriteProbeFileNameFormat = ".write-test-{0:N}.tmp";

    public const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    public const string LogTemplateUnhandledUiException = "Unhandled UI exception.";

    public const string LogTemplateUnhandledApplicationException = "Unhandled application exception.";

    public const string LogTemplateApplicationStarted = "HandleScope started.";

    public const string LogTemplateApplicationExited = "HandleScope exited with code {ExitCode}.";

    public static string LocalAppDataLogDirectory { get; } = Path.Combine(
                                                                           Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                                           ApplicationFolderName,
                                                                           LogDirectoryName);
}
