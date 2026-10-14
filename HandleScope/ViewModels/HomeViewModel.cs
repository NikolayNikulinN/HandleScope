using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandleScope.Services.Abstract;
using HandleScope.Logger.Constants;
using HandleScope.Presentation.Abstract;
using HandleScope.Services.Enums;
using HandleScope.Services.Models;
using Serilog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using HandleScope.Models;

namespace HandleScope.ViewModels;

/// <summary>Provides search commands and results for the home page.</summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private readonly IHandleSearchService handleSearchService;
    private readonly IProcessActionService processActionService;
    private readonly ILogger logger;
    private readonly IResourceWrapper resourceWrapper;
    private readonly IProcessTerminationConfirmationService processTerminationConfirmationService;
    private CancellationTokenSource? searchCancellation;
    private HandleSearchRequest? lastSearchRequest;

    public IReadOnlyList<HandleSearchModeOption> SearchModes { get; }

    public ObservableCollection<HandleLockResultModel> Results { get; } = [];

    public ObservableCollection<string> Warnings { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    [NotifyCanExecuteChangedFor(nameof(SearchOrStopCommand))]
    private string targetPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirectorySearchMode))]
    private HandleSearchModeOption selectedSearchMode = null!;

    [ObservableProperty]
    private bool _includeChildren = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    [NotifyCanExecuteChangedFor(nameof(SearchOrStopCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    [NotifyCanExecuteChangedFor(nameof(EndSelectedProcessCommand))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyPropertyChangedFor(nameof(SearchOrStopButtonText))]
    private bool isSearching;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    [NotifyCanExecuteChangedFor(nameof(SearchOrStopCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseCommand))]
    [NotifyCanExecuteChangedFor(nameof(EndSelectedProcessCommand))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    private bool isEndingProcess;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EndSelectedProcessCommand))]
    private HandleLockResultModel? selectedResult;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    public IResourceWrapper ResourceWrapper => resourceWrapper;

    public bool IsIdle => !IsSearching && !IsEndingProcess;

    public bool IsBusy => IsSearching || IsEndingProcess;

    public string SearchOrStopButtonText => resourceWrapper.Get(IsSearching ? UiTextKey.StopButton : UiTextKey.SearchButton);

    public bool HasResults => Results.Count > 0;

    public bool HasWarnings => Warnings.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsDirectorySearchMode => SelectedSearchMode.Mode == HandleSearchMode.Directory;

    public HomeViewModel(IHandleSearchService handleSearchService,
                         IProcessActionService processActionService,
                         ILogger logger,
                         IResourceWrapper resourceWrapper,
                         IProcessTerminationConfirmationService processTerminationConfirmationService)
    {
        this.handleSearchService = handleSearchService;
        this.processActionService = processActionService;
        this.logger = logger.ForContext<HomeViewModel>();
        this.resourceWrapper = resourceWrapper;
        this.processTerminationConfirmationService = processTerminationConfirmationService;
        var fileMode = new HandleSearchModeOption(resourceWrapper.Get(UiTextKey.FileSearchModeDisplayName), HandleSearchMode.File);
        SearchModes = [fileMode,
                       new HandleSearchModeOption(resourceWrapper.Get(UiTextKey.DirectorySearchModeDisplayName), HandleSearchMode.Directory)];
        selectedSearchMode = fileMode;
        statusMessage = resourceWrapper.Get(UiTextKey.InitialStatusMessage);
    }

    /// <summary>Uses a dropped file or folder as the next search target.</summary>
    public bool TrySetDroppedPath(string path)
    {
        if (!IsIdle)
        {
            return false;
        }

        HandleSearchMode mode;
        if (Directory.Exists(path))
        {
            mode = HandleSearchMode.Directory;
        }
        else if (File.Exists(path))
        {
            mode = HandleSearchMode.File;
        }
        else
        {
            return false;
        }

        SelectedSearchMode = SearchModes.Single(option => option.Mode == mode);
        TargetPath = path;
        return true;
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

    [RelayCommand(CanExecute = nameof(CanSearchOrStop))]
    private void SearchOrStop()
    {
        if (IsSearching)
        {
            searchCancellation?.Cancel();
            return;
        }

        SearchCommand.Execute(null);
    }

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        if (lastSearchRequest is null)
        {
            return;
        }

        await RunSearchAsync(lastSearchRequest, rememberRequest: false);
    }

    [RelayCommand(CanExecute = nameof(CanBrowse))]
    private void Browse()
    {
        if (SelectedSearchMode.Mode == HandleSearchMode.Directory)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = resourceWrapper.Get(UiTextKey.SelectTargetFolderDescription),
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
            Title = resourceWrapper.Get(UiTextKey.SelectTargetFileTitle)
        };

        if (fileDialog.ShowDialog() == true)
        {
            TargetPath = fileDialog.FileName;
        }
    }

    [RelayCommand(CanExecute = nameof(CanEndSelectedProcess))]
    private async Task EndSelectedProcessAsync()
    {
        if (SelectedResult is null)
        {
            return;
        }

        var selectedItem = SelectedResult;
        var selected = selectedItem.LockInfo;
        try
        {
            if (!processTerminationConfirmationService.Confirm(selected.ProcessName, selected.ProcessId))
            {
                return;
            }

            IsEndingProcess = true;
            ErrorMessage = null;
            StatusMessage = string.Format(resourceWrapper.Get(UiTextKey.EndingProcessStatusFormat), selected.ProcessName);

            await Dispatcher.Yield(DispatcherPriority.Background);

            var result = await processActionService.CloseProcessAsync(selected.ProcessId, CancellationToken.None);
            StatusMessage = result.Message;

            if (!result.Succeeded)
            {
                ErrorMessage = result.Message;
                return;
            }

            selectedItem.PropertyChanged -= OnResultPropertyChanged;
            Results.Remove(selectedItem);
            SelectedResult = null;
            NotifyCollectionStateChanged();

            if (lastSearchRequest is not null)
            {
                await RunSearchAsync(lastSearchRequest, rememberRequest: false);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, LogConstants.EndProcessFailedLogTemplate, selected.ProcessId);
            ErrorMessage = resourceWrapper.Get(UiTextKey.EndProcessFailedUserMessage);
            StatusMessage = ErrorMessage;
        }
        finally
        {
            IsEndingProcess = false;
        }
    }

    private async Task RunSearchAsync(HandleSearchRequest request, bool rememberRequest)
    {
        searchCancellation?.Cancel();
        var searchCancellationTemp = new CancellationTokenSource();
        searchCancellation = searchCancellationTemp;

        ClearResults();
        Warnings.Clear();
        ErrorMessage = null;
        SelectedResult = null;
        NotifyCollectionStateChanged();

        try
        {
            IsSearching = true;
            StatusMessage = resourceWrapper.Get(UiTextKey.SearchInProgressStatusMessage);

            // Let WPF render the busy state before a fast search can complete.
            await Dispatcher.Yield(DispatcherPriority.Background);

            if (rememberRequest)
            {
                lastSearchRequest = request;
                RefreshCommand.NotifyCanExecuteChanged();
            }

            logger.Information(
                LogConstants.StartingHandleSearchLogTemplate,
                request.TargetPath,
                request.SearchMode,
                request.IncludeChildren);

            var result = await handleSearchService.SearchAsync(request, searchCancellation.Token);
            searchCancellation.Token.ThrowIfCancellationRequested();

            foreach (var item in result.Items)
            {
                var resultItem = new HandleLockResultModel(item);
                resultItem.PropertyChanged += OnResultPropertyChanged;
                Results.Add(resultItem);
            }

            foreach (var warning in result.Warnings)
            {
                Warnings.Add(warning);
            }

            NotifyCollectionStateChanged();

            StatusMessage = result.Items.Count == 0
                ? resourceWrapper.Get(UiTextKey.NoLockingProcessesFoundStatusMessage)
                : string.Format(resourceWrapper.Get(UiTextKey.LockingProcessEntriesFoundFormat), result.Items.Count);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = resourceWrapper.Get(UiTextKey.SearchCancelledStatusMessage);
        }
        catch (Exception ex)
        {
            logger.Error(ex, LogConstants.SearchFailedLogTemplate, request.TargetPath);
            ErrorMessage = resourceWrapper.Get(UiTextKey.SearchFailedUserMessage);
            StatusMessage = ErrorMessage;
        }
        finally
        {
            IsSearching = false;
            searchCancellation.Dispose();
            searchCancellation = null;
            RefreshCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSearch()
    {
        return !IsSearching && !IsEndingProcess && !string.IsNullOrWhiteSpace(TargetPath);
    }

    private bool CanSearchOrStop()
    {
        return !IsEndingProcess && (IsSearching || CanSearch());
    }

    private bool CanRefresh()
    {
        return !IsSearching && !IsEndingProcess && lastSearchRequest is not null;
    }

    private bool CanBrowse()
    {
        return !IsSearching && !IsEndingProcess;
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

    private void OnResultPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(HandleLockResultModel.IsChecked) || sender is not HandleLockResultModel item)
        {
            return;
        }

        if (item.IsChecked)
        {
            if (SelectedResult is { } previous && !ReferenceEquals(previous, item))
            {
                previous.IsChecked = false;
            }

            SelectedResult = item;
        }
        else if (ReferenceEquals(SelectedResult, item))
        {
            SelectedResult = null;
        }
    }

    private void ClearResults()
    {
        foreach (var item in Results)
        {
            item.PropertyChanged -= OnResultPropertyChanged;
        }

        Results.Clear();
    }
}

/// <summary>Describes a selectable search mode.</summary>
public sealed record HandleSearchModeOption(string DisplayName, HandleSearchMode Mode);
