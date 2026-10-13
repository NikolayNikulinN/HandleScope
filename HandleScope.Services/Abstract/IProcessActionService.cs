using HandleScope.Services.Results;

namespace HandleScope.Services.Abstract;

/// <summary>Performs actions on running processes.</summary>
public interface IProcessActionService
{
    /// <summary>Closes the process with the specified identifier.</summary>
    Task<ProcessActionResult> CloseProcessAsync(int processId, CancellationToken cancellationToken);
}
