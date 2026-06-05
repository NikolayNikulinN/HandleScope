namespace HandleScope.Domain.Models;

public sealed record HandleLockInfo(
    int ProcessId,
    string ProcessName,
    string? ProcessPath,
    string? UserName,
    string LockedPath,
    string? HandleType,
    string? Access);
