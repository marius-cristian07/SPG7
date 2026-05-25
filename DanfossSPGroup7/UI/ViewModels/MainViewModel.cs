using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using DanfossSPGroup7.Data;
using DanfossSPGroup7.Domain;

namespace DanfossSPGroup7.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Optimizer _optimizer;
    private readonly IAssetManager _assetManager;
    private bool _lastIsSummerSelection;
    public AssetViewModel AssetPage { get; }
    [ObservableProperty] private ObservableObject? _currentViewModel;
    [ObservableProperty] private string _selectedTab = "Asset";

    public bool IsAssetTabSelected => SelectedTab == "Asset";
    public bool IsDashboardTabSelected => SelectedTab == "Dashboard";
    public bool IsResultTabSelected => SelectedTab == "Result";

    public MainViewModel(Optimizer optimizer, IAssetManager assetManager)
    {
        _optimizer = optimizer;
        _assetManager = assetManager;
        AssetPage = new AssetViewModel(_optimizer, _assetManager);
        CurrentViewModel = AssetPage;
    }

    [RelayCommand]
    public void Navigate(string viewName)
    {   
        if (CurrentViewModel is ResultViewModel activeResultViewModel)
        {
            _lastIsSummerSelection = activeResultViewModel.CurrentIsSummer;
        }

        SelectedTab = viewName;

        if (viewName == "Result")
        {
            bool isSummer = _lastIsSummerSelection;
            int scenario = AssetPage.SelectedScenario;

            var selectedUnits = AssetPage.GetSelectedUnitNames(scenario);

            CurrentViewModel = new ResultViewModel(_optimizer, AssetPage, scenario, isSummer, selectedUnits);
            return;
        }
            

        CurrentViewModel = viewName switch
        {
            "Dashboard" => new DashboardViewModel(_assetManager),
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
