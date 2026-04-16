using System;
using System.Collections.Generic;
using System.Linq;
using DanfossSPGroup7.Data;
using DanfossSPGroup7.Domain;
using Xunit;

namespace DanfossSPGroup7.Tests;

public class OptimizerTests
{
    [Fact]
    public void GetUnitsByNetProductionCost_Positive_ReturnsSortedAvailableUnits()
    {
        var hour = new DateTime(2026, 1, 1, 10, 0, 0);
        var source = new FakeSourceDataManager(
            new Dictionary<DateTime, DataPoint>
            {
                [hour] = new DataPoint { HeatDemand = 20, ElectricityPrice = 100 }
            },
            new Dictionary<DateTime, DataPoint>());

        var expensiveUnit = new ProductionUnit
        {
            Name = "Expensive",
            MaxHeatMW = 10,
            ElectricityMW = 1,
            ProductionCost = 80
        };

        var cheapUnit = new ProductionUnit
        {
            Name = "Cheap",
            MaxHeatMW = 10,
            ElectricityMW = 2,
            ProductionCost = 60
        };

        var asset = new FakeAssetManager(new List<ProductionUnit> { expensiveUnit, cheapUnit });
        var optimizer = new Optimizer(source, asset); // Creates Optimizer with fake managers

        var result = optimizer.GetUnitsByNetProductionCost(hour, isSummer: true);
        // Proves normal behavior and sorting logic work
        Assert.Equal(2, result.Count);
        Assert.Equal("Cheap", result[0].Unit.Name);
        Assert.Equal("Expensive", result[1].Unit.Name);
    }

    [Fact]
    public void GetUnitsByNetProductionCost_Negative_ThrowsWhenDateIsMissing()
    {
        var source = new FakeSourceDataManager( //Creates source data with no entries
            new Dictionary<DateTime, DataPoint>(),
            new Dictionary<DateTime, DataPoint>());
        var asset = new FakeAssetManager(new List<ProductionUnit>());
        var optimizer = new Optimizer(source, asset); // Calls method with a date not in dictionary

        Assert.Throws<ArgumentException>(() =>
            optimizer.GetUnitsByNetProductionCost(new DateTime(2026, 1, 1, 8, 0, 0), isSummer: true));
    }

    [Fact]
    public void GetUnitsByNetProductionCost_Edge_ReturnsEmptyWhenNoUnitsAvailable()
    {
        // Edge behavior when data exists but all units are unavailable
        var hour = new DateTime(2026, 2, 1, 12, 0, 0);
        var source = new FakeSourceDataManager(
            new Dictionary<DateTime, DataPoint>
            {
                [hour] = new DataPoint { HeatDemand = 10, ElectricityPrice = 50 }
            },
            new Dictionary<DateTime, DataPoint>());

        var unit = new ProductionUnit
        {
            Name = "InMaintenance",
            MaxHeatMW = 10,
            ElectricityMW = 1,
            ProductionCost = 40
        };

        unit.ScheduleMaintenance(new MaintenancePeriod(hour.AddHours(-1), hour.AddHours(1)));

        var asset = new FakeAssetManager(new List<ProductionUnit> { unit });
        var optimizer = new Optimizer(source, asset);

        var result = optimizer.GetUnitsByNetProductionCost(hour, isSummer: true);
        
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateNetProductionCost_Positive_ReturnsExpectedValue()
    {
        var unit = new ProductionUnit
        {
            Name = "UnitA",
            MaxHeatMW = 10,
            ElectricityMW = 2,
            ProductionCost = 70
        };

        var result = Optimizer.CalculateNetProductionCost(unit, 100);
        //Validates expected output when all the variables are right
        Assert.Equal(50, result);
    }

    [Fact]
    public void CalculateNetProductionCost_Negative_ThrowsWhenMaxHeatIsInvalid()
    {
        var unit = new ProductionUnit
        {
            Name = "BrokenUnit",
            MaxHeatMW = 0,
            ElectricityMW = 1,
            ProductionCost = 70
        };
        // Confirms guard clause against divide-by-zero/invalid config
        Assert.Throws<ArgumentException>(() => Optimizer.CalculateNetProductionCost(unit, 100));
    }

    [Fact]
    public void CalculateNetProductionCost_Edge_HandlesZeroElectricityPrice()
    {
        var unit = new ProductionUnit
        {
            Name = "NoPriceUnit",
            MaxHeatMW = 10,
            ElectricityMW = 2,
            ProductionCost = 35
        };

        var result = Optimizer.CalculateNetProductionCost(unit, 0);
        // Edge case where market price has no impact
        Assert.Equal(35, result);
    }

    private class FakeSourceDataManager : ISourceDataManager // Returns dictionaries you pass in
    {
        public FakeSourceDataManager(
            Dictionary<DateTime, DataPoint> summerData,
            Dictionary<DateTime, DataPoint> winterData)
        {
            summer = summerData;
            winter = winterData;
        }

        public Dictionary<DateTime, DataPoint> summer { get; }
        public Dictionary<DateTime, DataPoint> winter { get; }

        public Dictionary<DateTime, DataPoint> LoadScenario(string fileName) => new();
    }

    private class FakeAssetManager : IAssetManager //Returns a fixed list of units
    {
        private readonly List<ProductionUnit> _units;

        public FakeAssetManager(List<ProductionUnit> units)
        {
            _units = units;
        }

        public List<ProductionUnit> GetProductionUnits() => _units;
    }
}
