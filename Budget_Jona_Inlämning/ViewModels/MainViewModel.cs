#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Budget_Jona_Inlämning.Models;

namespace Budget_Jona_Inlämning.ViewModels;

/// <summary>
/// Represents the main view model for the application's dashboard, providing access to transactions, categories,
/// income/loss data, and projection calculations for the selected month. Serves as the central data context for UI
/// views, aggregating and coordinating child view models.
/// </summary>
/// <remarks>MainViewModel exposes collections and properties for binding in UI views, including filtered
/// transactions, category lists, and income/loss details. It coordinates loading and updating of child view models to
/// ensure data consistency and UI responsiveness. Projection properties are automatically updated when underlying data
/// changes. All property and collection changes are dispatched to the UI thread to maintain thread safety for UI-bound
/// data. Sequential loading of child view models is performed to avoid database concurrency issues.</remarks>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly TransactionViewModel _transactionsVm;
    private readonly CategoryViewModel _categoriesVm;
    private readonly IncomeLossViewModel _incomeLossesVm;

    [ObservableProperty]
    private decimal projectedIncome;

    [ObservableProperty]
    private decimal projectedExpense;

    [ObservableProperty]
    private decimal projectedNet;

    [ObservableProperty]
    private decimal incomeLossAdjustment;

    [ObservableProperty]
    private DateTime selectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private ObservableCollection<Transaction> filteredTransactions = new();

    [ObservableProperty]
    private ObservableCollection<IncomeLoss> currentIncomeLosses = new();

    [ObservableProperty]
    private ObservableCollection<IncomeLoss> projectedIncomeLosses = new();

    public MainViewModel(
        TransactionViewModel transactionsVm,
        CategoryViewModel categoriesVm,
        IncomeLossViewModel incomeLossesVm)
    {
        this._transactionsVm = transactionsVm;
        this._categoriesVm = categoriesVm;
        this._incomeLossesVm = incomeLossesVm;

        if (this._transactionsVm is INotifyPropertyChanged txNotify)
        {
            txNotify.PropertyChanged += this.Child_PropertyChanged;
        }

        if (this._categoriesVm is INotifyPropertyChanged cNotify)
        {
            cNotify.PropertyChanged += this.Child_PropertyChanged;
        }

        if (this._incomeLossesVm is INotifyPropertyChanged ilNotify)
        {
            ilNotify.PropertyChanged += this.Child_PropertyChanged;
        }

        this._transactionsVm.Transactions.CollectionChanged += this.Transactions_CollectionChanged;

        this._incomeLossesVm.IncomeLosses.CollectionChanged += this.IncomeLosses_CollectionChanged;
    }

    partial void OnSelectedMonthChanged(DateTime value)
    {
        this.OnPropertyChanged(nameof(this.SelectedMonthLabel));
        this.UpdateFilteredTransactions();
    }

    private void Transactions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.UpdateFilteredTransactions();
    }

    private void IncomeLosses_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.ComputeProjections();
    }

    public ObservableCollection<Transaction> Transactions => this._transactionsVm.Transactions;

    public Transaction? SelectedTransaction
    {
        get => this._transactionsVm.SelectedTransaction;
        set => this._transactionsVm.SelectedTransaction = value;
    }

    public ObservableCollection<Category> Categories => this._categoriesVm.Categories;
    public ObservableCollection<IncomeLoss> IncomeLosses => this._incomeLossesVm.IncomeLosses;

    public decimal IncomeTotal => this._transactionsVm.IncomeTotal;
    public decimal ExpenseTotal => this._transactionsVm.ExpenseTotal;
    public decimal NetTotal => this.IncomeTotal - this.ExpenseTotal;

    public string SelectedMonthLabel => this.SelectedMonth.ToString("MMMM yyyy");

    [RelayCommand]
    public async Task LoadAllAsync()
    {
        // Load categories, transactions, incomeloss sequentially
        await this._categoriesVm.LoadAsync().ConfigureAwait(false);
        await this._transactionsVm.LoadAsync().ConfigureAwait(false);
        await this._incomeLossesVm.LoadAsync().ConfigureAwait(false);

        // Compute projection and update lists on UI thread
        this.ComputeProjections();
        this.NotifyTotals();

        // Update filtered transactions for the selected month
        this.UpdateFilteredTransactions();
    }

    private void ComputeProjections()
    {
        DateTime today = DateTime.Today;
        DateTime nextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);

        Decimal recurringIncome = this._transactionsVm.Transactions.Where(t => t.IsMonthly && t.IsIncome).Sum(t => t.Amount);
        Decimal recurringExpense = this._transactionsVm.Transactions.Where(t => t.IsMonthly && !t.IsIncome).Sum(t => t.Amount);

        Decimal oneTimeIncomeNext = this._transactionsVm.Transactions.Where(t => !t.IsMonthly && t.IsIncome && t.Date.Year == nextMonth.Year && t.Date.Month == nextMonth.Month).Sum(t => t.Amount);
        Decimal oneTimeExpenseNext = this._transactionsVm.Transactions.Where(t => !t.IsMonthly && !t.IsIncome && t.Date.Year == nextMonth.Year && t.Date.Month == nextMonth.Month).Sum(t => t.Amount);

        // Refresh IncomeLoss lists for current/next month
        DateTime todayDate = DateTime.Today;
        var currentMonthSet = (todayDate.Year, todayDate.Month);
        var nextMonthSet = (nextMonth.Year, nextMonth.Month);

        List<IncomeLoss> currentItems = this._incomeLossesVm.IncomeLosses
            .Where(i => (i.Date.Year, i.Date.Month) == currentMonthSet)
            .OrderByDescending(i => i.Date)
            .ToList();

        List<IncomeLoss> projectedItems = this._incomeLossesVm.IncomeLosses
            .Where(i => (i.Date.Year, i.Date.Month) == nextMonthSet || (i.Date.Year, i.Date.Month) == currentMonthSet)
            .OrderByDescending(i => i.Date)
            .ToList();

        // (AmountLost - RefundAmount)
        Decimal adjustment = this._incomeLossesVm.ComputeAdjustmentForMonths(currentMonthSet, nextMonthSet);
        Decimal netLoss = Math.Max(0m, adjustment);

        this.IncomeLossAdjustment = netLoss;
        this.ProjectedIncome = recurringIncome + oneTimeIncomeNext - netLoss;
        this.ProjectedExpense = recurringExpense + oneTimeExpenseNext;
        this.ProjectedNet = this.ProjectedIncome - this.ProjectedExpense;

        // Update collections on UI 
        Application.Current?.Dispatcher.Invoke(() =>
        {
            this.CurrentIncomeLosses.Clear();
            foreach (IncomeLoss i in currentItems) this.CurrentIncomeLosses.Add(i);

            this.ProjectedIncomeLosses.Clear();
            foreach (IncomeLoss i in projectedItems) this.ProjectedIncomeLosses.Add(i);
        });
    }

    // Update filtered transactions for the current month
    private void UpdateFilteredTransactions()
    {
        Int32 year = this.SelectedMonth.Year;
        Int32 month = this.SelectedMonth.Month;

        List<Transaction> items = this._transactionsVm.Transactions
            .Where(t => t.Date.Year == year && t.Date.Month == month)
            .OrderByDescending(t => t.Date)
            .ToList();

        Application.Current?.Dispatcher.Invoke(() =>
        {
            this.FilteredTransactions.Clear();
            foreach (Transaction t in items) this.FilteredTransactions.Add(t);
        });

        // totals may change visually. notify
        this.OnPropertyChanged(nameof(this.IncomeTotal));
        this.OnPropertyChanged(nameof(this.ExpenseTotal));
        this.OnPropertyChanged(nameof(this.NetTotal));
    }

    private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // If transactions totals change, notify UI
        if (e.PropertyName is nameof(TransactionViewModel.IncomeTotal)
            or nameof(TransactionViewModel.ExpenseTotal)
            or nameof(TransactionViewModel.Transactions)
            or nameof(TransactionViewModel.SelectedTransaction))
        {
            this.NotifyTotals();
        }

        // If income-loss list changed, recompute projections and lists
        if (e.PropertyName is nameof(IncomeLossViewModel.IncomeLosses))
        {
            this.ComputeProjections();
        }

        // If categories changed, raise categories change
        if (e.PropertyName is nameof(CategoryViewModel.Categories))
        {
            this.OnPropertyChanged(nameof(this.Categories));
        }
    }

    private void NotifyTotals()
    {
        this.OnPropertyChanged(nameof(this.IncomeTotal));
        this.OnPropertyChanged(nameof(this.ExpenseTotal));
        this.OnPropertyChanged(nameof(this.NetTotal));
    }

    // Helper to invoke child VM commands (reduces duplicated pattern)
    private static async Task ExecuteVmCommandAsync(object? commandObj, object? parameter = null)
    {
        if (commandObj is IAsyncRelayCommand asyncCmd)
        {
            await asyncCmd.ExecuteAsync(parameter).ConfigureAwait(false);
        }
        else if (commandObj is System.Windows.Input.ICommand cmd)
        {
            cmd.Execute(parameter);
        }
    }

    [RelayCommand]
    private async Task AddTransactionAsync()
    {
        await ExecuteVmCommandAsync(this._transactionsVm.AddCommand, null).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task EditTransactionAsync(Transaction? t)
    {
        await ExecuteVmCommandAsync(this._transactionsVm.EditCommand, t).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task DeleteTransactionAsync(Transaction? t)
    {
        await ExecuteVmCommandAsync(this._transactionsVm.DeleteCommand, t).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        await ExecuteVmCommandAsync(this._categoriesVm.AddCommand, null).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task EditCategoryAsync(Category? c)
    {
        await ExecuteVmCommandAsync(this._categoriesVm.EditCommand, c).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(Category? c)
    {
        await ExecuteVmCommandAsync(this._categoriesVm.DeleteCommand, c).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task AddIncomeLossAsync()
    {
        await ExecuteVmCommandAsync(this._incomeLossesVm.AddCommand, null).ConfigureAwait(false);
    }

    // Forward Edit/Delete commands for IncomeLoss items so Overview buttons work
    [RelayCommand]
    private async Task EditIncomeLossAsync(IncomeLoss? item)
    {
        await ExecuteVmCommandAsync(this._incomeLossesVm.EditCommand, item).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task DeleteIncomeLossAsync(IncomeLoss? item)
    {
        await ExecuteVmCommandAsync(this._incomeLossesVm.DeleteCommand, item).ConfigureAwait(false);
    }

    [RelayCommand]
    private void PrevMonth()
    {
        this.SelectedMonth = this.SelectedMonth.AddMonths(-1);
    }

    [RelayCommand]
    private void NextMonth()
    {
        this.SelectedMonth = this.SelectedMonth.AddMonths(1);
    }

    [RelayCommand]
    private void GoToCurrentMonth()
    {
        this.SelectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    }
}