using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DanfossSPGroup7.Data;
using DanfossSPGroup7.Domain;
using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace DanfossSPGroup7.UI.ViewModels;

public partial class AssetViewModel : ObservableObject
{

    private readonly MaintenanceCalculation _calculator = new MaintenanceCalculation();
    private readonly AssetManager _dataService = new AssetManager();
    private readonly ObservableCollection<UnitConfigViewModel> _scenario1Configs = new();
    private readonly ObservableCollection<UnitConfigViewModel> _scenario2Configs = new();
    [ObservableProperty] private int _selectedScenario = 1; // make the default scenario 1
    [ObservableProperty] private ObservableCollection<UnitConfigViewModel> _displayUnits = new();

    public AssetViewModel()
    {
        var allUnits = _dataService.GetProductionUnits();
        // load all units so the checkboxes/maintenance slider dont reset
        // scenario 1 config (GB1, GB2, GB3, OB1)
        var s1Names = new[] { "GB1", "GB2", "GB3", "OB1" };
        foreach (var unit in allUnits.Where(u => s1Names.Contains(u.Name)))
        {
            _scenario1Configs.Add(new UnitConfigViewModel(unit, this) { IsScenario2 = false });
        }

        // scenario 2 config (GM1, EB1, GB1, GB3)
        var s2Names = new[] { "GM1", "EB1", "GB1", "GB3" };
        foreach (var unit in allUnits.Where(u => s2Names.Contains(u.Name)))
        {
            // We create NEW instances here so they are independent of Scenario 1
            _scenario2Configs.Add(new UnitConfigViewModel(unit, this) { IsScenario2 = true });
        }
        LoadScenario(1);
    }

    [RelayCommand]
    public void SwitchScenario(string scenarioNumber)
    {    
        SelectedScenario = int.Parse(scenarioNumber);
        LoadScenario(SelectedScenario);
    }

    [RelayCommand]
    // disable other maintenance options when one is selected already
    public void HandleMaintenanceChange()
    {
        // check if any unit is already selected for maintenance
        var selectedUnit = DisplayUnits.FirstOrDefault(unit => unit.IsSelectedForMaintenance);
        
        foreach (var unit in DisplayUnits)
        {
            if (selectedUnit == null)
                unit.CanToggleMaintenance = true;
            else
                unit.CanToggleMaintenance = unit == selectedUnit;
        }
    }

    public void LoadScenario(int scenario)
    {
        SelectedScenario = scenario;
        
        // preserve checkboxes/sliders in the list that is hidden
        DisplayUnits = (scenario == 1) ? _scenario1Configs : _scenario2Configs;

        HandleMaintenanceChange();
    }

    // bonus requirement logic for results tab to check if scenario2 is modified from default state
    public bool IsScenario2Modified()
    {
        if (SelectedScenario != 2) return false;
        // check if any unit that is by default ON is turned OFF
        return DisplayUnits.Any(unit => !unit.IsActive);
    }

    public List<string> GetSelectedUnitNames()
    {
        return DisplayUnits
            .Where(unit => unit.IsActive)
            .Select(unit => unit.Unit.Name)
            .ToList();
    }


    public partial class UnitConfigViewModel : ObservableObject
    {
        private AssetViewModel? _parent;

        public ProductionUnit Unit {get; set;}
        public Bitmap UnitImage { get; }
        [ObservableProperty] private bool _isActive;
        [ObservableProperty] private bool _isScenario2;
        [ObservableProperty] private bool _isSelectedForMaintenance;
        [ObservableProperty] private bool _canToggleMaintenance = true; // default for every unit
        [ObservableProperty] private int _maintenanceDuration = 30; // by default we select the minimum

        public UnitConfigViewModel(ProductionUnit unit, AssetViewModel? parent = null) 
        { 
            Unit = unit;
            UnitImage = new Bitmap(AssetLoader.Open(new Uri(unit.ImagePath)));
            _parent = parent;
        }

        partial void OnIsSelectedForMaintenanceChanged(bool value)
        {
            // Notify parent to update maintenance lock when checkbox changes
            _parent?.HandleMaintenanceChange();
        }
    }


    [RelayCommand]
    public void PrepareOptimization()
    {   
        if (Optimizer.Instance == null)
            return;

        // reset all previous maintenance every time results are recalculated
        foreach (var unit in Optimizer.Instance.ProductionUnits)
        {
            unit.ClearMaintenance();
        }

        // finds the unit where the user checked the maintenance box
        var selectedUnit = DisplayUnits.FirstOrDefault(unit => unit.IsSelectedForMaintenance);

        if(selectedUnit == null)
            return;
        if(Optimizer.Instance == null)
            return;
        try
        {

            _calculator.CreateMaintenanceForBoiler(
                selectedUnit.Unit.Name,
                selectedUnit.MaintenanceDuration,
                Optimizer.Instance.ProductionUnits
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
    }
}
