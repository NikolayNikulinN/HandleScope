using HandleScope.Services.Models;

namespace HandleScope.Services.Abstract;

/// <summary>Provides details about running processes.</summary>
public interface IProcessInfoService
{
    /// <summary>Gets details for the process with the specified identifier.</summary>
    ProcessDetails GetProcessDetails(int processId);
}
