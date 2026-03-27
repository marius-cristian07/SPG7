using Avalonia.Controls;
using DanfossSPGroup7.UI.ViewModels;

namespace DanfossSPGroup7.UI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }
}
