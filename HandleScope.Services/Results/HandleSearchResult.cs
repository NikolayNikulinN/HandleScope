using HandleScope.Services.Models;

namespace HandleScope.Services.Results;

/// <summary>Contains processes and warnings returned by a handle search.</summary>
public sealed record HandleSearchResult(
                                        string TargetPath,
                                        DateTimeOffset CapturedAt,
                                        IReadOnlyList<HandleLockInfo> Items,
                                        IReadOnlyList<string> Warnings);
