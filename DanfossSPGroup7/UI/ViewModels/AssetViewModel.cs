using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DanfossSPGroup7.Data;
using DanfossSPGroup7.Domain;
using System;
using System.Text;

namespace DanfossSPGroup7.UI.ViewModels;

public partial class AssetViewModel : ObservableObject
{
    private readonly Optimizer _optimizer;
    private readonly IAssetManager _assetManager;
    private readonly MaintenanceCalculation _calculator = new MaintenanceCalculation();
    private readonly ObservableCollection<UnitConfigViewModel> _scenario1Configs = new();
    private readonly ObservableCollection<UnitConfigViewModel> _scenario2Configs = new();
    [ObservableProperty] private int _selectedScenario = 1; // default scenario is 1
    [ObservableProperty] private ObservableCollection<UnitConfigViewModel> _displayUnits = new();
    [ObservableProperty] private string _maintenanceWarning = string.Empty;
    public IReadOnlyList<string> MaintenanceStartDayOptions { get; } =
        new[] { "1st day", "2nd day", "3rd day", "4th day" };

    public AssetViewModel(Optimizer optimizer, IAssetManager assetManager)
    {
        _optimizer = optimizer;
        _assetManager = assetManager;
        var allUnits = _assetManager.GetProductionUnits();
        // load all units so the checkboxes and sliders do not reset
        // create config objects for scenario 1
        var s1Names = new[] { "GB1", "GB2", "GB3", "OB1" };
        foreach (var unit in allUnits.Where(u => s1Names.Contains(u.Name)))
        {
            var unitConfig = new UnitConfigViewModel(unit);
            unitConfig.ConfigChanged += RefreshMaintenanceWarning;
            _scenario1Configs.Add(unitConfig);
        }

        // create config objects for scenario 2
        var s2Names = new[] { "GM1", "EB1", "GB1", "GB3" };
        foreach (var unit in allUnits.Where(u => s2Names.Contains(u.Name)))
        {
            // New objects keep scenario 2 separate from scenario 1
            var unitConfig = new UnitConfigViewModel(unit);
            unitConfig.ConfigChanged += RefreshMaintenanceWarning;
            _scenario2Configs.Add(unitConfig);
        }
        LoadScenario(1);
    }

    [RelayCommand]
    public void SwitchScenario(string scenarioNumber)
    {    
        // change the visible scenario from the UI
        SelectedScenario = int.Parse(scenarioNumber);
        LoadScenario(SelectedScenario);
    }

    public void LoadScenario(int scenario)
    {
        SelectedScenario = scenario;
        
        // show the config list for the selected scenario
        DisplayUnits = (scenario == 1) ? _scenario1Configs : _scenario2Configs;
        RefreshMaintenanceWarning();
    }

    public List<string> GetSelectedUnitNames()
    {
        return GetSelectedUnitNames(SelectedScenario);
    }

    public List<string> GetSelectedUnitNames(int scenario)
    {
        var configs = GetScenarioConfigs(scenario);

        return configs
            .Select(unit => unit.Unit.Name)
            .ToList();
    }

    public bool IsScenario1Selected => SelectedScenario == 1;
    public bool IsScenario2Selected => SelectedScenario == 2;

    partial void OnSelectedScenarioChanged(int value)
    {
        OnPropertyChanged(nameof(IsScenario1Selected));
        OnPropertyChanged(nameof(IsScenario2Selected));
        RefreshMaintenanceWarning();
    }

    partial void OnMaintenanceWarningChanged(string value)
    {
        OnPropertyChanged(nameof(HasMaintenanceWarning));
    }

    public bool HasMaintenanceWarning => !string.IsNullOrWhiteSpace(MaintenanceWarning);

    private ObservableCollection<UnitConfigViewModel> GetScenarioConfigs(int scenario)
    {
        // get the saved config list for a scenario
        return scenario == 1 ? _scenario1Configs : _scenario2Configs;
    }

    private DateTime GetSourceStartDate(bool isSummer)
    {
        // use the first date from the chosen season data
        var sourceData = isSummer ? _optimizer.Summer : _optimizer.Winter;
        if (sourceData.Count == 0)
            return new DateTime(2026, 1, 5);

        return sourceData.Keys.Min().Date;
    }

