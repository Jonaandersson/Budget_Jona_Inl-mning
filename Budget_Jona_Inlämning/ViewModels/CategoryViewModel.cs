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

public sealed class CategoryViewModel : BaseViewModel, IDialogRequestClose, IHaveDialogResult
{
    private readonly ICategoryService _categoryService;
    private readonly IDialogService _dialogService;

    // Editor model
    public Category Model { get; }

    // List collection
    public ObservableCollection<Category> Categories { get; } = new();

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }

    // List commands
    public IRelayCommand LoadCommand { get; }
    public IRelayCommand AddCommand { get; }
    public IRelayCommand<Category?> EditCommand { get; }
    public IRelayCommand<Category?> DeleteCommand { get; }

    public event EventHandler? CloseRequested;
    public bool? DialogResult { get; private set; }

    // Constructor used by DI for list VM (dialogService injected)
    public CategoryViewModel(ICategoryService categoryService, IDialogService dialogService)
    {
        this._categoryService = categoryService;
        this._dialogService = dialogService;

        this.Model = new Category();

        // Editor commands
        this.SaveCommand = new AsyncRelayCommand(this.SaveAsync);
        this.CancelCommand = new RelayCommand(this.Cancel);

        // List commands
        this.LoadCommand = new AsyncRelayCommand(this.LoadAsync);
        this.AddCommand = new AsyncRelayCommand(this.AddAsync);
        this.EditCommand = new AsyncRelayCommand<Category?>(this.EditAsync);
        this.DeleteCommand = new AsyncRelayCommand<Category?>(this.DeleteAsync);
    }

    // Convenience constructor to create an editor VM manually (dialog use)
    public CategoryViewModel(ICategoryService categoryService)
        : this(categoryService, null!)
    {
        // Note: DI should use the two-arg ctor. This ctor exists to allow manual creation
    }

    public async Task LoadAsync()
    {
        if (this.IsBusy)
        {
            return;
        }

        try
        {
            this.IsBusy = true;

            var cats = await this._categoryService.GetAllAsync().ConfigureAwait(false);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                this.Categories.Clear();
                foreach (var c in cats)
                {
                    this.Categories.Add(c);
                }
            });
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    // List: open editor to add
    private async Task AddAsync()
    {
        // Create a fresh editor VM (use DI-resolved dialog service)
        var editor = new CategoryViewModel(this._categoryService, this._dialogService);
        var view = new CategoryEditView();

        var result = await this._dialogService.ShowDialogAsync(view, editor).ConfigureAwait(false);
        if (result == true)
        {
            await this.LoadAsync().ConfigureAwait(false);
        }
    }

    // List: open editor to edit
    private async Task EditAsync(Category? category)
    {
        if (category is null) return;

        var entity = await this._categoryService.GetByIdAsync(category.Id).ConfigureAwait(false);
        if (entity is null) return;

        var editor = new CategoryViewModel(this._categoryService, this._dialogService, entity);
        var view = new CategoryEditView();

        var result = await this._dialogService.ShowDialogAsync(view, editor).ConfigureAwait(false);
        if (result == true)
        {
            await this.LoadAsync().ConfigureAwait(false);
        }
    }

    // Optional constructor overload to initialize editor with existing entity
    public CategoryViewModel(ICategoryService categoryService, IDialogService dialogService, Category existing)
        : this(categoryService, dialogService)
    {
        this.Model.Id = existing.Id;
        this.Model.Name = existing.Name;
        this.Model.Type = existing.Type;
    }

    private async Task DeleteAsync(Category? category)
    {
        if (category is null) return;

        var answer = MessageBox.Show($"Delete category '{category.Name}'? This will not delete transactions.", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        await this._categoryService.DeleteAsync(category.Id).ConfigureAwait(false);

        Application.Current?.Dispatcher.Invoke(() =>
        {
            this.Categories.Remove(category);
        });
    }

    // Editor save
    private async Task SaveAsync()
    {
        if (this.IsBusy)
        {
            return;
        }

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
            this.CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    private void Cancel()
    {
        this.DialogResult = false;
        this.CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}