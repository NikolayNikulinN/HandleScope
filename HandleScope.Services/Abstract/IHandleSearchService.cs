using HandleScope.Services.Models;
using HandleScope.Services.Results;

namespace HandleScope.Services.Abstract;

/// <summary>Finds processes holding handles to a file or directory.</summary>
public interface IHandleSearchService
{
    /// <summary>Searches for processes holding handles to the requested path.</summary>
    Task<HandleSearchResult> SearchAsync(HandleSearchRequest request, CancellationToken cancellationToken);
}
