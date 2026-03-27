using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DanfossSPGroup7.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? currentViewModel;

    public MainViewModel()
    {
        CurrentViewModel = new AssetViewModel();
    }

    [RelayCommand]
    public void Navigate(string viewName)
    {
        CurrentViewModel = viewName switch
        {
            "Dashboard" => new DashboardViewModel(),
            "Asset" => new AssetViewModel(),
            "Result" => new ResultViewModel(),
            _ => CurrentViewModel
        };
    }
}
