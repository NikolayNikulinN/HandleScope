namespace HandleScope.Services.Models;

/// <summary>Describes a running process.</summary>
public sealed record ProcessDetails(
    int ProcessId,
    string ProcessName,
    string? ExecutablePath,
    string? UserName);
