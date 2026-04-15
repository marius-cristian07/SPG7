using DanfossSPGroup7.Data;
using DanfossSPGroup7.Domain;
using Xunit;

namespace SPG7UnitTesting
{
    public class AssetManagerTests
    {
        [Fact]
        public void Test()
        {
            var manager = new AssetManager();

            var units = manager.GetProductionUnits();

            Assert.NotNull(units);
        }
    }
}