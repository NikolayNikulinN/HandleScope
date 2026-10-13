namespace HandleScope.Services.Results;

/// <summary>Reports the outcome of a process action.</summary>
public sealed record ProcessActionResult(bool Succeeded, string Message, bool RequiresElevation = false);
