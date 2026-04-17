using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DanfossSPGroup7.Domain;
using DanfossSPGroup7.Data;

namespace DanfossSPGroup7.Tests
{
    

    public class FakeSourceDataManager : ISourceDataManager
    {
        public Dictionary<DateTime, DataPoint> summer { get; set; } = new();
        public Dictionary<DateTime, DataPoint> winter { get; set; } = new();

        public Dictionary<DateTime, DataPoint> LoadScenario(string scenario)
        {
            // Return appropriate dataset based on scenario
            return scenario.ToLower() switch
            {
                "summer" => summer,
                "winter" => winter,
                _ => new Dictionary<DateTime, DataPoint>()
            };
        }
    }

    public class FakeAssetManager : IAssetManager
    {
        private readonly List<ProductionUnit> _units;

        public FakeAssetManager(List<ProductionUnit> units)
        {
            _units = units;
        }

        public List<ProductionUnit> GetProductionUnits() => _units;
    }

    

    public class ScenariosTest
    {
        private ProductionUnit CreateUnit(string name, double maxHeat, double cost, double electricity)
        {
            return new ProductionUnit
            {
                Name = name,
                MaxHeatMW = maxHeat,
                ProductionCost = cost,
                ElectricityMW = electricity
            };
        }

        

        [Fact]
        public void RunScenario1_ShouldMeetDemand_WithCheapestUnitsFirst()
        {
            var date = DateTime.Now;

            var source = new FakeSourceDataManager
            {
                summer = new Dictionary<DateTime, DataPoint>
                {
                    { date, new DataPoint { HeatDemand = 100, ElectricityPrice = 50 } }
                }
            };

            var units = new List<ProductionUnit>
            {
                CreateUnit("Cheap", 60, 10, 0),
                CreateUnit("Expensive", 60, 20, 0)
            };

            var optimizer = new Optimizer(source, new FakeAssetManager(units));

            var result = optimizer.RunScenario1(true);

            Assert.Single(result);
            Assert.Equal(100, result[0].Schedule.Sum(x => x.HeatMW));
            Assert.Equal("Cheap", result[0].Schedule[0].Unit.Name);
        }

        [Fact]
        public void RunScenario2_ShouldOrderByNetCost()
        {
            var date = DateTime.Now;

            var source = new FakeSourceDataManager
            {
                summer = new Dictionary<DateTime, DataPoint>
                {
                    { date, new DataPoint { HeatDemand = 50, ElectricityPrice = 100 } }
                }
            };

            var units = new List<ProductionUnit>
            {
                CreateUnit("LowElectricityBenefit", 50, 30, 0),
                CreateUnit("HighElectricityBenefit", 50, 40, 50)
            };

            var optimizer = new Optimizer(source, new FakeAssetManager(units));

            var result = optimizer.RunScenario2(true);

            Assert.Single(result);
            Assert.Equal("HighElectricityBenefit", result[0].Schedule[0].Unit.Name);
        }

        

        [Fact]
        public void RunScenario1_ZeroDemand_ShouldReturnEmptySchedule()
        {
            var date = DateTime.Now;

            var source = new FakeSourceDataManager
            {
                summer = new Dictionary<DateTime, DataPoint>
                {
                    { date, new DataPoint { HeatDemand = 0, ElectricityPrice = 50 } }
                }
            };

            var units = new List<ProductionUnit>
            {
                CreateUnit("Unit", 100, 10, 0)
            };

            var optimizer = new Optimizer(source, new FakeAssetManager(units));

            var result = optimizer.RunScenario1(true);

            Assert.Single(result);
            Assert.Empty(result[0].Schedule);
        }

        [Fact]
        public void RunScenario1_DemandExceedsCapacity_ShouldUseAllUnits()
        {
            var date = DateTime.Now;

            var source = new FakeSourceDataManager
            {
                summer = new Dictionary<DateTime, DataPoint>
                {
                    { date, new DataPoint { HeatDemand = 200, ElectricityPrice = 50 } }
                }
            };

            var units = new List<ProductionUnit>
            {
                CreateUnit("Unit1", 50, 10, 0),
                CreateUnit("Unit2", 50, 20, 0)
            };

            var optimizer = new Optimizer(source, new FakeAssetManager(units));

            var result = optimizer.RunScenario1(true);

            Assert.Single(result);
            Assert.Equal(100, result[0].Schedule.Sum(x => x.HeatMW));
        }

        [Fact]
        public void RunScenario2_NoAvailableUnits_ShouldReturnEmptySchedule()
        {
            var date = DateTime.Now;

            var source = new FakeSourceDataManager
            {
                summer = new Dictionary<DateTime, DataPoint>
                {
                    { date, new DataPoint { HeatDemand = 100, ElectricityPrice = 50 } }
                }
            };

            var unit = CreateUnit("Unit", 100, 10, 0);

            
            unit.ScheduleMaintenance(new MaintenancePeriod(date.AddHours(-1), date.AddHours(1)));

            var optimizer = new Optimizer(source, new FakeAssetManager(new List<ProductionUnit> { unit }));

            var result = optimizer.RunScenario2(true);

            Assert.Single(result);
            Assert.Empty(result[0].Schedule);
        }

        

        [Fact]
        public void RunScenario1_EmptyData_ShouldReturnEmptyResult()
        {
            var source = new FakeSourceDataManager
            {
                summer = new Dictionary<DateTime, DataPoint>(),
                winter = new Dictionary<DateTime, DataPoint>()
            };

            var optimizer = new Optimizer(source,
                new FakeAssetManager(new List<ProductionUnit>()));

            var result = optimizer.RunScenario1(true);

            Assert.Empty(result);
        }

        [Fact]
        public void RunScenario2_UnitWithZeroCapacity_ShouldThrowException()
        {
            var date = DateTime.Now;

            var source = new FakeSourceDataManager
            {
                summer = new Dictionary<DateTime, DataPoint>
                {
                    { date, new DataPoint { HeatDemand = 50, ElectricityPrice = 50 } }
                }
            };

            var units = new List<ProductionUnit>
            {
                CreateUnit("ZeroCap", 0, 10, 0)
            };

            var optimizer = new Optimizer(source, new FakeAssetManager(units));

            Assert.Throws<ArgumentException>(() => optimizer.RunScenario2(true));
        }
    }
}