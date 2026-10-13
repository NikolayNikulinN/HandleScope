using System.Windows;
using HandleScope.ViewModels;

namespace HandleScope.Views;

/// <summary>Displays handle search and process results.</summary>
public partial class HomePage : System.Windows.Controls.UserControl
{
    /// <summary>Initializes the home page.</summary>
    public HomePage() => InitializeComponent();

    private void OnTargetPathPreviewDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            return;
        }

        e.Effects = DataContext is HomeViewModel { IsIdle: true }
                    && e.AllowedEffects.HasFlag(System.Windows.DragDropEffects.Copy)
                    && e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] { Length: 1 }
            ? System.Windows.DragDropEffects.Copy
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void OnTargetPathPreviewDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            return;
        }

        e.Effects = e.AllowedEffects.HasFlag(System.Windows.DragDropEffects.Copy)
                    && e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] { Length: 1 } paths
                    && TrySetDroppedPath(paths[0])
            ? System.Windows.DragDropEffects.Copy
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private bool TrySetDroppedPath(string path) =>
        DataContext is HomeViewModel viewModel && viewModel.TrySetDroppedPath(path);

    private void OnResultsSelectedCellsChanged(object sender, System.Windows.Controls.SelectedCellsChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.DataGrid grid && grid.SelectedCells.Count > 0)
        {
            grid.UnselectAllCells();
        }
    }
}
