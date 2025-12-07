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
/// Represents the view model for editing a transaction, providing data binding and command logic for transaction
/// creation or modification dialogs.
/// </summary>
/// <remarks>This view model manages the transaction editing workflow, including loading available categories,
/// handling dialog results, and coordinating with related services. It exposes commands for saving, canceling, and
/// adding categories, and raises the CloseRequested event to signal dialog closure. The view model is intended for use
/// in UI scenarios where transactions are created or edited interactively.</remarks>
public sealed partial class TransactionEditorViewModel : BaseViewModel, IDialogRequestClose, IHaveDialogResult
{
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IDialogService _dialogService;
    private readonly Func<CategoryEditorViewModel> _categoryEditorFactory;

    public event EventHandler? CloseRequested;

    // Generated property: public Transaction Model { get; set; }
    [ObservableProperty]
    private Transaction model = new Transaction { Date = DateTime.Today };

    public ObservableCollection<Category> Categories { get; } = new();

    public bool? DialogResult { get; private set; }

    public TransactionEditorViewModel(
        ITransactionService transactionService,
        ICategoryService categoryService,
        IDialogService dialogService,
        Func<CategoryEditorViewModel> categoryEditorFactory)
    {
        this._transactionService = transactionService;
        this._categoryService = categoryService;
        this._dialogService = dialogService;
        this._categoryEditorFactory = categoryEditorFactory;
    }

    public TransactionEditorViewModel(
        ITransactionService transactionService,
        ICategoryService categoryService,
        IDialogService dialogService,
        Func<CategoryEditorViewModel> categoryEditorFactory,
        Transaction existing)
        : this(transactionService, categoryService, dialogService, categoryEditorFactory)
    {
        this.Model.Id = existing.Id;
        this.Model.Amount = existing.Amount;
        this.Model.Date = existing.Date;
        this.Model.Description = existing.Description;
        this.Model.IsMonthly = existing.IsMonthly;
        this.Model.IsIncome = existing.IsIncome;
        this.Model.CategoryId = existing.CategoryId;
    }

    public async Task LoadCategoriesAsync()
    {
        List<Category> cats = await this._categoryService.GetAllAsync().ConfigureAwait(false);

        // Update collection on UI thread
        Application.Current?.Dispatcher.Invoke(() =>
        {
            this.Categories.Clear();
            foreach (Category c in cats)
            {
                this.Categories.Add(c);
            }
        });
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        CategoryEditorViewModel editor = this._categoryEditorFactory();
        CategoryEditView view = new CategoryEditView();

        // show dialog; keep context so UI flow is preserve
        Nullable<bool> result = await this._dialogService.ShowDialogAsync(view, editor);
        if (result == true)
        {
            // reload categories on UI
            await this.LoadCategoriesAsync();
        }
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
                await this._transactionService.AddAsync(this.Model).ConfigureAwait(false);
            }
            else
            {
                await this._transactionService.UpdateAsync(this.Model).ConfigureAwait(false);
            }

            RequestClose(true);
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose(false);
    }

    private void RequestClose(bool result)
    {
        this.DialogResult = result;
        Application.Current?.Dispatcher.Invoke(() => this.CloseRequested?.Invoke(this, EventArgs.Empty));
    }
}