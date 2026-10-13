using HandleScope.Application.Services;
using HandleScope.Domain;
using HandleScope.Domain.Enums;
using HandleScope.Domain.Models;
using HandleScope.Domain.Results;
using Serilog;
using System.ComponentModel;
using System.Text;

namespace HandleScope.Infrastructure.Native;

public sealed class RestartManagerHandleSearchService : IHandleSearchService
{
    private const int MaxFolderResources = 512;

    private readonly IProcessInfoService _processInfoService;
    private readonly ILogger _logger;
    private readonly IUiText _uiText;

    public RestartManagerHandleSearchService(IProcessInfoService processInfoService, ILogger logger, IUiText uiText)
    {
        _processInfoService = processInfoService;
        _logger = logger.ForContext<RestartManagerHandleSearchService>();
        _uiText = uiText;
    }

    public Task<HandleSearchResult> SearchAsync(HandleSearchRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var warnings = new List<string>();
        var resources = BuildResourceList(request, warnings);

        if (resources.Count == 0)
        {
            return Task.FromResult(new HandleSearchResult(
                request.TargetPath,
                DateTimeOffset.Now,
                Array.Empty<HandleLockInfo>(),
                warnings));
        }

        var sessionKey = new StringBuilder(64);
        var error = RestartManagerNativeMethods.RmStartSession(out var sessionHandle, 0, sessionKey);
        if (error != RestartManagerNativeMethods.ErrorSuccess)
        {
            throw new Win32Exception(error, _uiText.Get(UiTextKey.RestartManagerStartSessionFailed));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            error = RestartManagerNativeMethods.RmRegisterResources(
                sessionHandle,
                (uint)resources.Count,
                resources.ToArray(),
                0,
                null,
                0,
                null);

            if (error != RestartManagerNativeMethods.ErrorSuccess)
            {
                throw new Win32Exception(error, _uiText.Get(UiTextKey.RestartManagerRegisterResourcesFailed));
            }

            var processInfoNeeded = 0u;
            var processInfoCount = 0u;
            var rebootReasons = 0u;

            error = RestartManagerNativeMethods.RmGetList(
                sessionHandle,
                out processInfoNeeded,
                ref processInfoCount,
                null,
                ref rebootReasons);

            if (error == RestartManagerNativeMethods.ErrorMoreData)
            {
                processInfoCount = processInfoNeeded;
                var affectedApplications = new RestartManagerNativeMethods.RmProcessInfo[processInfoNeeded];

                error = RestartManagerNativeMethods.RmGetList(
                    sessionHandle,
                    out processInfoNeeded,
                    ref processInfoCount,
                    affectedApplications,
                    ref rebootReasons);

                if (error != RestartManagerNativeMethods.ErrorSuccess)
                {
                    throw new Win32Exception(error, _uiText.Get(UiTextKey.RestartManagerGetLockingProcessListFailed));
                }

                var items = CreateResults(affectedApplications.Take((int)processInfoCount), request, resources);
                LogCompletion(request, resources.Count, items.Count);

                return Task.FromResult(new HandleSearchResult(
                    request.TargetPath,
                    DateTimeOffset.Now,
                    items,
                    warnings));
            }

            if (error != RestartManagerNativeMethods.ErrorSuccess)
            {
                throw new Win32Exception(error, _uiText.Get(UiTextKey.RestartManagerGetLockingProcessListFailed));
            }

            LogCompletion(request, resources.Count, 0);

            return Task.FromResult(new HandleSearchResult(
                request.TargetPath,
                DateTimeOffset.Now,
                Array.Empty<HandleLockInfo>(),
                warnings));
        }
        finally
        {
            RestartManagerNativeMethods.RmEndSession(sessionHandle);
        }
    }

    private IReadOnlyList<HandleLockInfo> CreateResults(
        IEnumerable<RestartManagerNativeMethods.RmProcessInfo> affectedApplications,
        HandleSearchRequest request,
        IReadOnlyList<string> resources)
    {
        var lockedPath = resources.Count == 1 ? resources[0] : NormalizePath(request.TargetPath);
        var results = new List<HandleLockInfo>();
        var seen = new HashSet<int>();

        foreach (var application in affectedApplications)
        {
            var processId = application.Process.ProcessId;
            if (processId <= 0 || !seen.Add(processId))
            {
                continue;
            }

            var details = _processInfoService.GetProcessDetails(processId);
            var processName = string.IsNullOrWhiteSpace(details.ProcessName)
                ? application.ApplicationName
                : details.ProcessName;

            results.Add(new HandleLockInfo(
                processId,
                processName,
                details.ExecutablePath,
                details.UserName,
                lockedPath,
                application.ApplicationType.ToString(),
                _uiText.Get(UiTextKey.RestartManagerSourceName)));
        }

        return results
            .OrderBy(item => item.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ProcessId)
            .ToArray();
    }

    private IReadOnlyList<string> BuildResourceList(HandleSearchRequest request, List<string> warnings)
    {
        var targetPath = NormalizePath(request.TargetPath);

        if (request.SearchMode == HandleSearchMode.File)
        {
            if (!File.Exists(targetPath))
            {
                warnings.Add(_uiText.Get(UiTextKey.TargetFileMissingWarning));
                return Array.Empty<string>();
            }

            return [targetPath];
        }

        if (!Directory.Exists(targetPath))
        {
            warnings.Add(_uiText.Get(UiTextKey.TargetFolderMissingWarning));
            return Array.Empty<string>();
        }

        var resources = new List<string>();
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = request.IncludeChildren,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        foreach (var file in Directory.EnumerateFiles(targetPath, "*", options))
        {
            resources.Add(NormalizePath(file));

            if (resources.Count >= MaxFolderResources)
            {
                warnings.Add(string.Format(_uiText.Get(UiTextKey.FolderSearchLimitedFormat), MaxFolderResources));
                break;
            }
        }

        if (resources.Count == 0)
        {
            warnings.Add(_uiText.Get(UiTextKey.EmptyFolderWarning));
        }

        return resources;
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Trim().Replace('/', '\\');

        try
        {
            normalized = Path.GetFullPath(normalized);
        }
        catch
        {
        }

        return normalized.TrimEnd('\\');
    }

    private void LogCompletion(HandleSearchRequest request, int resourceCount, int resultCount)
    {
        _logger.Information(
            AppStrings.RestartManagerSearchCompletedLogTemplate,
            request.TargetPath,
            resourceCount,
            resultCount);
    }
}
