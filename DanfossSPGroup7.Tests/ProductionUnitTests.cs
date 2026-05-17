using DanfossSPGroup7.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DanfossSPGroup7.Tests
{
    public class ProductionUnitTests
    {
        //Positive test case
        [Fact]
        public void IsAvailable_ReturnsTrueWhenNoMaintenanceExists()
        {
            var unit = new ProductionUnit
            {
                Name = "GB1"
            };

            var time = new DateTime(2026, 1, 1, 10, 0, 0);

            var result = unit.IsAvailable(time);

            Assert.True(result);
        }
        //Negative test case
        [Fact]
        public void IsAvailable_ReturnsFalseWhenTimeIsInsideMaintenancePeriod()
        {
            var unit = new ProductionUnit
            {
                Name = "GB1"
            };

            var maintenance = new MaintenancePeriod(
                new DateTime(2026, 1, 1, 8, 0, 0),
                new DateTime(2026, 1, 1, 12, 0, 0)
            );

            unit.ScheduleMaintenance(maintenance);

            var result = unit.IsAvailable(new DateTime(2026, 1, 1, 10, 0, 0));

            Assert.False(result);
        }
        //Edge test case
        [Fact]
        public void IsAvailable_ReturnsTrueWhenTimeIsBeforeMaintenancePeriod()
        {
            var unit = new ProductionUnit
            {
                Name = "GB1"
            };

            var maintenance = new MaintenancePeriod(
                new DateTime(2026, 1, 1, 8, 0, 0),
                new DateTime(2026, 1, 1, 12, 0, 0)
            );

            unit.ScheduleMaintenance(maintenance);

            var result = unit.IsAvailable(new DateTime(2026, 1, 1, 7, 59, 0));

            Assert.True(result);
        }
    }
}
