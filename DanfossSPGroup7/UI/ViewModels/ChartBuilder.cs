using System;
using System.Collections.Generic;
using System.Linq;
using DanfossSPGroup7.Data;
using DanfossSPGroup7.Domain;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace DanfossSPGroup7.UI.ViewModels
{
    public static class ChartBuilder
    {
        public static ISeries[] BuildHeatProductionSeries(
            IReadOnlyDictionary<DateTime, DataPoint> sourceData,
            IReadOnlyList<string> allowedUnitNames,
            IReadOnlyList<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results)
        {
            var distinctUnitNames = allowedUnitNames.Distinct().ToList();
            var unitHeatValues = distinctUnitNames
                .ToDictionary(unitName => unitName, _ => new double[results.Count]);

            for (var i = 0; i < results.Count; i++)
            {
                foreach (var item in results[i].Schedule)
                {
                    if (unitHeatValues.TryGetValue(item.Unit.Name, out var values))
                    {
                        values[i] = item.HeatMW;
                    }
                }
            }

            var chartSeries = new List<ISeries>();

            foreach (var unitName in distinctUnitNames)
            {
                var color = GetUnitColor(unitName);

                chartSeries.Add(new StackedAreaSeries<double>
                {
                    Values = unitHeatValues[unitName],
                    Name = unitName,
                    GeometrySize = 0,
                    LineSmoothness = 0.35,
                    Fill = new SolidColorPaint(color),
                    Stroke = new SolidColorPaint(color) { StrokeThickness = 2 }
                });
            }

            var heatDemandValues = results
                .Select(result => sourceData[result.Hour].HeatDemand)
                .ToArray();

            chartSeries.Add(new LineSeries<double>
            {
                Values = heatDemandValues,
                Name = "Heat Demand",
                GeometrySize = 0,
                LineSmoothness = 0.45,
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Black) { StrokeThickness = 6 }
            });

            return chartSeries.ToArray();
        }

        public static ISeries[] BuildHeatDemandSeries(
            bool isSummer,
            int scenarioNumber,
            IReadOnlyDictionary<DateTime, DataPoint> sourceData)
        {
            var demandValues = sourceData
                .OrderBy(kvp => kvp.Key)
                .Take(336)
                .Select(kvp => kvp.Value.HeatDemand)
                .ToArray();

            return new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = demandValues,
                    Name = $"Scenario {scenarioNumber} {(isSummer ? "Summer" : "Winter")} Heat Demand",
                    GeometrySize = 0,
                    LineSmoothness = 0.45,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.Black) { StrokeThickness = 5 }
                }
            };
        }

        public static ISeries[] BuildCo2Series(
            bool isSummer,
            int scenarioNumber,
            IReadOnlyList<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results)
        {
            var hourlyCo2 = new double[results.Count];

            for (var i = 0; i < results.Count; i++)
            {
                hourlyCo2[i] = results[i].Schedule.Sum(x => x.HeatMW * x.Co2);
            }

            return new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = hourlyCo2,
                    Name = $"Scenario {scenarioNumber} {(isSummer ? "Summer" : "Winter")} CO2",
                    GeometrySize = 0,
                    LineSmoothness = 0.45,
                    Fill = null,
                    Stroke = new SolidColorPaint(new SKColor(110, 110, 110)) { StrokeThickness = 3 }
                }
            };
        }

        public static ISeries[] BuildNetCostSeries(
            IReadOnlyDictionary<DateTime, DataPoint> sourceData,
            IReadOnlyList<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results)
        {
            var values = new double[results.Count];

            for (var i = 0; i < results.Count; i++)
            {
                if (results[i].Schedule.Count == 0)
                {
                    values[i] = 0;
                    continue;
                }

                var electricityPrice = sourceData[results[i].Hour].ElectricityPrice;
                double totalHeat = 0;
                double totalCost = 0;

                foreach (var item in results[i].Schedule)
                {
                    var netCost = Optimizer.CalculateNetProductionCost(item.Unit, electricityPrice);
                    totalHeat += item.HeatMW;
                    totalCost += item.HeatMW * netCost;
                }

                values[i] = totalHeat > 0 ? totalCost / totalHeat : 0;
            }

            return new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = values,
                    Name = "Net Production Cost",
                    GeometrySize = 0,
                    LineSmoothness = 0.45,
                    Fill = null,
                    Stroke = new SolidColorPaint(new SKColor(52, 73, 94)) { StrokeThickness = 3 }
                }
            };
        }

        public static ISeries[] BuildElectricitySeries(
            bool isSummer,
            int scenarioNumber,
            IReadOnlyDictionary<DateTime, DataPoint> sourceData)
        {
            if (scenarioNumber != 2)
            {
                return Array.Empty<ISeries>();
            }

            var electricityValues = sourceData
                .OrderBy(kvp => kvp.Key)
                .Take(336)
                .Select(kvp => kvp.Value.ElectricityPrice)
                .ToArray();

            return new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = electricityValues,
                    Name = $"Scenario {scenarioNumber} {(isSummer ? "Summer" : "Winter")} Electricity Price",
                    GeometrySize = 0,
                    LineSmoothness = 0.45,
                    Fill = null,
                    Stroke = new SolidColorPaint(new SKColor(214, 137, 16)) { StrokeThickness = 3 }
                }
            };
        }

        private static SKColor GetUnitColor(string unitName)
        {
            return unitName switch
            {
                "GB1" => new SKColor(244, 183, 126),
                "GB2" => new SKColor(230, 126, 34),
                "GB3" => new SKColor(128, 65, 32),
                "OB1" => new SKColor(128, 128, 128),
                "GM1" => new SKColor(52, 152, 219),
                "EB1" => new SKColor(46, 204, 113),
                _ => new SKColor(100, 100, 100)
            };
        }
    }
}
