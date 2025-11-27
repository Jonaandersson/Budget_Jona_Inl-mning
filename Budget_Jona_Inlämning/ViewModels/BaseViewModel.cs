#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace Budget_Jona_Inlämning.ViewModels;

public abstract class BaseViewModel : ObservableObject
{
    private bool _isBusy;

    public bool IsBusy
    {
        get => this._isBusy;
        set => this.SetProperty(ref this._isBusy, value);
    }
}