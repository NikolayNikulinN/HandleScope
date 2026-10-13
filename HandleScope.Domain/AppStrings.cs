namespace HandleScope.Domain;

public static class AppStrings
{
    public const string LogFilePath = "logs/handlescope-.log";
    public const string ApplicationStartingLogMessage = "HandleScope starting";
    public const string ApplicationShuttingDownLogMessage = "HandleScope shutting down";

    public const string FileSearchModeDisplayName = "File";
    public const string DirectorySearchModeDisplayName = "Folder";

    public const string InitialStatusMessage = "Enter a file or folder path to find locking processes.";
    public const string SelectTargetFolderDescription = "Select target folder";
    public const string SelectTargetFileTitle = "Select target file";
    public const string ConfirmProcessTerminationTitle = "Confirm Process Termination";
    public const string ConfirmProcessTerminationFormat = "End process {0} (PID {1})?";
    public const string EndingProcessStatusFormat = "Ending process {0}...";
    public const string EndProcessFailedUserMessage = "Failed to end process. See the log file for details.";
    public const string SearchInProgressStatusMessage = "Searching for locking processes...";
    public const string NoLockingProcessesFoundStatusMessage = "No locking processes found.";
    public const string LockingProcessEntriesFoundFormat = "Found {0} locking process entries.";
    public const string SearchCancelledStatusMessage = "Search cancelled.";
    public const string SearchFailedUserMessage = "Search failed. See the log file for details.";
    public const string StartingHandleSearchLogTemplate = "Starting handle search for {TargetPath} with mode {SearchMode} and includeChildren={IncludeChildren}";
    public const string SearchFailedLogTemplate = "Search failed for {TargetPath}";
    public const string EndProcessFailedLogTemplate = "Failed to end process {ProcessId}";

    public const string RestartManagerSourceName = "Restart Manager";
    public const string RestartManagerDllName = "rstrtmgr.dll";
    public const string RestartManagerStartSessionFailed = "Failed to start Restart Manager session.";
    public const string RestartManagerRegisterResourcesFailed = "Failed to register resources with Restart Manager.";
    public const string RestartManagerGetLockingProcessListFailed = "Failed to get locking process list from Restart Manager.";
    public const string TargetFileMissingWarning = "The target file does not exist. Restart Manager can inspect only existing files.";
    public const string TargetFolderMissingWarning = "The target folder does not exist. Restart Manager can inspect only files inside existing folders.";
    public const string FolderSearchLimitedFormat = "Folder search was limited to the first {0} files.";
    public const string EmptyFolderWarning = "No files were found inside the folder. Pure directory handles are not detected by Restart Manager.";
    public const string RestartManagerSearchCompletedLogTemplate = "Restart Manager search completed for {TargetPath}. Registered {ResourceCount} resources and found {ResultCount} processes.";

    public const string CannotTerminateSelfMessage = "HandleScope cannot terminate itself.";
    public const string ProcessTerminationRequestedLogTemplate = "Process termination requested for PID {ProcessId} ({ProcessName})";
    public const string ProcessAlreadyExitedFormat = "Process {0} has already exited.";
    public const string ProcessCloseRequestedLogTemplate = "Sent close request to PID {ProcessId} ({ProcessName})";
    public const string ProcessClosedFormat = "Process {0} was closed.";
    public const string ProcessForceTerminatingLogTemplate = "Force terminating PID {ProcessId} ({ProcessName})";
    public const string ProcessTerminatedFormat = "Process {0} was terminated.";
    public const string ProcessDidNotExitFormat = "Process {0} did not exit in time.";
    public const string ProcessNoLongerRunningFormat = "PID {0} is no longer running.";
    public const string ProcessTerminationFailedLogTemplate = "Failed to terminate PID {ProcessId}";
    public const string ProcessTerminationFailedFormat = "Failed to terminate PID {0}: {1}";

    public const string ProcessDetailsFallbackNameFormat = "PID {0}";
    public const string ProcessDetailsResolveFailedLogTemplate = "Failed to resolve process details for PID {ProcessId}";
    public const string ExecutablePathResolveFailedLogTemplate = "Failed to resolve executable path for PID {ProcessId}";
}
