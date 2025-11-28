#nullable enable
using System.Windows;
using Budget_Jona_Inlämning.ViewModels;
using Budget_Jona_Inlämning.Views;

namespace Budget_Jona_Inlämning;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        this._viewModel = viewModel;
        this.InitializeComponent();
        this.DataContext = this._viewModel;
    }
}