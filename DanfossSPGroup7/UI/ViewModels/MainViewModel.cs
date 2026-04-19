using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;

namespace DanfossSPGroup7.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public AssetViewModel AssetPage {get;} = new AssetViewModel();

    [ObservableProperty] private ObservableObject? _currentViewModel;

    public MainViewModel()
    {
        CurrentViewModel = AssetPage;
    }

    [RelayCommand]
    public void Navigate(string viewName)
    {

        CurrentViewModel = viewName switch
        {
            "Dashboard" => new DashboardViewModel(),
            "Asset" => AssetPage,
            "Result" => new ResultViewModel(),
            _ => CurrentViewModel
        };
    }
}
