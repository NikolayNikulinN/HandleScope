using HandleScope.Domain.Models;
using HandleScope.Domain.Results;

namespace HandleScope.Application.Services;

public interface IHandleSearchService
{
    Task<HandleSearchResult> SearchAsync(HandleSearchRequest request, CancellationToken cancellationToken);
}
