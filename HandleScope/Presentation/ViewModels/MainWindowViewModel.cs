using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandleScope.Application.Services;
using HandleScope.Domain;
using HandleScope.Domain.Enums;
using HandleScope.Domain.Models;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace HandleScope.Presentation.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private static readonly HandleSearchModeOption FileSearchMode = new(AppStrings.FileSearchModeDisplayName, HandleSearchMode.File);
    private static readonly HandleSearchModeOption DirectorySearchMode = new(AppStrings.DirectorySearchModeDisplayName, HandleSearchMode.Directory);

    private readonly IHandleSearchService _handleSearchService;
    private readonly IProcessActionService _processActionService;
    private readonly ILogger _logger;
    private CancellationTokenSource? _searchCancellation;
    private HandleSearchRequest? _lastSearchRequest;

    public IReadOnlyList<HandleSearchModeOption> SearchModes { get; } =
    [
        FileSearchMode,
        DirectorySearchMode
    ];

    public ObservableCollection<HandleLockInfo> Results { get; } = [];

    public ObservableCollection<string> Warnings { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string _targetPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirectorySearchMode))]
    private HandleSearchModeOption _selectedSearchMode = FileSearchMode;

    [ObservableProperty]
    private bool _includeChildren = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelSearchCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    [NotifyCanExecuteChangedFor(nameof(EndSelectedProcessCommand))]
    private bool _isSearching;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    [NotifyCanExecuteChangedFor(nameof(EndSelectedProcessCommand))]
    private bool _isEndingProcess;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EndSelectedProcessCommand))]
    private HandleLockInfo? _selectedResult;

    [ObservableProperty]
    private string _statusMessage = AppStrings.InitialStatusMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasResults => Results.Count > 0;

    public bool HasWarnings => Warnings.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsDirectorySearchMode => SelectedSearchMode.Mode == HandleSearchMode.Directory;

    public MainWindowViewModel(
        IHandleSearchService handleSearchService,
        IProcessActionService processActionService,
        ILogger logger)
    {
        _handleSearchService = handleSearchService;
        _processActionService = processActionService;
        _logger = logger.ForContext<MainWindowViewModel>();
    }

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchAsync()
    {
        var request = new HandleSearchRequest(
            TargetPath.Trim(),
            SelectedSearchMode.Mode,
            IncludeChildren);

        await RunSearchAsync(request, rememberRequest: true);
    }

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        if (_lastSearchRequest is null)
        {
            return;
        }

        await RunSearchAsync(_lastSearchRequest, rememberRequest: false);
    }

    [RelayCommand(CanExecute = nameof(CanBrowse))]
    private void Browse()
    {
        if (SelectedSearchMode.Mode == HandleSearchMode.Directory)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = AppStrings.SelectTargetFolderDescription,
                UseDescriptionForTitle = true,
                SelectedPath = Directory.Exists(TargetPath) ? TargetPath : string.Empty
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TargetPath = dialog.SelectedPath;
            }

            return;
        }

        var fileDialog = new Microsoft.Win32.OpenFileDialog
        {
            CheckFileExists = false,
            CheckPathExists = true,
            Title = AppStrings.SelectTargetFileTitle
        };

        if (fileDialog.ShowDialog() == true)
        {
            TargetPath = fileDialog.FileName;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelSearch))]
    private void CancelSearch()
    {
        _searchCancellation?.Cancel();
    }

    [RelayCommand(CanExecute = nameof(CanEndSelectedProcess))]
    private async Task EndSelectedProcessAsync()
    {
        if (SelectedResult is null)
        {
            return;
        }

        var selected = SelectedResult;
        var confirmation = System.Windows.MessageBox.Show(
            string.Format(AppStrings.ConfirmProcessTerminationFormat, selected.ProcessName, selected.ProcessId),
            AppStrings.ConfirmProcessTerminationTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsEndingProcess = true;
            ErrorMessage = null;
            StatusMessage = string.Format(AppStrings.EndingProcessStatusFormat, selected.ProcessName);

            var result = await _processActionService.CloseProcessAsync(selected.ProcessId, CancellationToken.None);
            StatusMessage = result.Message;

            if (!result.Succeeded)
            {
                ErrorMessage = result.Message;
                return;
            }

            Results.Remove(selected);
            NotifyCollectionStateChanged();

            if (_lastSearchRequest is not null)
            {
                await RunSearchAsync(_lastSearchRequest, rememberRequest: false);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, AppStrings.EndProcessFailedLogTemplate, selected.ProcessId);
            ErrorMessage = AppStrings.EndProcessFailedUserMessage;
            StatusMessage = ErrorMessage;
        }
        finally
        {
            IsEndingProcess = false;
        }
    }

    private async Task RunSearchAsync(HandleSearchRequest request, bool rememberRequest)
    {
        _searchCancellation?.Cancel();
        _searchCancellation = new CancellationTokenSource();

        Results.Clear();
        Warnings.Clear();
        ErrorMessage = null;
        SelectedResult = null;
        NotifyCollectionStateChanged();

        try
        {
            IsSearching = true;
            StatusMessage = AppStrings.SearchInProgressStatusMessage;

            if (rememberRequest)
            {
                _lastSearchRequest = request;
                RefreshCommand.NotifyCanExecuteChanged();
            }

            _logger.Information(
                AppStrings.StartingHandleSearchLogTemplate,
                request.TargetPath,
                request.SearchMode,
                request.IncludeChildren);

            var result = await _handleSearchService.SearchAsync(request, _searchCancellation.Token);

            foreach (var item in result.Items)
            {
                Results.Add(item);
            }

            foreach (var warning in result.Warnings)
            {
                Warnings.Add(warning);
            }

            NotifyCollectionStateChanged();

            StatusMessage = result.Items.Count == 0
                ? AppStrings.NoLockingProcessesFoundStatusMessage
                : string.Format(AppStrings.LockingProcessEntriesFoundFormat, result.Items.Count);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = AppStrings.SearchCancelledStatusMessage;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, AppStrings.SearchFailedLogTemplate, request.TargetPath);
            ErrorMessage = AppStrings.SearchFailedUserMessage;
            StatusMessage = ErrorMessage;
        }
        finally
        {
            IsSearching = false;
            _searchCancellation.Dispose();
            _searchCancellation = null;
            RefreshCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSearch()
    {
        return !IsSearching && !IsEndingProcess && !string.IsNullOrWhiteSpace(TargetPath);
    }

    private bool CanRefresh()
    {
        return !IsSearching && !IsEndingProcess && _lastSearchRequest is not null;
    }

    private bool CanBrowse()
    {
        return !IsSearching && !IsEndingProcess;
    }

    private bool CanCancelSearch()
    {
        return IsSearching;
    }

    private bool CanEndSelectedProcess()
    {
        return !IsSearching && !IsEndingProcess && SelectedResult is not null;
    }

    private void NotifyCollectionStateChanged()
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(HasWarnings));
    }
}

public sealed record HandleSearchModeOption(string DisplayName, HandleSearchMode Mode);
