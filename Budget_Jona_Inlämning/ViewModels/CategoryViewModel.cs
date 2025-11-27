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

public sealed partial class CategoryViewModel : BaseViewModel
{
    private readonly ICategoryService _categoryService;
    private readonly IDialogService _dialogService;

    // Generated property: public Category Model { get; set; }
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

    // Convenience ctor for editor usage if needed
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
            var cats = await this._categoryService.GetAllAsync().ConfigureAwait(false);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                this.Categories.Clear();
                foreach (var c in cats) this.Categories.Add(c);
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
        var editor = new CategoryEditorViewModel(this._categoryService);
        var view = new CategoryEditView();

        var result = await this._dialogService.ShowDialogAsync(view, editor);
        if (result == true)
        {
            await this.LoadAsync();
        }
    }

    [RelayCommand]
    private async Task EditAsync(Category? category)
    {
        if (category is null) return;

        var entity = await this._categoryService.GetByIdAsync(category.Id).ConfigureAwait(false);
        if (entity is null) return;

        var editor = new CategoryEditorViewModel(this._categoryService);
        // initialize editor model
        editor.Model.Id = entity.Id;
        editor.Model.Name = entity.Name;
        editor.Model.Type = entity.Type;

        var view = new CategoryEditView();
        var result = await this._dialogService.ShowDialogAsync(view, editor);
        if (result == true)
        {
            await this.LoadAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Category? category)
    {
        if (category is null) return;

        var answer = MessageBox.Show($"Delete category '{category.Name}'? This will not delete transactions.", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        await this._categoryService.DeleteAsync(category.Id).ConfigureAwait(false);

        Application.Current?.Dispatcher.Invoke(() => this.Categories.Remove(category));
    }

    // Editor save/cancel kept in CategoryEditorViewModel; this class is the list VM.
}