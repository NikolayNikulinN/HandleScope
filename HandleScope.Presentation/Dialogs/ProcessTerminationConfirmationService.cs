using System.Globalization;
using System.Windows;
using HandleScope.Presentation.Abstract;
using HandleScope.Presentation.Constants;
using HandleScope.Presentation.Controls;
using HandleScope.Services.Abstract;

namespace HandleScope.Presentation.Dialogs;

/// <summary>Shows the process termination confirmation as a modal application dialog.</summary>
public sealed class ProcessTerminationConfirmationService : IProcessTerminationConfirmationService
{
    private readonly IResourceWrapper resourceWrapper;
    private readonly AppConstants appConstants;

    /// <summary>Creates a confirmation service using application text and image resources.</summary>
    public ProcessTerminationConfirmationService(IResourceWrapper resourceWrapper, AppConstants appConstants)
    {
        this.resourceWrapper = resourceWrapper;
        this.appConstants = appConstants;
    }

    /// <inheritdoc />
    public bool Confirm(string processName, int processId)
    {
        var message = string.Format(CultureInfo.CurrentUICulture,
                                    resourceWrapper.Get(UiTextKey.ConfirmProcessTerminationFormat),
                                    processName,
                                    processId);
        var images = appConstants.Images;
        var dialog = new UiPopupWindow(message,
                                       resourceWrapper.Get(UiTextKey.ConfirmProcessTerminationTitle),
                                       resourceWrapper.Get(UiTextKey.EndProcessButton),
                                       resourceWrapper.Get(UiTextKey.CancelButton),
                                       images.WarningPath,
                                       images.AppIconPath,
                                       images.ClosePath,
                                       resourceWrapper.CloseToolTip)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true;
    }
}
