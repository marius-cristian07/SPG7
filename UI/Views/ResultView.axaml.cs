using Avalonia.Controls;
using DanfossSPGroup7.UI.ViewModels;

namespace DanfossSPGroup7.UI.Views;

public partial class ResultView : UserControl
{
    public ResultView()
    {
        InitializeComponent();
        DataContext = new ResultViewModel();
    }
}
