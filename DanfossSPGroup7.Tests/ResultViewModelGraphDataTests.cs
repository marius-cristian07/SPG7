using System;
using System.Collections.Generic;
using System.Linq;
using DanfossSPGroup7.Domain;
using Xunit;

namespace DanfossSPGroup7.Tests;

//Tests chart numeric rules mirrored from <c>ResultViewModel</c> (heat demand, CO₂, net cost, production stack).
public class ResultViewModelGraphDataTests
{
    //Same data shaping as the result charts; keep aligned with the view model if logic changes.
    private static class MirrorResultViewModelGraphs
    {
        public static double[] ExtractHeatDemandSeries(
            IReadOnlyDictionary<DateTime, DataPoint> sourceData,
            int maxHours = 336) =>
            sourceData
                .OrderBy(kvp => kvp.Key)
                .Take(maxHours)
                .Select(kvp => kvp.Value.HeatDemand)
                .ToArray();

        public static double[] ComputeHourlyCo2Totals(
            IReadOnlyList<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results)
        {
            var hourlyCo2 = new double[results.Count];
            for (var i = 0; i < results.Count; i++)
                hourlyCo2[i] = results[i].Schedule.Sum(x => x.HeatMW * x.Co2);
            return hourlyCo2;
        }

        public static double[] ComputeHourlyWeightedAverageNetCost(
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

            return values;
        }

        public static (
            IReadOnlyList<(string UnitName, double[] Values)> UnitSeries,
            double[] HeatDemandLine) BuildHeatProductionChartData(
            IReadOnlyDictionary<DateTime, DataPoint> sourceData,
            IReadOnlyList<string> allowedUnitNames,
            IReadOnlyList<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results)
        {
            var distinctUnits = allowedUnitNames.Distinct().ToList();
            var unitHeatValues = distinctUnits.ToDictionary(u => u, _ => new double[results.Count]);

            for (var i = 0; i < results.Count; i++)
            {
                foreach (var item in results[i].Schedule)
                {
                    if (unitHeatValues.TryGetValue(item.Unit.Name, out var values))
                        values[i] = item.HeatMW;
                }
            }

            var series = distinctUnits
                .Select(u => (UnitName: u, Values: unitHeatValues[u]))
                .ToList();

            var heatDemandLine = results
                .Select(result => sourceData[result.Hour].HeatDemand)
                .ToArray();

            return (series, heatDemandLine);
        }
    }

    // production unit for test schedules.
    private static ProductionUnit Unit(string name, double maxHeat = 10, double elecMw = 1, double prodCost = 100) =>
        new()
        {
            Name = name,
            MaxHeatMW = maxHeat,
            ElectricityMW = elecMw,
            ProductionCost = prodCost
        };

    //Heat demand series is sorted by timestamp, not dictionary insertion order.
    [Fact]
    public void HeatDemandGraph_Positive_ReturnsChronologicalHeatDemand()
    {
        var t0 = new DateTime(2026, 1, 2, 0, 0, 0);
        var t1 = new DateTime(2026, 1, 1, 0, 0, 0);
        var t2 = new DateTime(2026, 1, 3, 0, 0, 0);

        var source = new Dictionary<DateTime, DataPoint>
        {
            [t0] = new DataPoint { HeatDemand = 5, ElectricityPrice = 1 },
            [t1] = new DataPoint { HeatDemand = 10, ElectricityPrice = 2 },
            [t2] = new DataPoint { HeatDemand = 7, ElectricityPrice = 3 }
        };

        var series = MirrorResultViewModelGraphs.ExtractHeatDemandSeries(source, maxHours: 10);
        Assert.Equal(new[] { 10.0, 5, 7 }, series);
    }

