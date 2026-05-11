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
            bool isSummer = false;
            int scenario = AssetPage.SelectedScenario;

            var selectedUnits = AssetPage.GetSelectedUnitNames(scenario);

            CurrentViewModel = new ResultViewModel(AssetPage, scenario, isSummer, selectedUnits);
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
