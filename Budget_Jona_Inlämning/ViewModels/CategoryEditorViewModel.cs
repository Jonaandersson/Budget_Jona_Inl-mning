#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Budget_Jona_Inlämning.Models;
using Budget_Jona_Inlämning.Services;

namespace Budget_Jona_Inlämning.ViewModels;

/// <summary>
/// Represents the view model for editing a category, providing properties and commands to manage category data and
/// dialog interactions.
/// </summary>
/// <remarks>This view model coordinates the editing workflow for a category, including saving changes and
/// handling dialog closure requests. It exposes commands for saving and canceling edits, and raises the <see
/// cref="CloseRequested"/> event to signal when the dialog should be closed. The <see cref="DialogResult"/> property
/// indicates the outcome of the dialog operation. This class is intended for use in UI scenarios where category data is
/// edited within a modal dialog.</remarks>
public sealed partial class CategoryEditorViewModel : BaseViewModel, IDialogRequestClose, IHaveDialogResult
{
    private readonly ICategoryService _categoryService;

    public event EventHandler? CloseRequested;

    
    [ObservableProperty]
    private Category model = new();

    public bool? DialogResult { get; private set; }

    public CategoryEditorViewModel(ICategoryService categoryService)
    {
        this._categoryService = categoryService;
    }

    [RelayCommand]
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

    [RelayCommand]
    private void Cancel()
    {
        this.DialogResult = false;
        Application.Current?.Dispatcher.Invoke(() => this.CloseRequested?.Invoke(this, EventArgs.Empty));
    }
}