#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Budget_Jona_Inlämning.Models;
using Budget_Jona_Inlämning.Services;
using Budget_Jona_Inlämning.Views;

namespace Budget_Jona_Inlämning.ViewModels;

/// <summary>
/// Represents a view model for managing and editing categories within the application.
/// </summary>
/// <remarks>CategoryViewModel provides commands and properties for loading, adding, editing, and deleting
/// categories. It interacts with category and dialog services to support user-driven category management workflows.
/// This class is intended for use in UI scenarios where categories are displayed and modified. The class raises the
/// CloseRequested event to signal when the associated dialog should be closed, and exposes the DialogResult property to
/// indicate the outcome of dialog operations.</remarks>
public sealed partial class CategoryViewModel : BaseViewModel
{
    private readonly ICategoryService _categoryService;
    private readonly IDialogService _dialogService;

    
    [ObservableProperty]
    private Category model = new();

    public ObservableCollection<Category> Categories { get; } = new();

    public event EventHandler? CloseRequested;
    public bool? DialogResult { get; private set; }

    public CategoryViewModel(ICategoryService categoryService, IDialogService dialogService)
    {
        this._categoryService = categoryService;
        this._dialogService = dialogService;
    }

    
    public CategoryViewModel(ICategoryService categoryService)
        : this(categoryService, null!)
    {
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (this.IsBusy) return;

        try
        {
            this.IsBusy = true;
            List<Category> cats = await this._categoryService.GetAllAsync().ConfigureAwait(false);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                this.Categories.Clear();
                foreach (Category c in cats) this.Categories.Add(c);
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
        CategoryEditorViewModel editor = new CategoryEditorViewModel(this._categoryService);
        CategoryEditView view = new CategoryEditView();

        Nullable<bool> result = await this._dialogService.ShowDialogAsync(view, editor);
        if (result == true)
        {
            await this.LoadAsync();
        }
    }

    [RelayCommand]
    private async Task EditAsync(Category? category)
    {
        if (category is null) return;

        Category entity = await this._categoryService.GetByIdAsync(category.Id).ConfigureAwait(false);
        if (entity is null) return;

        CategoryEditorViewModel editor = new CategoryEditorViewModel(this._categoryService);
        // initialize editor model
        editor.Model.Id = entity.Id;
        editor.Model.Name = entity.Name;
        editor.Model.Type = entity.Type;

        CategoryEditView view = new CategoryEditView();
        Nullable<bool> result = await this._dialogService.ShowDialogAsync(view, editor);
        if (result == true)
        {
            await this.LoadAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Category? category)
    {
        if (category is null) return;

        MessageBoxResult answer = MessageBox.Show($"Delete category '{category.Name}'? This will not delete transactions.", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        await this._categoryService.DeleteAsync(category.Id).ConfigureAwait(false);

        Application.Current?.Dispatcher.Invoke(() => this.Categories.Remove(category));
    }
}