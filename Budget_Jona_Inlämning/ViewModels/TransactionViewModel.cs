#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Budget_Jona_Inlämning.Models;
using Budget_Jona_Inlämning.Services;
using Budget_Jona_Inlämning.Views;

namespace Budget_Jona_Inlämning.ViewModels;

public sealed partial class TransactionViewModel : BaseViewModel
{
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IDialogService _dialogService;
    private readonly Func<TransactionEditorViewModel> _editorFactory;

    public ObservableCollection<Transaction> Transactions { get; } = new();
    public Transaction? SelectedTransaction { get; set; }

    private decimal _incomeTotal;
    private decimal _expenseTotal;

    public decimal IncomeTotal
    {
        get => this._incomeTotal;
        private set
        {
            if (this.SetProperty(ref this._incomeTotal, value))
            {
                this.OnPropertyChanged(nameof(NetTotal));
            }
        }
    }

    public decimal ExpenseTotal
    {
        get => this._expenseTotal;
        private set
        {
            if (this.SetProperty(ref this._expenseTotal, value))
            {
                this.OnPropertyChanged(nameof(NetTotal));
            }
        }
    }

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

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (this.IsBusy) return;

        try
        {
            this.IsBusy = true;
            var list = await this._transactionService.GetAllAsync().ConfigureAwait(false);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                this.Transactions.Clear();
                foreach (var t in list) this.Transactions.Add(t);
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
        var editor = this._editorFactory();
        await editor.LoadCategoriesAsync();

        var view = new TransactionEditView();
        var result = await this._dialogService.ShowDialogAsync(view, editor);
        if (result == true) await this.LoadAsync();
    }

    [RelayCommand]
    private async Task EditAsync(Transaction? t)
    {
        if (t is null) return;

        var entity = await this._transactionService.GetByIdAsync(t.Id).ConfigureAwait(false);
        if (entity is null) return;

        var editor = this._editorFactory();

        // populate editor model
        editor.Model.Id = entity.Id;
        editor.Model.Amount = entity.Amount;
        editor.Model.Date = entity.Date;
        editor.Model.Description = entity.Description;
        editor.Model.IsMonthly = entity.IsMonthly;
        editor.Model.IsIncome = entity.IsIncome;
        editor.Model.CategoryId = entity.CategoryId;

        await editor.LoadCategoriesAsync();

        var view = new TransactionEditView();
        var result = await this._dialogService.ShowDialogAsync(view, editor);
        if (result == true) await this.LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(Transaction? t)
    {
        if (t is null) return;

        var answer = MessageBox.Show($"Delete transaction '{t.Description}'?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        await this._transactionService.DeleteAsync(t.Id).ConfigureAwait(false);

        Application.Current?.Dispatcher.Invoke(() => this.Transactions.Remove(t));

        this.IncomeTotal = this.Transactions.Where(x => x.IsIncome).Sum(x => x.Amount);
        this.ExpenseTotal = this.Transactions.Where(x => !x.IsIncome).Sum(x => x.Amount);
    }
}