using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;

namespace DanfossSPGroup7.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public AssetViewModel AssetPage {get;} = new AssetViewModel();
    [ObservableProperty] private ObservableObject? _currentViewModel;
    [ObservableProperty] private string _selectedTab = "Asset";

    public bool IsAssetTabSelected => SelectedTab == "Asset";
    public bool IsDashboardTabSelected => SelectedTab == "Dashboard";
    public bool IsResultTabSelected => SelectedTab == "Result";

    public MainViewModel()
    {
        CurrentViewModel = AssetPage;
    }

    [RelayCommand]
    public void Navigate(string viewName)
    {   
        SelectedTab = viewName;

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

    partial void OnSelectedTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsAssetTabSelected));
        OnPropertyChanged(nameof(IsDashboardTabSelected));
        OnPropertyChanged(nameof(IsResultTabSelected));
    }
}
