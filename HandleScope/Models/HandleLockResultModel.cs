using CommunityToolkit.Mvvm.ComponentModel;
using HandleScope.Services.Models;

namespace HandleScope.Models;

/// <summary>Provides selection state for a handle search result.</summary>
public sealed partial class HandleLockResultModel : ObservableObject
{
    public HandleLockInfo LockInfo { get; }

    [ObservableProperty]
    private bool isChecked;

    public HandleLockResultModel(HandleLockInfo lockInfo) => LockInfo = lockInfo;
}
