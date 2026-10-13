using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandleScope.Application.Services;
using HandleScope.Presentation.Abstract;
using HandleScope.Domain;
using HandleScope.Domain.Enums;
using HandleScope.Domain.Models;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace HandleScope.ViewModels;

/// <summary>Provides search commands and results for the home page.</summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private readonly IHandleSearchService _handleSearchService;
    private readonly IProcessActionService _processActionService;
    private readonly ILogger _logger;
    private readonly IResourceWrapper _resourceWrapper;
    private CancellationTokenSource? _searchCancellation;
    private HandleSearchRequest? _lastSearchRequest;

    public IReadOnlyList<HandleSearchModeOption> SearchModes { get; }

    public ObservableCollection<HandleLockInfo> Results { get; } = [];

    public ObservableCollection<string> Warnings { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string _targetPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirectorySearchMode))]
    private HandleSearchModeOption _selectedSearchMode = null!;

    [ObservableProperty]
    private bool _includeChildren = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelSearchCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    [NotifyCanExecuteChangedFor(nameof(EndSelectedProcessCommand))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isSearching;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    [NotifyCanExecuteChangedFor(nameof(EndSelectedProcessCommand))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isEndingProcess;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EndSelectedProcessCommand))]
    private HandleLockInfo? _selectedResult;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public IResourceWrapper ResourceWrapper => _resourceWrapper;

    public bool IsIdle => !IsSearching && !IsEndingProcess;

    public bool HasResults => Results.Count > 0;

    public bool HasWarnings => Warnings.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsDirectorySearchMode => SelectedSearchMode.Mode == HandleSearchMode.Directory;

    public HomeViewModel(IHandleSearchService handleSearchService,
                         IProcessActionService processActionService,
                         ILogger logger,
                         IResourceWrapper resourceWrapper)
    {
        _handleSearchService = handleSearchService;
        _processActionService = processActionService;
        _logger = logger.ForContext<HomeViewModel>();
        _resourceWrapper = resourceWrapper;
        var fileMode = new HandleSearchModeOption(resourceWrapper.Get(UiTextKey.FileSearchModeDisplayName), HandleSearchMode.File);
        SearchModes = [fileMode,
                       new HandleSearchModeOption(resourceWrapper.Get(UiTextKey.DirectorySearchModeDisplayName), HandleSearchMode.Directory)];
        _selectedSearchMode = fileMode;
        _statusMessage = resourceWrapper.Get(UiTextKey.InitialStatusMessage);
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
                Description = _resourceWrapper.Get(UiTextKey.SelectTargetFolderDescription),
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
            Title = _resourceWrapper.Get(UiTextKey.SelectTargetFileTitle)
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
            string.Format(_resourceWrapper.Get(UiTextKey.ConfirmProcessTerminationFormat), selected.ProcessName, selected.ProcessId),
            _resourceWrapper.Get(UiTextKey.ConfirmProcessTerminationTitle),
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
            StatusMessage = string.Format(_resourceWrapper.Get(UiTextKey.EndingProcessStatusFormat), selected.ProcessName);

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
            ErrorMessage = _resourceWrapper.Get(UiTextKey.EndProcessFailedUserMessage);
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
            StatusMessage = _resourceWrapper.Get(UiTextKey.SearchInProgressStatusMessage);

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
                ? _resourceWrapper.Get(UiTextKey.NoLockingProcessesFoundStatusMessage)
                : string.Format(_resourceWrapper.Get(UiTextKey.LockingProcessEntriesFoundFormat), result.Items.Count);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = _resourceWrapper.Get(UiTextKey.SearchCancelledStatusMessage);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, AppStrings.SearchFailedLogTemplate, request.TargetPath);
            ErrorMessage = _resourceWrapper.Get(UiTextKey.SearchFailedUserMessage);
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

/// <summary>Describes a selectable search mode.</summary>
public sealed record HandleSearchModeOption(string DisplayName, HandleSearchMode Mode);
