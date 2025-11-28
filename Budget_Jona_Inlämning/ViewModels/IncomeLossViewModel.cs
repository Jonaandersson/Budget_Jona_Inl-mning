#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Budget_Jona_Inlämning.Models;
using Budget_Jona_Inlämning.Services;
using Budget_Jona_Inlämning.Views;

namespace Budget_Jona_Inlämning.ViewModels;

/// <summary>
/// Represents the view model for managing income loss records and dialog interactions within the application.
/// </summary>
/// <remarks>This class provides functionality to load, add, delete, and save income loss entries, as well as to
/// handle dialog result and close requests. It is intended for use in UI scenarios where users can view and edit income
/// loss data. The class is sealed and partially generated, and implements dialog-related interfaces to support modal
/// workflows.</remarks>
public sealed partial class IncomeLossViewModel : BaseViewModel, IDialogRequestClose, IHaveDialogResult
{
    private readonly IIncomeLossService _incomeLossService;
    private readonly IDialogService _dialogService;

    
    [ObservableProperty]
    private IncomeLoss model = new IncomeLoss { Date = DateTime.Today, RefundPercentage = 0.80m };

    public ObservableCollection<IncomeLoss> IncomeLosses { get; } = new();

    public bool? DialogResult { get; private set; }
    public event EventHandler? CloseRequested;

    public IncomeLossViewModel(IIncomeLossService incomeLossService, IDialogService dialogService)
    {
        this._incomeLossService = incomeLossService;
        this._dialogService = dialogService;
    }

    public IncomeLossViewModel(IIncomeLossService incomeLossService)
        : this(incomeLossService, null!)
    {
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (this.IsBusy) return;

        try
        {
            this.IsBusy = true;
            List<IncomeLoss> list = await this._incomeLossService.GetAllAsync().ConfigureAwait(false);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                this.IncomeLosses.Clear();
                foreach (IncomeLoss i in list) this.IncomeLosses.Add(i);
            });
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        IncomeLossViewModel editor = new IncomeLossViewModel(this._incomeLossService, this._dialogService);
        IncomeLossEditView view = new IncomeLossEditView();

        Nullable<bool> result = await this._dialogService.ShowDialogAsync(view, editor);
        if (result == true)
        {
            await this.LoadAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(IncomeLoss? item)
    {
        if (item is null) return;

        await this._incomeLossService.DeleteAsync(item.Id).ConfigureAwait(false);

        Application.Current?.Dispatcher.Invoke(() => this.IncomeLosses.Remove(item));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (this.IsBusy) return;

        try
        {
            this.IsBusy = true;

            if (this.Model.Id == 0) await this._incomeLossService.AddAsync(this.Model).ConfigureAwait(false);
            else await this._incomeLossService.UpdateAsync(this.Model).ConfigureAwait(false);

            this.DialogResult = true;
            Application.Current?.Dispatcher.Invoke(() => this.CloseRequested?.Invoke(this, EventArgs.Empty));
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        this.DialogResult = false;
        Application.Current?.Dispatcher.Invoke(() => this.CloseRequested?.Invoke(this, EventArgs.Empty));
    }

    // Compute method unchanged
    public decimal ComputeAdjustmentForMonths(params (int Year, int Month)[] months)
    {
        if (months is null || months.Length == 0) return 0m;

        var set = months.ToHashSet();
        return this.IncomeLosses.Where(i => set.Contains((i.Date.Year, i.Date.Month))).Sum(i => (i.AmountLost - i.RefundAmount));
    }
}