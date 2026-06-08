using HandleScope.Domain.Results;

namespace HandleScope.Application.Services;

public interface IProcessActionService
{
    Task<ProcessActionResult> CloseProcessAsync(int processId, CancellationToken cancellationToken);
}
