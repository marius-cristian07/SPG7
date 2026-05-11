using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;

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
            AssetPage.PrepareOptimization(1, false); // saves maintenance to units

            // make scenario1 show first
            var scenario1Units = AssetPage.GetSelectedUnitNames(1);

            // force scenario 1 (winter) as the first view
            CurrentViewModel = new ResultViewModel(AssetPage, 1, false, scenario1Units);
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
