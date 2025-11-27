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

public sealed class TransactionEditorViewModel : BaseViewModel, IDialogRequestClose, IHaveDialogResult
{
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IDialogService _dialogService;
    private readonly Func<CategoryEditorViewModel> _categoryEditorFactory;

    public event EventHandler? CloseRequested;

    public Transaction Model { get; }

    public ObservableCollection<Category> Categories { get; } = new();

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand AddCategoryCommand { get; }

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

        this.Model = new Transaction { Date = DateTime.Today };
        this.SaveCommand = new AsyncRelayCommand(this.SaveAsync);
        this.CancelCommand = new RelayCommand(this.Cancel);
        this.AddCategoryCommand = new AsyncRelayCommand(this.AddCategoryAsync);
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
        var cats = await this._categoryService.GetAllAsync().ConfigureAwait(false);

        // Update collection on UI thread
        Application.Current?.Dispatcher.Invoke(() =>
        {
            this.Categories.Clear();
            foreach (var c in cats)
            {
                this.Categories.Add(c);
            }
        });
    }

    private async Task AddCategoryAsync()
    {
        var editor = this._categoryEditorFactory();
        var view = new CategoryEditView();

        // show dialog; do not ConfigureAwait(false) so UI flow is preserved
        var result = await this._dialogService.ShowDialogAsync(view, editor);
        if (result == true)
        {
            // reload categories on UI
            await this.LoadCategoriesAsync();
        }
    }

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

            this.DialogResult = true;
            // raise CloseRequested on UI thread
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