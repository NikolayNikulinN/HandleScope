using System.Globalization;
using System.Resources;
using HandleScope.Application.Services;
using HandleScope.Presentation.Abstract;

namespace HandleScope.Presentation.Wrappers;

/// <summary>Reads localized application text from AppResource.resx.</summary>
public sealed class ResourceWrapper : IResourceWrapper
{
    private static readonly ResourceManager Resources = new(
        "HandleScope.Presentation.Resources.AppResource", typeof(ResourceWrapper).Assembly);

    /// <inheritdoc />
    public string Get(UiTextKey key) =>
        Resources.GetString(key.ToString(), CultureInfo.CurrentUICulture)
        ?? throw new MissingManifestResourceException($"Missing UI resource: {key}");

    public string AppTitle => Get(UiTextKey.AppTitle);
    public string AppSubtitle => Get(UiTextKey.AppSubtitle);
    public string BrowseButton => Get(UiTextKey.BrowseButton);
    public string SearchButton => Get(UiTextKey.SearchButton);
    public string RefreshButton => Get(UiTextKey.RefreshButton);
    public string EndProcessButton => Get(UiTextKey.EndProcessButton);
    public string CancelButton => Get(UiTextKey.CancelButton);
    public string ModeLabel => Get(UiTextKey.ModeLabel);
    public string IncludeChildPaths => Get(UiTextKey.IncludeChildPaths);
    public string GridProcess => Get(UiTextKey.GridProcess);
    public string GridProcessId => Get(UiTextKey.GridProcessId);
    public string GridUser => Get(UiTextKey.GridUser);
    public string GridExecutablePath => Get(UiTextKey.GridExecutablePath);
    public string GridLockedPath => Get(UiTextKey.GridLockedPath);
    public string GridHandleType => Get(UiTextKey.GridHandleType);
    public string GridAccess => Get(UiTextKey.GridAccess);
    public string MinimizeToolTip => Get(UiTextKey.MinimizeToolTip);
    public string MaximizeToolTip => Get(UiTextKey.MaximizeToolTip);
    public string RestoreToolTip => Get(UiTextKey.RestoreToolTip);
    public string CloseToolTip => Get(UiTextKey.CloseToolTip);
    public string CloseBlockedToolTip => Get(UiTextKey.CloseBlockedToolTip);
    public string UnhandledErrorMessage => Get(UiTextKey.UnhandledErrorMessage);
}
