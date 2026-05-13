using System;
using System.Collections.Generic;
using System.Linq;
using DanfossSPGroup7.Domain;
using Xunit;

namespace DanfossSPGroup7.Tests;


//Tests the numeric rules behind the result charts (heat production stack, heat demand line, CO₂, net cost).

public class ResultViewModelGraphDataTests
{
    //Mirror of graph data steps in ResultViewModel (test assembly only).
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

    private static ProductionUnit Unit(string name, double maxHeat = 10, double elecMw = 1, double prodCost = 100) =>
        new()
        {
            Name = name,
            MaxHeatMW = maxHeat,
            ElectricityMW = elecMw,
            ProductionCost = prodCost
        };

    //Heat demand series

    //Inserts three timestamps out of order in the dictionary but demands 10, 5, 7. Asserts the series is 10, 5, 7 — proves ordering is by time, not insertion order.
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

    //Passes null as the source dictionary. LINQ’s OrderBy on null throws ArgumentNullException, documenting 
    // that bad callers fail fast.
    [Fact]
    public void HeatDemandGraph_Negative_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MirrorResultViewModelGraphs.ExtractHeatDemandSeries(null!));
    }

    //Empty dictionary → empty array (no points on the chart).
    [Fact]
    public void HeatDemandGraph_Edge_EmptySource_ReturnsEmptyArray()
    {
        var series = MirrorResultViewModelGraphs.ExtractHeatDemandSeries(new Dictionary<DateTime, DataPoint>());
        Assert.Empty(series);
    }

    //Builds 400 hourly keys; asks for maxHours: 336. Asserts length 336 and every value still 1 — same cap as the app’s Take(336).
    [Fact]
    public void HeatDemandGraph_Edge_TruncatesTo336Hours()
    {
        var source = Enumerable.Range(0, 400)
            .ToDictionary(
                i => new DateTime(2026, 1, 1).AddHours(i),
                _ => new DataPoint { HeatDemand = 1, ElectricityPrice = 0 });

        var series = MirrorResultViewModelGraphs.ExtractHeatDemandSeries(source, maxHours: 336);
        Assert.Equal(336, series.Length);
        Assert.All(series, v => Assert.Equal(1, v));
    }

    //Single hour with HeatDemand = 0. Ensures zero is kept (not filtered out), which matters for plotting flat demand.
    [Fact]
    public void HeatDemandGraph_Edge_IncludesZeroDemandHours()
    {
        var hour = new DateTime(2026, 6, 1, 0, 0, 0);
        var source = new Dictionary<DateTime, DataPoint>
        {
            [hour] = new DataPoint { HeatDemand = 0, ElectricityPrice = 50 }
        };

        var series = MirrorResultViewModelGraphs.ExtractHeatDemandSeries(source);
        Assert.Single(series);
        Assert.Equal(0, series[0]);
    }

    // CO₂ hourly totals

    //One hour, two rows: (2 MW × 100) + (3 MW × 50) = 350. Checks the formula.
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

    //No hours at all → no CO₂ points (empty double[]).
    [Fact]
    public void Co2Graph_Negative_EmptyResults_ReturnsEmptyArray()
    {
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>();
        Assert.Empty(MirrorResultViewModelGraphs.ComputeHourlyCo2Totals(results));
    }

    //One hour exists but schedule list is empty → sum is 0 (not an error).
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

    //Uses negative heat and negative CO₂ factors to show the code does not clamp; it blindly sums products (useful if 
    // data is bad or for sensitivity).
    [Fact]
    public void Co2Graph_Edge_NegativeHeatOrCo2_IsPropagatedInSum()
    {
        var u = Unit("X");
        var hour = new DateTime(2026, 1, 1, 0, 0, 0);
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (hour, new List<(ProductionUnit, double, double)> { (u, -1, 10), (u, 2, -5) })
        };

        var totals = MirrorResultViewModelGraphs.ComputeHourlyCo2Totals(results);
        Assert.Equal(-10 + 2 * -5, totals[0]);
    }


    //Net production cost

    //Two units, prices 50 and 150 DKK-ish style costs, 4 MW and 2 MW, electricity price 0 so net cost equals ProductionCost. 
    // Manual expectation (4×50 + 2×150) / 6.
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

    //Result references an hour that is not in source → dictionary lookup throws KeyNotFoundException (same as production if CSV keys 
    // and result hours diverge).
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

    //Source has that hour; schedule empty → 0 (no division).
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

    //Schedule has rows but 0 MW each → totalHeat is 0 → line value 0 (avoids 0/0).
    [Fact]
    public void NetCostGraph_Edge_AllZeroHeat_ReturnsZeroDespiteSchedule()
    {
        var hour = new DateTime(2026, 1, 1, 0, 0, 0);
        var u = Unit("Idle", maxHeat: 10, elecMw: 1, prodCost: 200);
        var source = new Dictionary<DateTime, DataPoint>
        {
            [hour] = new DataPoint { HeatDemand = 0, ElectricityPrice = 50 }
        };
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (hour, new List<(ProductionUnit, double, double)> { (u, 0, 0), (u, 0, 0) })
        };

        var values = MirrorResultViewModelGraphs.ComputeHourlyWeightedAverageNetCost(source, results);
        Assert.Equal(0, values[0]);
    }

    //Same unit, full 10 MW both hours; hour 0 price 0, hour 1 price 20. Asserts each plotted point equals 
    // Optimizer.CalculateNetProductionCost for that hour’s price — proves the graph follows hourly electricity price, 
    // not a global constant.
    [Fact]
    public void NetCostGraph_Edge_UsesHourSpecificElectricityPrice()
    {
        var h0 = new DateTime(2026, 1, 1, 0, 0, 0);
        var h1 = new DateTime(2026, 1, 1, 1, 0, 0);
        var u = Unit("Gen", maxHeat: 10, elecMw: 5, prodCost: 100);

        var source = new Dictionary<DateTime, DataPoint>
        {
            [h0] = new DataPoint { HeatDemand = 10, ElectricityPrice = 0 },
            [h1] = new DataPoint { HeatDemand = 10, ElectricityPrice = 20 }
        };

        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (h0, new List<(ProductionUnit, double, double)> { (u, 10, 0) }),
            (h1, new List<(ProductionUnit, double, double)> { (u, 10, 0) })
        };

        var values = MirrorResultViewModelGraphs.ComputeHourlyWeightedAverageNetCost(source, results);
        var net0 = Optimizer.CalculateNetProductionCost(u, 0);
        var net1 = Optimizer.CalculateNetProductionCost(u, 20);
        Assert.Equal(net0, values[0], precision: 9);
        Assert.Equal(net1, values[1], precision: 9);
    }


    //Heat production stack + demand overlay

    //Two hours, two units; checks GB1/GB2 arrays [10,3] and [5,7] and demand line [100,200] from source.
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

    //Empty source but result has an hour → KeyNotFoundException when building demand line.
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

    //Schedule has “Hidden” at 99 MW and “GB1” at 2 MW; allowed list is only GB1 → only 
    // one series, value 2; demand still from source (50).
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

    //Allowed list GB1, GB1, GB1 → Distinct() leaves one series; still [8] for that hour.
    [Fact]
    public void HeatProductionGraph_Edge_DuplicateAllowedNames_UsesSingleSeriesRow()
    {
        var hour = new DateTime(2026, 1, 1, 0, 0, 0);
        var gb1 = Unit("GB1");
        var source = new Dictionary<DateTime, DataPoint>
        {
            [hour] = new DataPoint { HeatDemand = 1, ElectricityPrice = 0 }
        };
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (hour, new List<(ProductionUnit, double, double)> { (gb1, 8, 0) })
        };

        var (unitSeries, _) = MirrorResultViewModelGraphs.BuildHeatProductionChartData(
            source,
            new List<string> { "GB1", "GB1", "GB1" },
            results);

        Assert.Single(unitSeries);
        Assert.Equal(new[] { 8.0 }, unitSeries[0].Values);
    }

    //Empty schedule but allowed GB1 and GB2 → two series of zeros; demand line still [10] from source 
    // (overlay independent of schedule).
    [Fact]
    public void HeatProductionGraph_Edge_AllowedUnitWithNoOutput_HasZeroSeries()
    {
        var hour = new DateTime(2026, 1, 1, 0, 0, 0);
        var source = new Dictionary<DateTime, DataPoint>
        {
            [hour] = new DataPoint { HeatDemand = 10, ElectricityPrice = 0 }
        };
        var results = new List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)>
        {
            (hour, new List<(ProductionUnit, double, double)>())
        };

        var (unitSeries, demandLine) = MirrorResultViewModelGraphs.BuildHeatProductionChartData(
            source,
            new List<string> { "GB1", "GB2" },
            results);

        Assert.Equal(2, unitSeries.Count);
        Assert.All(unitSeries, s => Assert.Equal(0, s.Values[0]));
        Assert.Equal(new[] { 10.0 }, demandLine);
    }

}
