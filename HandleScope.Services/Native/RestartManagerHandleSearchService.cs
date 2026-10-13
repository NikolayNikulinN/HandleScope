using HandleScope.Services.Abstract;
using HandleScope.Services.Constants;
using HandleScope.Logger.Constants;
using HandleScope.Services.Enums;
using HandleScope.Services.Models;
using HandleScope.Services.Results;
using Serilog;
using System.ComponentModel;
using System.Text;

namespace HandleScope.Services.Native;

/// <summary>Finds file and directory locks through Windows Restart Manager.</summary>
public sealed class RestartManagerHandleSearchService : IHandleSearchService
{
    private readonly IProcessInfoService processInfoService;
    private readonly ILogger logger;
    private readonly IUiText uiText;

    /// <summary>Initializes the Restart Manager search service.</summary>
    public RestartManagerHandleSearchService(IProcessInfoService processInfoService, ILogger logger, IUiText uiText)
    {
        this.processInfoService = processInfoService;
        this.logger = logger.ForContext<RestartManagerHandleSearchService>();
        this.uiText = uiText;
    }

    /// <inheritdoc />
    public Task<HandleSearchResult> SearchAsync(HandleSearchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(() => Search(request, cancellationToken), cancellationToken);
    }

    private HandleSearchResult Search(HandleSearchRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var warnings = new List<string>();
        var resources = BuildResourceList(request, warnings, cancellationToken);

        if (resources.Count == 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new HandleSearchResult(
                request.TargetPath,
                DateTimeOffset.Now,
                Array.Empty<HandleLockInfo>(),
                warnings);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var sessionKey = new StringBuilder(ServiceConstants.SessionKeyCapacity);
        var error = RestartManagerNativeMethods.RmStartSession(out var sessionHandle, 0, sessionKey);
        if (error != ServiceConstants.ErrorSuccess)
        {
            throw new Win32Exception(error, uiText.Get(UiTextKey.RestartManagerStartSessionFailed));
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

            if (error != ServiceConstants.ErrorSuccess)
            {
                throw new Win32Exception(error, uiText.Get(UiTextKey.RestartManagerRegisterResourcesFailed));
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
            cancellationToken.ThrowIfCancellationRequested();

            if (error == ServiceConstants.ErrorMoreData)
            {
                processInfoCount = processInfoNeeded;
                var affectedApplications = new RestartManagerNativeMethods.RmProcessInfo[processInfoNeeded];

                error = RestartManagerNativeMethods.RmGetList(
                    sessionHandle,
                    out processInfoNeeded,
                    ref processInfoCount,
                    affectedApplications,
                    ref rebootReasons);
                cancellationToken.ThrowIfCancellationRequested();

                if (error != ServiceConstants.ErrorSuccess)
                {
                    throw new Win32Exception(error, uiText.Get(UiTextKey.RestartManagerGetLockingProcessListFailed));
                }

                var items = CreateResults(affectedApplications.Take((int)processInfoCount), request, resources, cancellationToken);
                LogCompletion(request, resources.Count, items.Count);

                return new HandleSearchResult(
                    request.TargetPath,
                    DateTimeOffset.Now,
                    items,
                    warnings);
            }

            if (error != ServiceConstants.ErrorSuccess)
            {
                throw new Win32Exception(error, uiText.Get(UiTextKey.RestartManagerGetLockingProcessListFailed));
            }

            LogCompletion(request, resources.Count, 0);

            return new HandleSearchResult(
                request.TargetPath,
                DateTimeOffset.Now,
                Array.Empty<HandleLockInfo>(),
                warnings);
        }
        finally
        {
            RestartManagerNativeMethods.RmEndSession(sessionHandle);
        }
    }

    private IReadOnlyList<HandleLockInfo> CreateResults(
        IEnumerable<RestartManagerNativeMethods.RmProcessInfo> affectedApplications,
        HandleSearchRequest request,
        IReadOnlyList<string> resources,
        CancellationToken cancellationToken)
    {
        var lockedPath = resources.Count == 1 ? resources[0] : NormalizePath(request.TargetPath);
        var results = new List<HandleLockInfo>();
        var seen = new HashSet<int>();

        foreach (var application in affectedApplications)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var processId = application.Process.ProcessId;
            if (processId <= 0 || !seen.Add(processId))
            {
                continue;
            }

            var details = processInfoService.GetProcessDetails(processId);
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
                                            uiText.Get(UiTextKey.RestartManagerSourceName)));
        }

        return results
                    .OrderBy(item => item.ProcessName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.ProcessId)
                    .ToArray();
    }

    private IReadOnlyList<string> BuildResourceList(HandleSearchRequest request, List<string> warnings, CancellationToken cancellationToken)
    {
        var targetPath = NormalizePath(request.TargetPath);

        if (request.SearchMode == HandleSearchMode.File)
        {
            if (!File.Exists(targetPath))
            {
                warnings.Add(uiText.Get(UiTextKey.TargetFileMissingWarning));
                return Array.Empty<string>();
            }

            return [targetPath];
        }

        if (!Directory.Exists(targetPath))
        {
            warnings.Add(uiText.Get(UiTextKey.TargetFolderMissingWarning));
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
            cancellationToken.ThrowIfCancellationRequested();
            resources.Add(NormalizePath(file));

            if (resources.Count >= ServiceConstants.MaxFolderResources)
            {
                warnings.Add(string.Format(uiText.Get(UiTextKey.FolderSearchLimitedFormat), ServiceConstants.MaxFolderResources));
                break;
            }
        }

        if (resources.Count == 0)
        {
            warnings.Add(uiText.Get(UiTextKey.EmptyFolderWarning));
        }

        return resources;
    }

    private static string NormalizePath(string path)
    {
        var normalized = Path.GetFullPath(path.Trim().Replace('/', '\\'));
        return Path.TrimEndingDirectorySeparator(normalized);
    }

    private void LogCompletion(HandleSearchRequest request, int resourceCount, int resultCount)
    {
        logger.Information(
                            LogConstants.RestartManagerSearchCompletedLogTemplate,
                            request.TargetPath,
                            resourceCount,
                            resultCount);
    }
}
