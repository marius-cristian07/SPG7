using DanfossSPGroup7.Data;
using DanfossSPGroup7.Domain;
using Xunit;
using System.Linq;


namespace SPG7UnitTesting
{
    public class AssetManagerTests
    {
        [Fact]
        public void GetProductionUnits_ShouldReturnNonNullList()
        {
            var manager = new AssetManager();

            var units = manager.GetProductionUnits();

            Assert.NotNull(units);
        }
        [Fact]
        public void GetProductionUnits_ShouldContainExpectedUnitNames()
        {
            var manager = new AssetManager();

            var units = manager.GetProductionUnits();

            Assert.Contains(units, u => u.Name == "GB1");
            Assert.Contains(units, u => u.Name == "GM1");
            Assert.Contains(units, u => u.Name == "EB1");
        }
        [Fact]
        public void GetProductionUnits_ShouldLoadCorrectDataForGB1()
        {
            var manager = new AssetManager();

            var units = manager.GetProductionUnits();
            var gb1 = units.FirstOrDefault(u => u.Name == "GB1");

            Assert.NotNull(gb1);
            Assert.Equal("Assets/Unit1.png", gb1!.ImagePath);
            Assert.Equal(3.0, gb1.MaxHeatMW);
            Assert.Equal(0, gb1.ElectricityMW);
            Assert.Equal(1.05, gb1.EnergyConsumption);
            Assert.Equal(510, gb1.ProductionCost);
            Assert.Equal(132, gb1.CO2Emissions);
        }
        [Fact]
        public void GetProductionUnits_AllUnits_ShouldHaveNoMaintenanceByDefault()
        {
            var manager = new AssetManager();

            var units = manager.GetProductionUnits();

            Assert.All(units, u => Assert.Empty(u.MaintenancePeriods));
        }
    }
}