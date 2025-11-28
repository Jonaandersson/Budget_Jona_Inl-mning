#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;

namespace Budget_Jona_Inlämning.ViewModels;

/// <summary>
/// Provides a base class for view models that supports property change notification and busy state tracking.
/// </summary>
/// <remarks>Inherit from this class to implement view models that require notification of property changes and a
/// standard mechanism for indicating when the view model is performing a long-running operation. The busy state can be
/// used to control UI elements such as loading indicators or to prevent user interaction during background
/// tasks.</remarks>
public abstract class BaseViewModel : ObservableObject
{
    private bool _isBusy;

    public bool IsBusy
    {
        get => this._isBusy;
        set => this.SetProperty(ref this._isBusy, value);
    }
}