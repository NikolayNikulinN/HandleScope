namespace HandleScope.Domain.Models;

public sealed record ProcessDetails(
    int ProcessId,
    string ProcessName,
    string? ExecutablePath,
    string? UserName);
