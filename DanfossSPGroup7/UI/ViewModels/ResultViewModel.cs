using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DanfossSPGroup7.Domain;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Text;
namespace DanfossSPGroup7.UI.ViewModels;

public partial class ResultViewModel : ObservableObject
{
    [ObservableProperty] private string resultsText = "No results yet.";

    [ObservableProperty] private ISeries[] series = Array.Empty<ISeries>();

    [ObservableProperty] private ISeries[] heatDemandSeries = Array.Empty<ISeries>();

    [ObservableProperty] private ISeries[] co2Series = Array.Empty<ISeries>();

    public Axis[] XAxes { get; set; } = Array.Empty<Axis>();

    public Axis[] YAxes { get; set; } = Array.Empty<Axis>();

    public Axis[] HeatDemandXAxes { get; set; } = Array.Empty<Axis>();

    public Axis[] HeatDemandYAxes { get; set; } = Array.Empty<Axis>();

    public Axis[] Co2XAxes { get; set; } = Array.Empty<Axis>();

    public Axis[] Co2YAxes { get; set; } = Array.Empty<Axis>();

    private readonly List<string> _allowedUnitNames = new();

    public ResultViewModel()
    {
        // Default to Scenario 1, Winter
        try
        {
            SetupAxes();
            SetupHeatDemandAxes();
            SetupCo2Axes();
            LoadReport(1, false, new List<string> { "GB1", "GB2", "GB3", "OB1" });
        }
        catch (Exception ex)
        {
            ResultsText = "RESULTS ERROR:\n" + ex.ToString();
            Series = Array.Empty<ISeries>();
        }
    }

    public ResultViewModel(int scenarioNumber, bool isSummer, List<string> allowedUnitNames)
    {
        try
        {
            SetupAxes();
            SetupHeatDemandAxes();
            SetupCo2Axes();
            _allowedUnitNames = allowedUnitNames ?? new List<string>();
            LoadReport(scenarioNumber, isSummer, _allowedUnitNames);
        }
        catch (Exception ex)
        {
            ResultsText = "RESULTS ERROR:\n" + ex;
            Series = Array.Empty<ISeries>();
        }
    }

    private void SetupAxes()
    {
        XAxes = new Axis[]
        {
            new Axis
            {
                Name = "Time",
                UnitWidth = 24,
                MinStep = 24,
                ForceStepToMin = true,
                Labeler = value => $"Day {(int)value / 24 + 1}"
            }
        };

        YAxes = new Axis[]
        {
            new Axis
            {
                Name = "Cost",
                Labeler = value => $"{value:N0} DKK"
            }
        };
    }


    private void LoadHeatDemandGraph(bool isSummer, int scenarioNumber)
    {
        if (Optimizer.Instance == null)
        {
            HeatDemandSeries = Array.Empty<ISeries>();
            return;
        }

        var sourceData = isSummer ? Optimizer.Instance.Summer : Optimizer.Instance.Winter;

        var orderedPoints = sourceData.OrderBy(kvp => kvp.Key).Take(336).ToList();

        var demandValues = orderedPoints.Select(kvp => kvp.Value.HeatDemand).ToArray();

        HeatDemandSeries = new ISeries[]
        {
        new LineSeries<double>
        {
            Values = demandValues,
            Name = isSummer? $"Scenario {scenarioNumber} Summer Heat Demand": $"Scenario {scenarioNumber} Winter Heat Demand",
            GeometrySize = 0
        }
        };
    }

    private void SetupHeatDemandAxes()
    {
        HeatDemandXAxes = new Axis[]
        {
        new Axis
        {
            Name = "Time",
            UnitWidth = 24,
            MinStep = 24,
            ForceStepToMin = true,
            Labeler = value => $"Day {(int)value / 24 + 1}"
        }
        };

        HeatDemandYAxes = new Axis[]
        {
        new Axis
        {
            Name = "Heat Demand (MW)",
            Labeler = value => $"{value:F1}"
        }
        };
    }

    private void SetupCo2Axes()
    {
        Co2XAxes = new Axis[]
        {
            new Axis
            {
                Name = "Time",
                UnitWidth = 24,
                MinStep = 24,
                ForceStepToMin = true,
                Labeler = value => $"Day {(int)value / 24 + 1}"
            }
        };

        Co2YAxes = new Axis[]
        {
            new Axis
            {
                Name = "CO2 Emissions (kg/h)",
                Labeler = value => $"{value:N0}"
            }
        };
    }

    private void LoadCo2Graph(bool isSummer, int scenarioNumber, List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results)
    {
        double[] hourlyCo2 = new double[results.Count];

        for (int i = 0; i < results.Count; i++)
        {
            double totalCo2ForThisHour = 0;

            foreach (var item in results[i].Schedule)
            {
                totalCo2ForThisHour += item.HeatMW * item.Co2;
            }

            hourlyCo2[i] = totalCo2ForThisHour;
        }

        Co2Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = hourlyCo2,
                Name = isSummer ? $"Scenario {scenarioNumber} Summer CO2" : $"Scenario {scenarioNumber} Winter CO2",
                GeometrySize = 0
            }
        };
    }

    public void LoadReport(int scenarioNumber, bool isSummer, List<string> allowedUnitNames)
    {
        if (Optimizer.Instance == null)
        {
            ResultsText = "Optimizer is not initialized.";
            Series = Array.Empty<ISeries>();
            return;
        }

        if (scenarioNumber != 1)
        {
            ResultsText = "Scenario 2 is not implemented yet.";
            Series = Array.Empty<ISeries>();
            return;
        }

        var results = Optimizer.Instance.RunScenario1(isSummer, allowedUnitNames);

        // Limit to 14 days (336 hours) - the CSV data contains exactly 14 days
        if (results.Count > 336)
        {
            results = results.Take(336).ToList();
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Scenario 1 - {(isSummer ? "Summer" : "Winter")}");
        sb.AppendLine();

        foreach (var hour in results.Where(r => r.Hour >= new DateTime(2026, 1, 7, 0, 0, 0)).Take(96))
        {
            sb.AppendLine($"{hour.Hour:yyyy-MM-dd HH:mm}");

            foreach (var item in hour.Schedule)
            {
                sb.AppendLine($"  {item.Unit.Name}: {item.HeatMW:F2} MW");
            }

            sb.AppendLine();
        }

        ResultsText = sb.ToString();

        double[] hourlyCosts = new double[results.Count];

        for (int i = 0; i < results.Count; i++)
        {
            double totalCostForThisHour = 0;

            foreach (var item in results[i].Schedule)
            {
                totalCostForThisHour += item.HeatMW * item.Unit.ProductionCost;
            }

            hourlyCosts[i] = totalCostForThisHour;
        }

        Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = hourlyCosts,
                Name = isSummer ? "Scenario 1 Summer Cost" : "Scenario 1 Winter Cost",
                GeometrySize = 0
            }
        };
        LoadHeatDemandGraph(isSummer, scenarioNumber);
        LoadCo2Graph(isSummer, scenarioNumber, results);
    }
}
