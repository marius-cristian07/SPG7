using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanfossSPGroup7.Domain;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace DanfossSPGroup7.UI.ViewModels;

public partial class ResultViewModel : ObservableObject
{
    private readonly Optimizer _optimizer;
    private readonly AssetViewModel? _assetPage;
    private List<string> _currentUnitNames = new();

    [ObservableProperty] private string resultsText = "No results yet.";
    [ObservableProperty] private ISeries[] series = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] heatDemandSeries = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] co2Series = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] netProductionCostSeries = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] electricitySeries = Array.Empty<ISeries>();
    [ObservableProperty] private bool isScenario2;
    [ObservableProperty] private int currentScenario = 1;
    [ObservableProperty] private bool currentIsSummer;

    public Axis[] XAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] YAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] HeatDemandXAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] HeatDemandYAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] Co2XAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] Co2YAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] NetCostXAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] NetCostYAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] ElectricityXAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] ElectricityYAxes { get; set; } = Array.Empty<Axis>();

    public bool IsScenario1Selected => CurrentScenario == 1;
    public bool IsScenario2Selected => CurrentScenario == 2;
    public bool IsWinterSelected => !CurrentIsSummer;
    public bool IsSummerSelected => CurrentIsSummer;

    public ResultViewModel(Optimizer optimizer)
        : this(optimizer, null, 1, false, new List<string> { "GB1", "GB2", "GB3", "OB1" })
    {
    }

    public ResultViewModel(Optimizer optimizer, AssetViewModel? assetPage, int scenarioNumber, bool isSummer, List<string> allowedUnitNames)
    {
        _optimizer = optimizer;
        _assetPage = assetPage;

        try
        {
            SetupAxes();
            CurrentScenario = scenarioNumber;
            CurrentIsSummer = isSummer;
            _currentUnitNames = allowedUnitNames ?? new List<string>();
            LoadReport(CurrentScenario, CurrentIsSummer, _currentUnitNames);
        }
        catch (Exception ex)
        {
            ResultsText = "RESULTS ERROR:\n" + ex;
            Series = Array.Empty<ISeries>();
        }
    }

    [RelayCommand]
    public void ChangeScenario(string scenarioNum)
    {
        CurrentScenario = int.Parse(scenarioNum);

        if (_assetPage != null)
        {
            _currentUnitNames = _assetPage.GetSelectedUnitNames(CurrentScenario);
        }
        else
        {
            _currentUnitNames = CurrentScenario == 1
                ? new List<string> { "GB1", "GB2", "GB3", "OB1" }
                : new List<string> { "GM1", "EB1", "GB1", "GB3" };
        }

        LoadReport(CurrentScenario, CurrentIsSummer, _currentUnitNames);
    }

    [RelayCommand]
    public void ChangeSeason(string isSummerStr)
    {
        CurrentIsSummer = bool.Parse(isSummerStr);
        LoadReport(CurrentScenario, CurrentIsSummer, _currentUnitNames);
    }

    partial void OnCurrentScenarioChanged(int value)
    {
        IsScenario2 = value == 2;
        OnPropertyChanged(nameof(IsScenario1Selected));
        OnPropertyChanged(nameof(IsScenario2Selected));
    }

    partial void OnCurrentIsSummerChanged(bool value)
    {
        OnPropertyChanged(nameof(IsWinterSelected));
        OnPropertyChanged(nameof(IsSummerSelected));
    }

    public void LoadReport(int scenarioNumber, bool isSummer, List<string> allowedUnitNames)
    {
        CurrentScenario = scenarioNumber;
        CurrentIsSummer = isSummer;

        if (_assetPage != null)
        {
            _assetPage.PrepareOptimization(scenarioNumber, isSummer);
            allowedUnitNames = _assetPage.GetSelectedUnitNames(scenarioNumber);
            _currentUnitNames = allowedUnitNames;
        }

        List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results;
        if (scenarioNumber == 1)
        {
            results = _optimizer.RunScenario1(isSummer, allowedUnitNames);
        }
        else if (scenarioNumber == 2)
        {
            results = _optimizer.RunScenario2(isSummer, allowedUnitNames);
        }
        else
        {
            ResultsText = "Invalid scenario selected.";
            Series = Array.Empty<ISeries>();
            return;
        }

        if (results.Count > 336)
        {
            results = results.Take(336).ToList();
        }

        var sourceData = isSummer ? _optimizer.Summer : _optimizer.Winter;

        ResultsText = ResultFormatter.BuildTextReport(
            scenarioNumber,
            isSummer,
            results,
            _optimizer.ProductionUnits);

        Series = ChartBuilder.BuildHeatProductionSeries(sourceData, allowedUnitNames, results);
        HeatDemandSeries = ChartBuilder.BuildHeatDemandSeries(isSummer, scenarioNumber, sourceData);
        Co2Series = ChartBuilder.BuildCo2Series(isSummer, scenarioNumber, results);
        NetProductionCostSeries = ChartBuilder.BuildNetCostSeries(sourceData, results);
        ElectricitySeries = ChartBuilder.BuildElectricitySeries(isSummer, scenarioNumber, sourceData);
    }

    private void SetupAxes()
    {
        (XAxes, YAxes) = CreateTimeAxes("Heat Production (MW)", value => $"{value:F1}");
        XAxes[0].Labeler = BuildDayDateLabel;

        (HeatDemandXAxes, HeatDemandYAxes) = CreateTimeAxes("Heat Demand (MW)", value => $"{value:F1}");
        HeatDemandXAxes[0].Labeler = BuildDayDateLabel;

        (Co2XAxes, Co2YAxes) = CreateTimeAxes("CO2 Emissions (kg/h)", value => $"{value:N0}");
        Co2XAxes[0].Labeler = BuildDayDateLabel;

        (NetCostXAxes, NetCostYAxes) = CreateTimeAxes("Net Production Cost", value => $"{value:N0} DKK/MWh");
        NetCostXAxes[0].Labeler = BuildDayDateLabel;

        (ElectricityXAxes, ElectricityYAxes) = CreateTimeAxes("Electricity Price (DKK/MWh)", value => $"{value:N0}");
        ElectricityXAxes[0].Labeler = BuildDayDateLabel;
    }

    private static (Axis[] xAxes, Axis[] yAxes) CreateTimeAxes(string yName, Func<double, string> yLabeler)
    {
        var xAxes = new Axis[]
        {
            new Axis
            {
                Name = "Time",
                UnitWidth = 24,
                MinStep = 24,
                ForceStepToMin = true
            }
        };

        var yAxes = new Axis[]
        {
            new Axis
            {
                Name = yName,
                Labeler = yLabeler
            }
        };

        return (xAxes, yAxes);
    }

    private string BuildDayDateLabel(double value)
    {
        var dayIndex = (int)value / 24;
        var dayNumber = dayIndex + 1;

        var sourceData = CurrentIsSummer ? _optimizer.Summer : _optimizer.Winter;
        if (sourceData.Count == 0)
        {
            return $"Day {dayNumber}";
        }

        var startDate = sourceData.Keys.Min().Date;
        var dayDate = startDate.AddDays(dayIndex);
        return $"Day {dayNumber}\r\n({dayDate:dd/MM})";
    }
}
