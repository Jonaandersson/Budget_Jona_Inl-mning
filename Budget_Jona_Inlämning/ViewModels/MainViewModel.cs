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

    // Projection values (generated properties)
    [ObservableProperty]
    private decimal projectedIncome;

    [ObservableProperty]
    private decimal projectedExpense;

    [ObservableProperty]
    private decimal projectedNet;

    [ObservableProperty]
    private decimal incomeLossAdjustment;

    // Selected month (generated property). Default to first day of current month.
    [ObservableProperty]
    private DateTime selectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    public MainViewModel(
        TransactionViewModel transactionsVm,
        CategoryViewModel categoriesVm,
        IncomeLossViewModel incomeLossesVm)
    {
        this._transactionsVm = transactionsVm;
        this._categoriesVm = categoriesVm;
        this._incomeLossesVm = incomeLossesVm;

        // Forward property-changed from child VMs so UI bound to MainViewModel updates
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

        // React to collection changes in transactions so filtered view updates
        this._transactionsVm.Transactions.CollectionChanged += this.Transactions_CollectionChanged;

        this.CurrentIncomeLosses = new ObservableCollection<IncomeLoss>();
        this.ProjectedIncomeLosses = new ObservableCollection<IncomeLoss>();
    }

    partial void OnSelectedMonthChanged(DateTime value)
    {
        this.OnPropertyChanged(nameof(this.SelectedMonthLabel));
        this.UpdateFilteredTransactions();
    }

    private void Transactions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Update filtered view on UI thread
        this.UpdateFilteredTransactions();
    }

    // Expose collections and properties expected by existing views (wrappers)
    public ObservableCollection<Transaction> Transactions => this._transactionsVm.Transactions;

    public ObservableCollection<Transaction> FilteredTransactions { get; } = new();

    public Transaction? SelectedTransaction
    {
        get => this._transactionsVm.SelectedTransaction;
        set => this._transactionsVm.SelectedTransaction = value;
    }

    public ObservableCollection<Category> Categories => this._categoriesVm.Categories;
    public ObservableCollection<IncomeLoss> IncomeLosses => this._incomeLossesVm.IncomeLosses;

    // Lists to present income-loss details
    public ObservableCollection<IncomeLoss> CurrentIncomeLosses { get; }
    public ObservableCollection<IncomeLoss> ProjectedIncomeLosses { get; }

    // Totals forwarded from Transactions VM
    public decimal IncomeTotal => this._transactionsVm.IncomeTotal;
    public decimal ExpenseTotal => this._transactionsVm.ExpenseTotal;
    public decimal NetTotal => this.IncomeTotal - this.ExpenseTotal;

    // Friendly label for UI
    public string SelectedMonthLabel => this.SelectedMonth.ToString("MMMM yyyy");

    // Load child VMs sequentially to avoid DbContext concurrency issues
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

        // Net adjustment (AmountLost - RefundAmount)
        Decimal adjustment = this._incomeLossesVm.ComputeAdjustmentForMonths(currentMonthSet, nextMonthSet);
        Decimal netLoss = Math.Max(0m, adjustment);

        this.IncomeLossAdjustment = netLoss;
        this.ProjectedIncome = recurringIncome + oneTimeIncomeNext - netLoss;
        this.ProjectedExpense = recurringExpense + oneTimeExpenseNext;
        this.ProjectedNet = this.ProjectedIncome - this.ProjectedExpense;

        // Update collections on UI thread
        Application.Current?.Dispatcher.Invoke(() =>
        {
            this.CurrentIncomeLosses.Clear();
            foreach (IncomeLoss i in currentItems) this.CurrentIncomeLosses.Add(i);

            this.ProjectedIncomeLosses.Clear();
            foreach (IncomeLoss i in projectedItems) this.ProjectedIncomeLosses.Add(i);
        });
    }

    // Update filtered transactions for the currently selected month
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

        // totals may change visually -> notify
        this.OnPropertyChanged(nameof(this.IncomeTotal));
        this.OnPropertyChanged(nameof(this.ExpenseTotal));
        this.OnPropertyChanged(nameof(this.NetTotal));
    }

    private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // If transactions totals change, notify UI
        if (e.PropertyName is "IncomeTotal" or "ExpenseTotal" or "Transactions" or "SelectedTransaction")
        {
            this.NotifyTotals();
        }

        // If income-loss list changed, recompute projections and lists
        if (e.PropertyName is "IncomeLosses")
        {
            this.ComputeProjections();
        }

        // If categories changed, raise categories change
        if (e.PropertyName is "Categories")
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

    [RelayCommand]
    private async Task AddTransactionAsync()
    {
        if (this._transactionsVm.AddCommand is IAsyncRelayCommand a) await a.ExecuteAsync(null);
        else this._transactionsVm.AddCommand.Execute(null);
    }

    [RelayCommand]
    private async Task EditTransactionAsync(Transaction? t)
    {
        if (this._transactionsVm.EditCommand is IAsyncRelayCommand a) await a.ExecuteAsync(t);
        else this._transactionsVm.EditCommand.Execute(t);
    }

    [RelayCommand]
    private async Task DeleteTransactionAsync(Transaction? t)
    {
        if (this._transactionsVm.DeleteCommand is IAsyncRelayCommand a) await a.ExecuteAsync(t);
        else this._transactionsVm.DeleteCommand.Execute(t);
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        if (this._categoriesVm.AddCommand is IAsyncRelayCommand a) await a.ExecuteAsync(null);
        else this._categoriesVm.AddCommand.Execute(null);
    }

    [RelayCommand]
    private async Task EditCategoryAsync(Category? c)
    {
        if (this._categoriesVm.EditCommand is IAsyncRelayCommand a) await a.ExecuteAsync(c);
        else this._categoriesVm.EditCommand.Execute(c);
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(Category? c)
    {
        if (this._categoriesVm.DeleteCommand is IAsyncRelayCommand a) await a.ExecuteAsync(c);
        else this._categoriesVm.DeleteCommand.Execute(c);
    }

    [RelayCommand]
    private async Task AddIncomeLossAsync()
    {
        if (this._incomeLossesVm.AddCommand is IAsyncRelayCommand a) await a.ExecuteAsync(null);
        else this._incomeLossesVm.AddCommand.Execute(null);
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