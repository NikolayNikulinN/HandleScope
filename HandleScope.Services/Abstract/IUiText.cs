namespace HandleScope.Services.Abstract;

/// <summary>Identifies text displayed by the application.</summary>
public enum UiTextKey
{
    AppTitle,
    AppSubtitle,
    TargetPathHint,
    BrowseButton,
    SearchButton,
    StopButton,
    RefreshButton,
    EndProcessButton,
    CancelButton,
    ModeLabel,
    IncludeChildPaths,
    GridProcess,
    GridProcessId,
    GridUser,
    GridExecutablePath,
    GridLockedPath,
    GridHandleType,
    GridAccess,
    FileSearchModeDisplayName,
    DirectorySearchModeDisplayName,
    InitialStatusMessage,
    SelectTargetFolderDescription,
    SelectTargetFileTitle,
    ConfirmProcessTerminationTitle,
    ConfirmProcessTerminationFormat,
    EndingProcessStatusFormat,
    EndProcessFailedUserMessage,
    ElevationCancelledMessage,
    ElevationStartFailedMessage,
    SearchInProgressStatusMessage,
    NoLockingProcessesFoundStatusMessage,
    LockingProcessEntriesFoundFormat,
    SearchCancelledStatusMessage,
    SearchFailedUserMessage,
    RestartManagerSourceName,
    RestartManagerStartSessionFailed,
    RestartManagerRegisterResourcesFailed,
    RestartManagerGetLockingProcessListFailed,
    TargetFileMissingWarning,
    TargetFolderMissingWarning,
    FolderSearchLimitedFormat,
    EmptyFolderWarning,
    CannotTerminateSelfMessage,
    ProcessAlreadyExitedFormat,
    ProcessClosedFormat,
    ProcessTerminatedFormat,
    ProcessDidNotExitFormat,
    ProcessNoLongerRunningFormat,
    ProcessTerminationFailedFormat,
    ProcessDetailsFallbackNameFormat,
    MinimizeToolTip,
    MaximizeToolTip,
    RestoreToolTip,
    CloseToolTip,
    CloseBlockedToolTip
}

/// <summary>Provides localized text without coupling services to WPF resources.</summary>
public interface IUiText
{
    /// <summary>Gets the localized text for a key.</summary>
    string Get(UiTextKey key);
}
