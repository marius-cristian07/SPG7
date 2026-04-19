using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DanfossSPGroup7.Data;
using DanfossSPGroup7.Domain;
using System;

namespace DanfossSPGroup7.UI.ViewModels;

public partial class AssetViewModel : ObservableObject
{
    private readonly AssetManager _dataService = new AssetManager();

    [ObservableProperty] private int _selectedScenario = 1; // make the default scenario 1
    [ObservableProperty] private ObservableCollection<UnitConfigViewModel> _displayUnits = new();

    public AssetViewModel()
    {
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
        DisplayUnits.Clear();

        var allUnits = _dataService.GetProductionUnits();
        IEnumerable<ProductionUnit> selectedUnits;

        if (scenario == 1)
        {
            //Scenario 1 - (GB1, GB2, GB3, OB1)
            selectedUnits= allUnits.Where(unit => new[] { "GB1", "GB2", "GB3", "OB1" }.Contains(unit.Name));
        }
        else
        {
            //Scenario 2 - (GM1, EB1, GB1, GB3)
            selectedUnits = allUnits.Where(unit => new[] { "GM1", "EB1", "GB1", "GB3" }.Contains(unit.Name));
        }

        foreach (var unit in selectedUnits)
        {
            DisplayUnits.Add(new UnitConfigViewModel(unit)
            {
                //Scenario 1 - all units are activated and cannot be deactivated unless for maintenance
                //Scenario 2 - by defaul the units are activated but can be toggled to see different combinations
                IsActive = true,
                IsScenario2 = (scenario == 2)
            });
        }
    }

    // bonus requirement logic for results tab to check if scenario2 is modified from default state
    public bool IsScenario2Modified()
    {
        if (SelectedScenario != 2) return false;
        // check if any unit that is by default ON is turned OFF
        return DisplayUnits.Any(unit => !unit.IsActive);
    }




    public partial class UnitConfigViewModel : ObservableObject
    {
        public ProductionUnit Unit {get; set;}
        [ObservableProperty] private bool _isActive;
        [ObservableProperty] private bool _isScenario2;
        [ObservableProperty] private bool _isSelectedForMaintenance;
        [ObservableProperty] private bool _canToggleMaintenance = true; // default for every unit
        [ObservableProperty] private int _maintenanceDuration = 30; // by default we select the minimum

        public UnitConfigViewModel(ProductionUnit unit) => Unit = unit;
    }


    // Maintenance call
    public void PassMaintenanceData(int hours, string unitName)
    {
        Console.WriteLine($"Sent to Optimizer: {unitName} for {hours} hours.");
        // fill actual maintennace call from my teams code
    }

    [RelayCommand]
    public void PrepareOptimization()
    {
        // finds the unit where the user checked the maintenance box
        var selectedUnit = DisplayUnits.FirstOrDefault(u => u.IsSelectedForMaintenance);

        if(selectedUnit != null)
        {
            PassMaintenanceData(selectedUnit.MaintenanceDuration, selectedUnit.Unit.Name);
        }
        else
        {
            // do something if no maintenance is selected
        }
    }
}