using HandleScope.Services.Enums;

namespace HandleScope.Services.Models;

/// <summary>Defines the target and options for a handle search.</summary>
public sealed record HandleSearchRequest(
    string TargetPath,
    HandleSearchMode SearchMode,
    bool IncludeChildren);
