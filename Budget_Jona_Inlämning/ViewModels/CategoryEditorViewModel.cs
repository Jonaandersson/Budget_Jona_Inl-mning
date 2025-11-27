#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Budget_Jona_Inlämning.Models;
using Budget_Jona_Inlämning.Services;

namespace Budget_Jona_Inlämning.ViewModels;

public sealed class CategoryEditorViewModel : BaseViewModel, IDialogRequestClose, IHaveDialogResult
{
    private readonly ICategoryService _categoryService;

    public event EventHandler? CloseRequested;

    public Category Model { get; } = new();

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public bool? DialogResult { get; private set; }

    public CategoryEditorViewModel(ICategoryService categoryService)
    {
        this._categoryService = categoryService;
        this.SaveCommand = new AsyncRelayCommand(this.SaveAsync);
        this.CancelCommand = new RelayCommand(this.Cancel);
    }

    private async Task SaveAsync()
    {
        if (this.IsBusy) return;

        try
        {
            this.IsBusy = true;

            if (this.Model.Id == 0)
            {
                await this._categoryService.AddAsync(this.Model).ConfigureAwait(false);
            }
            else
            {
                await this._categoryService.UpdateAsync(this.Model).ConfigureAwait(false);
            }

            this.DialogResult = true;
            Application.Current?.Dispatcher.Invoke(() => this.CloseRequested?.Invoke(this, EventArgs.Empty));
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    private void Cancel()
    {
        this.DialogResult = false;
        Application.Current?.Dispatcher.Invoke(() => this.CloseRequested?.Invoke(this, EventArgs.Empty));
    }
}