    //Null source data must fail fast (same LINQ behavior as the real chart path).
    [Fact]
    public void HeatDemandGraph_Negative_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MirrorResultViewModelGraphs.ExtractHeatDemandSeries(null!));
    }

    //No source rows yields an empty demand series (nothing to plot).
    [Fact]
    public void HeatDemandGraph_Edge_EmptySource_ReturnsEmptyArray()
    {
        var series = MirrorResultViewModelGraphs.ExtractHeatDemandSeries(new Dictionary<DateTime, DataPoint>());
        Assert.Empty(series);
    }

    //Each hour’s CO₂ line point equals the sum of HeatMW × Co2 over that hour’s schedule.
    [Fact]
    public void Co2Graph_Positive_SumsHeatWeightedEmissions()
    {
        var u1 = Unit("A", maxHeat: 10);
        var hour = new DateTime(2026, 1, 1, 0, 0, 0);
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (hour, new List<(ProductionUnit, double, double)> { (u1, 2, 100), (u1, 3, 50) })
        };

        var totals = MirrorResultViewModelGraphs.ComputeHourlyCo2Totals(results);
        Assert.Single(totals);
        Assert.Equal(2 * 100 + 3 * 50, totals[0]);
    }

    ///No optimization hours means no CO₂ series values.
    [Fact]
    public void Co2Graph_Negative_EmptyResults_ReturnsEmptyArray()
    {
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>();
        Assert.Empty(MirrorResultViewModelGraphs.ComputeHourlyCo2Totals(results));
    }

    //An hour with an empty schedule contributes 0 to the CO₂ series (no crash).
    [Fact]
    public void Co2Graph_Edge_EmptyScheduleHour_YieldsZero()
    {
        var hour = new DateTime(2026, 1, 1, 1, 0, 0);
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (hour, new List<(ProductionUnit, double, double)>())
        };

        var totals = MirrorResultViewModelGraphs.ComputeHourlyCo2Totals(results);
        Assert.Single(totals);
        Assert.Equal(0, totals[0]);
    }

    //Net cost per hour is heat-weighted average of each unit’s net production cost at that hour’s electricity price.
    [Fact]
    public void NetCostGraph_Positive_MatchesHeatWeightedAverageNetCost()
    {
        var hour = new DateTime(2026, 3, 1, 12, 0, 0);
        var uCheap = Unit("Cheap", maxHeat: 10, elecMw: 0, prodCost: 50);
        var uExp = Unit("Exp", maxHeat: 10, elecMw: 0, prodCost: 150);

        var source = new Dictionary<DateTime, DataPoint>
        {
            [hour] = new DataPoint { HeatDemand = 100, ElectricityPrice = 0 }
        };

        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (hour, new List<(ProductionUnit, double, double)> { (uCheap, 4, 0), (uExp, 2, 0) })
        };

        var netCosts = MirrorResultViewModelGraphs.ComputeHourlyWeightedAverageNetCost(source, results);
        var expected = (4 * 50 + 2 * 150) / 6.0;
        Assert.Single(netCosts);
        Assert.Equal(expected, netCosts[0], precision: 9);
    }

    //Missing source data for a result hour throws when resolving electricity price.
    [Fact]
    public void NetCostGraph_Negative_ThrowsWhenHourMissingFromSource()
    {
        var hour = new DateTime(2026, 1, 1, 0, 0, 0);
        var source = new Dictionary<DateTime, DataPoint>();
        var u = Unit("U");
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (hour, new List<(ProductionUnit, double, double)> { (u, 1, 0) })
        };

        Assert.Throws<KeyNotFoundException>(() =>
            MirrorResultViewModelGraphs.ComputeHourlyWeightedAverageNetCost(source, results));
    }

    //An hour with no dispatched units yields net cost 0 for that point.
    [Fact]
    public void NetCostGraph_Edge_EmptySchedule_ReturnsZero()
    {
        var hour = new DateTime(2026, 1, 1, 0, 0, 0);
        var source = new Dictionary<DateTime, DataPoint>
        {
            [hour] = new DataPoint { HeatDemand = 20, ElectricityPrice = 100 }
        };
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (hour, new List<(ProductionUnit, double, double)>())
        };

        var values = MirrorResultViewModelGraphs.ComputeHourlyWeightedAverageNetCost(source, results);
        Assert.Single(values);
        Assert.Equal(0, values[0]);
    }

    //Stacked series get per-unit MW per hour; overlay line matches source heat demand at each result hour.
    [Fact]
    public void HeatProductionGraph_Positive_MapsUnitsAndDemandLine()
    {
        var h0 = new DateTime(2026, 1, 1, 0, 0, 0);
        var h1 = new DateTime(2026, 1, 1, 1, 0, 0);
        var gb1 = Unit("GB1");
        var gb2 = Unit("GB2");

        var source = new Dictionary<DateTime, DataPoint>
        {
            [h0] = new DataPoint { HeatDemand = 100, ElectricityPrice = 1 },
            [h1] = new DataPoint { HeatDemand = 200, ElectricityPrice = 2 }
        };

        var allowed = new List<string> { "GB1", "GB2" };
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (h0, new List<(ProductionUnit, double, double)> { (gb1, 10, 0), (gb2, 5, 0) }),
            (h1, new List<(ProductionUnit, double, double)> { (gb1, 3, 0), (gb2, 7, 0) })
        };

        var (unitSeries, demandLine) =
            MirrorResultViewModelGraphs.BuildHeatProductionChartData(source, allowed, results);

        Assert.Equal(2, unitSeries.Count);
        var g1 = unitSeries.First(s => s.UnitName == "GB1").Values;
        var g2 = unitSeries.First(s => s.UnitName == "GB2").Values;
        Assert.Equal(new[] { 10.0, 3 }, g1);
        Assert.Equal(new[] { 5.0, 7 }, g2);
        Assert.Equal(new[] { 100.0, 200 }, demandLine);
    }

    //Demand overlay lookup fails if the result hour is absent from source data.
    [Fact]
    public void HeatProductionGraph_Negative_ThrowsWhenResultHourMissingFromSource()
    {
        var hour = new DateTime(2026, 1, 1, 0, 0, 0);
        var source = new Dictionary<DateTime, DataPoint>();
        var u = Unit("GB1");
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (hour, new List<(ProductionUnit, double, double)> { (u, 1, 0) })
        };

        Assert.Throws<KeyNotFoundException>(() =>
            MirrorResultViewModelGraphs.BuildHeatProductionChartData(source, new List<string> { "GB1" }, results));
    }

    //Only allowed unit names get a stack series; other units’ MW are ignored for the chart.
    [Fact]
    public void HeatProductionGraph_Edge_UnitNotInAllowedList_IsIgnored()
    {
        var hour = new DateTime(2026, 1, 1, 0, 0, 0);
        var hidden = Unit("Hidden");
        var visible = Unit("GB1");
        var source = new Dictionary<DateTime, DataPoint>
        {
            [hour] = new DataPoint { HeatDemand = 50, ElectricityPrice = 1 }
        };
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (hour, new List<(ProductionUnit, double, double)> { (hidden, 99, 0), (visible, 2, 0) })
        };

        var (unitSeries, demandLine) = MirrorResultViewModelGraphs.BuildHeatProductionChartData(
            source,
            new List<string> { "GB1" },
            results);

        Assert.Single(unitSeries);
        Assert.Equal("GB1", unitSeries[0].UnitName);
        Assert.Equal(new[] { 2.0 }, unitSeries[0].Values);
        Assert.Equal(new[] { 50.0 }, demandLine);
    }
}
