using Avalonia.Controls;
using DanfossSPGroup7.UI.ViewModels;
namespace DanfossSPGroup7.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
