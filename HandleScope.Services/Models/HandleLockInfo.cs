namespace HandleScope.Services.Models;

/// <summary>Describes a process holding a handle to a path.</summary>
public sealed record HandleLockInfo(
    int ProcessId,
    string ProcessName,
    string? ProcessPath,
    string? UserName,
    string LockedPath,
    string? HandleType,
    string? Access);
