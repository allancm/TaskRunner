using System.Windows;
using TaskRunner.ViewModels;

namespace TaskRunner;

public partial class MainWindow : Window
{
    // The ViewModel is injected by the container: "ask, don't create".
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
