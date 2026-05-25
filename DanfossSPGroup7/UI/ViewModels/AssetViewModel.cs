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
    [ObservableProperty] private int _selectedScenario = 1; // make the default scenario 1
    [ObservableProperty] private ObservableCollection<UnitConfigViewModel> _displayUnits = new();
    [ObservableProperty] private string _maintenanceWarning = string.Empty;
    public IReadOnlyList<string> MaintenanceStartDayOptions { get; } =
        new[] { "1st day", "2nd day", "3rd day", "4th day" };

    public AssetViewModel(Optimizer optimizer, IAssetManager assetManager)
    {
        _optimizer = optimizer;
        _assetManager = assetManager;
        var allUnits = _assetManager.GetProductionUnits();
        // load all units so the checkboxes/maintenance slider dont reset
        // scenario 1 config (GB1, GB2, GB3, OB1)
        var s1Names = new[] { "GB1", "GB2", "GB3", "OB1" };
        foreach (var unit in allUnits.Where(u => s1Names.Contains(u.Name)))
        {
            var unitConfig = new UnitConfigViewModel(unit);
            unitConfig.ConfigChanged += RefreshMaintenanceWarning;
            _scenario1Configs.Add(unitConfig);
        }

        // scenario 2 config (GM1, EB1, GB1, GB3)
        var s2Names = new[] { "GM1", "EB1", "GB1", "GB3" };
        foreach (var unit in allUnits.Where(u => s2Names.Contains(u.Name)))
        {
            // We create NEW instances here so they are independent of Scenario 1
            var unitConfig = new UnitConfigViewModel(unit);
            unitConfig.ConfigChanged += RefreshMaintenanceWarning;
            _scenario2Configs.Add(unitConfig);
        }
        LoadScenario(1);
    }

    [RelayCommand]
    public void SwitchScenario(string scenarioNumber)
    {    
        SelectedScenario = int.Parse(scenarioNumber);
        LoadScenario(SelectedScenario);
    }

    public void LoadScenario(int scenario)
    {
        SelectedScenario = scenario;
        
        // preserve checkboxes/sliders in the list that is hidden
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
        return scenario == 1 ? _scenario1Configs : _scenario2Configs;
    }

    private DateTime GetSourceStartDate(bool isSummer)
    {
        var sourceData = isSummer ? _optimizer.Summer : _optimizer.Winter;
        if (sourceData.Count == 0)
            return new DateTime(2026, 1, 5);

        return sourceData.Keys.Min().Date;
    }

    public void PrepareOptimization(int scenario, bool isSummer)
    {
        // reset all previous maintenance every time results are recalculated
        foreach (var unit in _optimizer.ProductionUnits)
        {
            unit.ClearMaintenance();
        }

        var scenarioConfigs = GetScenarioConfigs(scenario);
        var selectedUnits = scenarioConfigs.Where(unit => unit.IsSelectedForMaintenance).ToList();

        foreach (var selectedUnit in selectedUnits)
        {
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
        MaintenanceWarning = BuildMaintenanceWarning(SelectedScenario);
    }

    private string BuildMaintenanceWarning(int scenario)
    {
        var scenarioConfigs = GetScenarioConfigs(scenario).ToList();
        var selectedMaintenances = scenarioConfigs
            .Where(unit => unit.IsSelectedForMaintenance)
            .ToList();

        if (selectedMaintenances.Count == 0)
            return string.Empty;

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

        foreach (var hour in sourceData.OrderBy(item => item.Key))
        {
            var availableCapacity = scenarioConfigs.Sum(config =>
            {
                if (!maintenanceWindows.TryGetValue(config.Unit.Name, out var window))
                    return config.Unit.MaxHeatMW;

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
            warnings.Add(
                $"{label}: worst shortage {maxShortage:F1} MW at {worstHour:yyyy-MM-dd HH:mm}.");
        }
    }
}
