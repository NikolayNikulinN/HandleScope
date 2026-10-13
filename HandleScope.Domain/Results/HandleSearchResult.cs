using HandleScope.Domain.Models;

namespace HandleScope.Domain.Results;

public sealed record HandleSearchResult(
    string TargetPath,
    DateTimeOffset CapturedAt,
    IReadOnlyList<HandleLockInfo> Items,
    IReadOnlyList<string> Warnings);
