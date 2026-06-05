using HandleScope.Domain.Enums;

namespace HandleScope.Domain.Models;

public sealed record HandleSearchRequest(
    string TargetPath,
    HandleSearchMode SearchMode,
    bool IncludeChildren);
