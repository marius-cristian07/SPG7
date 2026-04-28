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

    [ObservableProperty] private ISeries[] netProductionCostSeries = Array.Empty<ISeries>();

    [ObservableProperty] private bool isScenario2;
    

    public Axis[] XAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] YAxes { get; set; } = Array.Empty<Axis>();

    public Axis[] HeatDemandXAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] HeatDemandYAxes { get; set; } = Array.Empty<Axis>();

    public Axis[] Co2XAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] Co2YAxes { get; set; } = Array.Empty<Axis>();

    public Axis[] NetCostXAxes { get; set; } = Array.Empty<Axis>();
    public Axis[] NetCostYAxes { get; set; } = Array.Empty<Axis>();

    private readonly List<string> _allowedUnitNames = new();

    public ResultViewModel()
    {
        try
        {
            SetupAxes();
            SetupHeatDemandAxes();
            SetupCo2Axes();
            SetupNetCostAxes();

            LoadReport(1, false, new List<string> { "GB1", "GB2", "GB3", "OB1" });
        }
        catch (Exception ex)
        {
            ResultsText = "RESULTS ERROR:\n" + ex;
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
            SetupNetCostAxes();

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

    private void SetupNetCostAxes()
    {
        NetCostXAxes = new Axis[]
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

        NetCostYAxes = new Axis[]
        {
            new Axis
            {
                Name = "Net Production Cost",
                Labeler = value => $"{value:N0} DKK/MWh"
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

        var sourceData = isSummer
            ? Optimizer.Instance.Summer
            : Optimizer.Instance.Winter;

        var demandValues = sourceData
            .OrderBy(kvp => kvp.Key)
            .Take(336)
            .Select(kvp => kvp.Value.HeatDemand)
            .ToArray();

        HeatDemandSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = demandValues,
                Name = $"Scenario {scenarioNumber} {(isSummer ? "Summer" : "Winter")} Heat Demand",
                GeometrySize = 0
            }
        };
    }

    private void LoadCo2Graph(
        bool isSummer,
        int scenarioNumber,
        List<(DateTime Hour,
        List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results)
    {
        double[] hourlyCo2 = new double[results.Count];

        for (int i = 0; i < results.Count; i++)
        {
            hourlyCo2[i] = results[i].Schedule.Sum(x => x.HeatMW * x.Co2);
        }

        Co2Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = hourlyCo2,
                Name = $"Scenario {scenarioNumber} {(isSummer ? "Summer" : "Winter")} CO2",
                GeometrySize = 0
            }
        };
    }

    private void LoadNetProductionCostGraph(
        bool isSummer,
        List<(DateTime Hour,
        List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results)
    {
        var sourceData = isSummer
            ? Optimizer.Instance.Summer
            : Optimizer.Instance.Winter;

        double[] values = new double[results.Count];

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].Schedule.Count == 0)
            {
                values[i] = 0;
                continue;
            }

            double electricityPrice =
                sourceData[results[i].Hour].ElectricityPrice;

            double totalHeat = 0;
            double totalCost = 0;

            foreach (var item in results[i].Schedule)
            {
                double netCost =
                    Optimizer.CalculateNetProductionCost(
                        item.Unit,
                        electricityPrice);

                totalHeat += item.HeatMW;
                totalCost += item.HeatMW * netCost;
            }

            values[i] = totalHeat > 0 ? totalCost / totalHeat : 0;
        }

        NetProductionCostSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = values,
                Name = "Scenario 2 Net Production Cost",
                GeometrySize = 0
            }
        };
    }

    public void LoadReport(int scenarioNumber, bool isSummer, List<string> allowedUnitNames)
    {

        IsScenario2 = scenarioNumber == 2;

        if (Optimizer.Instance == null)
        {
            ResultsText = "Optimizer is not initialized.";
            Series = Array.Empty<ISeries>();
            return;
        }

        List<(DateTime Hour,
        List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results;

        if (scenarioNumber == 1)
        {
            results = Optimizer.Instance.RunScenario1(isSummer, allowedUnitNames);
        }
        else if (scenarioNumber == 2)
        {
            results = Optimizer.Instance.RunScenario2(isSummer, allowedUnitNames);
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

        var sb = new StringBuilder();

        sb.AppendLine($"Scenario {scenarioNumber} - {(isSummer ? "Summer" : "Winter")}");
        sb.AppendLine();

        foreach (var hour in results.Take(96))
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
            double totalCost = 0;

            foreach (var item in results[i].Schedule)
            {
                if (scenarioNumber == 1)
                {
                    totalCost += item.HeatMW * item.Unit.ProductionCost;
                }
                else if (scenarioNumber == 2)
                {
                    var sourceData = isSummer
                        ? Optimizer.Instance.Summer
                        : Optimizer.Instance.Winter;

                    double electricityPrice =
                        sourceData[results[i].Hour].ElectricityPrice;

                    totalCost += item.HeatMW *
                        Optimizer.CalculateNetProductionCost(
                            item.Unit,
                            electricityPrice);
                }
            }

            hourlyCosts[i] = totalCost;
        }

        Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = hourlyCosts,
                Name = $"Scenario {scenarioNumber} {(isSummer ? "Summer" : "Winter")} Cost",
                GeometrySize = 0
            }
        };

        LoadHeatDemandGraph(isSummer, scenarioNumber);
        LoadCo2Graph(isSummer, scenarioNumber, results);

        if (scenarioNumber == 2)
        {
            LoadNetProductionCostGraph(isSummer, results);
        }
        else
        {
            NetProductionCostSeries = Array.Empty<ISeries>();
        }
    }
}