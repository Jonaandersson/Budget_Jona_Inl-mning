#nullable enable
using System.Windows;
using System.Windows.Controls;
using Budget_Jona_Inlämning.ViewModels;
using Budget_Jona_Inlämning.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Budget_Jona_Inlämning.Views;

public partial class TransactionEditView : UserControl
{
    public TransactionEditView()
    {
        this.InitializeComponent();
    }

    // New-category button handler: the DataContext of this view is the editor VM.
    private void OnAddCategoryClick(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is not TransactionEditorViewModel tvm)
        {
            return;
        }

        var provider = ((App)Application.Current).Services;
        var dialogService = provider.GetService<IDialogService>();
        var categoryEditorFactory = provider.GetService<IServiceProvider>(); // we'll resolve editor from provider

        if (dialogService is null || provider is null)
        {
            return;
        }

        // Resolve a transient Category editor VM from DI
        var vm = provider.GetService<CategoryViewModel>();
        if (vm is null)
        {
            return;
        }

        var view = new CategoryEditView();

        _ = dialogService.ShowDialogAsync(view, vm).ContinueWith(async t =>
        {
            var result = await t.ConfigureAwait(false);
            if (result == true)
            {
                // reload categories in the editor VM on UI thread
                await tvm.LoadCategoriesAsync().ConfigureAwait(false);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }
}