    public void PrepareOptimization(int scenario, bool isSummer)
    {
        // reset old maintenance before the results are recalculated
        foreach (var unit in _optimizer.ProductionUnits)
        {
            unit.ClearMaintenance();
        }

        // find the units where the maintenance checkbox is selected
        var scenarioConfigs = GetScenarioConfigs(scenario);
        var selectedUnits = scenarioConfigs.Where(unit => unit.IsSelectedForMaintenance).ToList();

        foreach (var selectedUnit in selectedUnits)
        {
            // turn the selected day into a real start date
            int selectedDayOffset = Math.Clamp(selectedUnit.MaintenanceStartDayIndex, 0, 3);
            DateTime startDate = GetSourceStartDate(isSummer).AddDays(selectedDayOffset);

            _calculator.CreateMaintenanceForBoiler(
                selectedUnit.Unit.Name,
                selectedUnit.MaintenanceDuration,
                _optimizer.ProductionUnits,
                startDate
            );
        }

        RefreshMaintenanceWarning();
    }

    private void RefreshMaintenanceWarning()
    {
        // rebuild the warning text after a change
        MaintenanceWarning = BuildMaintenanceWarning(SelectedScenario);
    }

    private string BuildMaintenanceWarning(int scenario)
    {
        // find all selected maintenance settings for this scenario
        var scenarioConfigs = GetScenarioConfigs(scenario).ToList();
        var selectedMaintenances = scenarioConfigs
            .Where(unit => unit.IsSelectedForMaintenance)
            .ToList();

        if (selectedMaintenances.Count == 0)
            return string.Empty;

        // check both seasons because the same maintenance can affect them differently
        var seasonalWarnings = new List<string>();
        TryBuildSeasonWarning(
            _optimizer.Winter,
            scenarioConfigs,
            selectedMaintenances,
            $"Scenario {scenario} winter",
            seasonalWarnings);
        TryBuildSeasonWarning(
            _optimizer.Summer,
            scenarioConfigs,
            selectedMaintenances,
            $"Scenario {scenario} summer",
            seasonalWarnings);

        if (seasonalWarnings.Count == 0)
            return string.Empty;

        // join all warnings into one text for the UI
        var sb = new StringBuilder("Warning: selected maintenance can leave heat demand unmet.");
        foreach (var warning in seasonalWarnings)
        {
            sb.Append(' ');
            sb.Append(warning);
        }

        return sb.ToString();
    }

    private static void TryBuildSeasonWarning(
        IReadOnlyDictionary<DateTime, DataPoint> sourceData,
        IReadOnlyList<UnitConfigViewModel> scenarioConfigs,
        IReadOnlyList<UnitConfigViewModel> selectedMaintenances,
        string label,
        ICollection<string> warnings)
    {
        if (sourceData.Count == 0)
            return;

        // build the maintenance start and end time for each selected unit
        var seasonStart = sourceData.Keys.Min().Date;
        var maintenanceWindows = selectedMaintenances.ToDictionary(
            unit => unit.Unit.Name,
            unit =>
            {
                var start = seasonStart.AddDays(Math.Clamp(unit.MaintenanceStartDayIndex, 0, 3));
                var end = start.AddHours(unit.MaintenanceDuration);
                return (Start: start, End: end);
            });

        double maxShortage = 0;
        DateTime worstHour = default;

        // check every hour and find the biggest heat shortage
        foreach (var hour in sourceData.OrderBy(item => item.Key))
        {
            var availableCapacity = scenarioConfigs.Sum(config =>
            {
                if (!maintenanceWindows.TryGetValue(config.Unit.Name, out var window))
                    return config.Unit.MaxHeatMW;

                // A unit gives no heat while it is in maintenance
                var inMaintenance = hour.Key >= window.Start && hour.Key < window.End;
                return inMaintenance ? 0 : config.Unit.MaxHeatMW;
            });

            var shortage = hour.Value.HeatDemand - availableCapacity;
            if (shortage > maxShortage)
            {
                maxShortage = shortage;
                worstHour = hour.Key;
            }
        }

        if (maxShortage > 0)
        {
            // add a warning when demand is higher than available capacity
            warnings.Add(
                $"{label}: worst shortage {maxShortage:F1} MW at {worstHour:yyyy-MM-dd HH:mm}.");
        }
    }
}
