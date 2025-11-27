#nullable enable
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Budget_Jona_Inlämning.Models;

namespace Budget_Jona_Inlämning.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly TransactionViewModel _transactionsVm;
    private readonly CategoryViewModel _categoriesVm;
    private readonly IncomeLossViewModel _incomeLossesVm;

    private decimal _projectedIncome;
    private decimal _projectedExpense;
    private decimal _projectedNet;
    private decimal _incomeLossAdjustment;

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
    }

    // Expose collections and properties expected by existing views (wrappers)

    public ObservableCollection<Transaction> Transactions => this._transactionsVm.Transactions;
    public Transaction? SelectedTransaction
    {
        get => this._transactionsVm.SelectedTransaction;
        set => this._transactionsVm.SelectedTransaction = value;
    }

    public ObservableCollection<Category> Categories => this._categoriesVm.Categories;
    public ObservableCollection<IncomeLoss> IncomeLosses => this._incomeLossesVm.IncomeLosses;

    // Totals forwarded from Transactions VM
    public decimal IncomeTotal => this._transactionsVm.IncomeTotal;
    public decimal ExpenseTotal => this._transactionsVm.ExpenseTotal;
    public decimal NetTotal => this.IncomeTotal - this.ExpenseTotal;

    // Commands forwarded from child VMs so XAML bindings keep working
    public IRelayCommand LoadCommand => this._transactionsVm.LoadCommand;
    public IRelayCommand AddTransactionCommand => this._transactionsVm.AddCommand;
    public IRelayCommand<Transaction?> EditTransactionCommand => this._transactionsVm.EditCommand;
    public IRelayCommand<Transaction?> DeleteTransactionCommand => this._transactionsVm.DeleteCommand;

    public IRelayCommand AddCategoryCommand => this._categoriesVm.AddCommand;
    public IRelayCommand<Category?> EditCategoryCommand => this._categoriesVm.EditCommand;
    public IRelayCommand<Category?> DeleteCategoryCommand => this._categoriesVm.DeleteCommand;
    public IRelayCommand AddIncomeLossCommand => this._incomeLossesVm.AddCommand;

    // Projection properties
    public decimal ProjectedIncome
    {
        get => this._projectedIncome;
        private set => this.SetProperty(ref this._projectedIncome, value);
    }

    public decimal ProjectedExpense
    {
        get => this._projectedExpense;
        private set => this.SetProperty(ref this._projectedExpense, value);
    }

    public decimal ProjectedNet
    {
        get => this._projectedNet;
        private set => this.SetProperty(ref this._projectedNet, value);
    }

    public decimal IncomeLossAdjustment
    {
        get => this._incomeLossAdjustment;
        private set => this.SetProperty(ref this._incomeLossAdjustment, value);
    }

    // Load child VMs sequentially to avoid DbContext concurrency issues
    public async Task LoadAllAsync()
    {
        // Load categories, transactions, incomeloss sequentially
        await this._categoriesVm.LoadAsync().ConfigureAwait(false);
        await this._transactionsVm.LoadAsync().ConfigureAwait(false);
        await this._incomeLossesVm.LoadAsync().ConfigureAwait(false);

        // Compute projection on UI-safe data
        this.ComputeProjections();
        // Notify totals forwarded
        this.NotifyTotals();
    }

    private void ComputeProjections()
    {
        var today = DateTime.Today;
        var nextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);

        var recurringIncome = this._transactionsVm.Transactions.Where(t => t.IsMonthly && t.IsIncome).Sum(t => t.Amount);
        var recurringExpense = this._transactionsVm.Transactions.Where(t => t.IsMonthly && !t.IsIncome).Sum(t => t.Amount);

        var oneTimeIncomeNext = this._transactionsVm.Transactions.Where(t => !t.IsMonthly && t.IsIncome && t.Date.Year == nextMonth.Year && t.Date.Month == nextMonth.Month).Sum(t => t.Amount);
        var oneTimeExpenseNext = this._transactionsVm.Transactions.Where(t => !t.IsMonthly && !t.IsIncome && t.Date.Year == nextMonth.Year && t.Date.Month == nextMonth.Month).Sum(t => t.Amount);

        var adjustment = this._incomeLossesVm.ComputeAdjustmentForMonths((today.Year, today.Month), (nextMonth.Year, nextMonth.Month));
        var netLoss = Math.Max(0m, adjustment);

        this.IncomeLossAdjustment = netLoss;
        this.ProjectedIncome = recurringIncome + oneTimeIncomeNext - netLoss;
        this.ProjectedExpense = recurringExpense + oneTimeExpenseNext;
        this.ProjectedNet = this.ProjectedIncome - this.ProjectedExpense;
    }

    // Forward child property changes (to update IncomeTotal/ExpenseTotal etc)
    private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // If transactions totals change, notify UI
        if (e.PropertyName is "IncomeTotal" or "ExpenseTotal" or "Transactions" or "SelectedTransaction")
        {
            this.NotifyTotals();
        }

        // If income-loss list changed, recompute projections
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
}