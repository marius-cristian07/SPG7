using System;
using System.Collections.Generic;
using DanfossSPGroup7.Data;
using DanfossSPGroup7.Domain;
using DanfossSPGroup7.UI.ViewModels;
using Xunit;

namespace DanfossSPGroup7.Tests
{
    public class ResultViewModelUiFlowTests
    {
        [Fact]
        public void UiFlow_Positive_ChangeScenarioAndSeason_UpdatesStateAndCharts()
        {
            var vm = new ResultViewModel(CreateOptimizerWithData());

            vm.ChangeScenario("2");
            vm.ChangeSeason("True");

            Assert.Equal(2, vm.CurrentScenario);
            Assert.True(vm.CurrentIsSummer);
            Assert.True(vm.IsScenario2Selected);
            Assert.NotEmpty(vm.Series);
            Assert.NotEmpty(vm.ElectricitySeries);
            Assert.Contains("Scenario 2 - Summer", vm.ResultsText);
        }

        [Fact]
        public void UiFlow_Negative_ChangeScenario_InvalidInput_ThrowsFormatException()
        {
            var vm = new ResultViewModel(CreateOptimizerWithData());

            Assert.Throws<FormatException>(() => vm.ChangeScenario("abc"));
            Assert.Equal(1, vm.CurrentScenario);
        }

        [Fact]
        public void UiFlow_Edge_EmptySourceData_StillBuildsEmptyFriendlySeries()
        {
            var vm = new ResultViewModel(CreateOptimizerWithEmptyData());

            Assert.Equal(5, vm.Series.Length);
            Assert.Single(vm.HeatDemandSeries);
            Assert.Single(vm.Co2Series);
            Assert.Single(vm.NetProductionCostSeries);
            Assert.Empty(vm.ElectricitySeries);
            Assert.Contains("Scenario 1 - Winter", vm.ResultsText);
        }

        private static Optimizer CreateOptimizerWithData()
        {
            var start = new DateTime(2026, 1, 1, 0, 0, 0);
            var summer = new Dictionary<DateTime, DataPoint>();
            var winter = new Dictionary<DateTime, DataPoint>();

            for (var i = 0; i < 24; i++)
            {
                var hour = start.AddHours(i);
                summer[hour] = new DataPoint { HeatDemand = 8, ElectricityPrice = 250 + i };
                winter[hour] = new DataPoint { HeatDemand = 8, ElectricityPrice = 200 + i };
            }

            return new Optimizer(
                new FakeSourceDataManager(summer, winter),
                new FakeAssetManager(CreateProductionUnits()));
        }

        private static Optimizer CreateOptimizerWithEmptyData()
        {
            return new Optimizer(
                new FakeSourceDataManager(new Dictionary<DateTime, DataPoint>(), new Dictionary<DateTime, DataPoint>()),
                new FakeAssetManager(CreateProductionUnits()));
        }

        private static List<ProductionUnit> CreateProductionUnits()
        {
            return new List<ProductionUnit>
            {
                new ProductionUnit
                {
                    Name = "GB1",
                    MaxHeatMW = 3.0,
                    ElectricityMW = 0,
                    ProductionCost = 510,
                    CO2Emissions = 132
                },
                new ProductionUnit
                {
                    Name = "GB2",
                    MaxHeatMW = 2.0,
                    ElectricityMW = 0,
                    ProductionCost = 540,
                    CO2Emissions = 134
                },
                new ProductionUnit
                {
                    Name = "GB3",
                    MaxHeatMW = 4.0,
                    ElectricityMW = 0,
                    ProductionCost = 580,
                    CO2Emissions = 136
                },
                new ProductionUnit
                {
                    Name = "OB1",
                    MaxHeatMW = 6.0,
                    ElectricityMW = 0,
                    ProductionCost = 690,
                    CO2Emissions = 147
                },
                new ProductionUnit
                {
                    Name = "GM1",
                    MaxHeatMW = 5.3,
                    ElectricityMW = 3.9,
                    ProductionCost = 975,
                    CO2Emissions = 227
                },
                new ProductionUnit
                {
                    Name = "EB1",
                    MaxHeatMW = 6.0,
                    ElectricityMW = -6.0,
                    ProductionCost = 15,
                    CO2Emissions = 0
                }
            };
        }

        private sealed class FakeSourceDataManager : ISourceDataManager
        {
            public FakeSourceDataManager(
                Dictionary<DateTime, DataPoint> summer,
                Dictionary<DateTime, DataPoint> winter)
            {
                Summer = summer;
                Winter = winter;
            }

            public Dictionary<DateTime, DataPoint> Summer { get; }
            public Dictionary<DateTime, DataPoint> Winter { get; }
        }

        private sealed class FakeAssetManager : IAssetManager
        {
            private readonly List<ProductionUnit> _units;

            public FakeAssetManager(List<ProductionUnit> units)
            {
                _units = units;
            }

            public List<ProductionUnit> GetProductionUnits() => _units;
        }
    }
}
