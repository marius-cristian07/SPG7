using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
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
        if (viewName == "Result")
        {
            AssetPage.PrepareOptimization(); // saves maintenance to units

            CurrentViewModel = new ResultViewModel(
                AssetPage.SelectedScenario,
                AssetPage.IsSummer,
                AssetPage.GetSelectedUnitNames()
            );
            return;
            }
            

        CurrentViewModel = viewName switch
        {
            "Dashboard" => new DashboardViewModel(),
            "Asset" => AssetPage,
            _ => CurrentViewModel
        };
    }
}
