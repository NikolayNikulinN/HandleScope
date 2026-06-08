using HandleScope.Domain.Models;

namespace HandleScope.Application.Services;

public interface IProcessInfoService
{
    ProcessDetails GetProcessDetails(int processId);
}
