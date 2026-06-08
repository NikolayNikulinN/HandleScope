namespace HandleScope.Domain.Results;

public sealed record ProcessActionResult(
    bool Succeeded,
    string Message);
