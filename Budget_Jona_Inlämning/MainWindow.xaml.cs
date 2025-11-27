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

        // Optionally embed the OverviewView in a named placeholder in your MainWindow.xaml
        // If MainWindow has a ContentControl named 'MainContent', you can assign:
        // this.MainContent.Content = new OverviewView();
    }
}