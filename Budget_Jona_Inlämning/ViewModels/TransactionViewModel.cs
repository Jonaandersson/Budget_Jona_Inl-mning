#nullable enable
using Budget_Jona_Inlämning.Models;
using Budget_Jona_Inlämning.Services;
using Budget_Jona_Inlämning.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualBasic;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Budget_Jona_Inlämning.ViewModels;
/// <summary>
//  Represents the view model for managing and displaying a collection of financial transactions, including support for
//  loading, adding, editing, and deleting transactions within the application's user interface.
/// </summary>
/// <remarks>TransactionViewModel provides properties and commands for interacting with transaction data,
/// including calculating income, expenses, and net totals. It coordinates with services for data access, category
/// management, and dialog presentation. This view model is typically used in MVVM scenarios to bind transaction data to
/// UI elements and handle user actions related to transaction management.</remarks>
public sealed partial class TransactionViewModel : BaseViewModel
{
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IDialogService _dialogService;
    private readonly Func<TransactionEditorViewModel> _editorFactory;

    public ObservableCollection<Transaction> Transactions { get; } = new();

    [ObservableProperty]
    private Transaction? selectedTransaction;

    [ObservableProperty]
    private decimal incomeTotal;

    [ObservableProperty]
    private decimal expenseTotal;

    public decimal NetTotal => this.IncomeTotal - this.ExpenseTotal;

    public TransactionViewModel(
        ITransactionService transactionService,
        ICategoryService categoryService,
        IDialogService dialogService,
        Func<TransactionEditorViewModel> editorFactory)
    {
        this._transactionService = transactionService;
        this._categoryService = categoryService;
        this._dialogService = dialogService;
        this._editorFactory = editorFactory;
    }

    // Keep behavior: when totals change, notify NetTotal
    partial void OnIncomeTotalChanged(decimal value)
    {
        this.OnPropertyChanged(nameof(NetTotal));
    }

    partial void OnExpenseTotalChanged(decimal value)
    {
        this.OnPropertyChanged(nameof(NetTotal));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (this.IsBusy) return;

        try
        {
            this.IsBusy = true;
            List<Transaction> list = await this._transactionService.GetAllAsync().ConfigureAwait(false);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                this.Transactions.Clear();
                foreach (Transaction t in list) this.Transactions.Add(t);
            });

            this.IncomeTotal = this.Transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            this.ExpenseTotal = this.Transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        TransactionEditorViewModel editor = this._editorFactory();
        await editor.LoadCategoriesAsync();

        TransactionEditView view = new TransactionEditView();
        Nullable<bool> result = await this._dialogService.ShowDialogAsync(view, editor);
        if (result == true) await this.LoadAsync();
    }

    [RelayCommand]
    private async Task EditAsync(Transaction? t)
    {
        if (t is null) return;

        Transaction entity = await this._transactionService.GetByIdAsync(t.Id).ConfigureAwait(false);
        if (entity is null) return;

        TransactionEditorViewModel editor = this._editorFactory();

        // populate editor model
        editor.Model.Id = entity.Id;
        editor.Model.Amount = entity.Amount;
        editor.Model.Date = entity.Date;
        editor.Model.Description = entity.Description;
        editor.Model.IsMonthly = entity.IsMonthly;
        editor.Model.IsIncome = entity.IsIncome;
        editor.Model.CategoryId = entity.CategoryId;

        await editor.LoadCategoriesAsync();

        TransactionEditView view = new TransactionEditView();
        Nullable<bool> result = await this._dialogService.ShowDialogAsync(view, editor);
        if (result == true) await this.LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(Transaction? t)
    {
        if (t is null) return;

        MessageBoxResult answer = MessageBox.Show($"Delete transaction '{t.Description}'?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        await this._transactionService.DeleteAsync(t.Id).ConfigureAwait(false);

        Application.Current?.Dispatcher.Invoke(() => this.Transactions.Remove(t));

        this.IncomeTotal = this.Transactions.Where(x => x.IsIncome).Sum(x => x.Amount);
        this.ExpenseTotal = this.Transactions.Where(x => !x.IsIncome).Sum(x => x.Amount);
    }
}