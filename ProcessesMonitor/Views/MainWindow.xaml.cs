// Views/MainWindow.xaml.cs
using System.Windows;
using System.Windows.Input;
using ProcessesMonitor.ViewModels;
using ProcessesMonitor.Views;

namespace ProcessesMonitor.Views;

public partial class MainWindow : Window
{
    private readonly ProcessViewModel _viewModel;
    
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new ProcessViewModel(Dispatcher);
        DataContext = _viewModel;
    }
    
    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        var help = new HelpWindow { Owner = this };
        help.ShowDialog();
    }
    
    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.RefreshProcesses();
    }
    
    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.ApplyFilters();
        }
    }
    
    private void ApplyPriority_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedProcess != null)
        {
            _viewModel.ChangePriority(_viewModel.SelectedProcess.Priority);
        }
    }
    
    private void KillButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.KillSelectedProcess();
    }
    
    private void ApplyAffinity_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedProcess != null)
        {
            _viewModel.ApplyCpuAffinity();
        }
    }
}