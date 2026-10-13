using HandleScope.Application.Services;

namespace HandleScope.Presentation.Abstract;

/// <summary>Exposes localized text to WPF bindings.</summary>
public interface IResourceWrapper : IUiText
{
    /// <summary>Gets the application title.</summary>
    string AppTitle { get; }
    /// <summary>Gets the minimize button tooltip.</summary>
    string MinimizeToolTip { get; }
    /// <summary>Gets the maximize button tooltip.</summary>
    string MaximizeToolTip { get; }
    /// <summary>Gets the restore button tooltip.</summary>
    string RestoreToolTip { get; }
    /// <summary>Gets the close button tooltip.</summary>
    string CloseToolTip { get; }
    /// <summary>Gets the tooltip shown while closing is unavailable.</summary>
    string CloseBlockedToolTip { get; }
}
