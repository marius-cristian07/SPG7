using System;
using System.Collections.Generic;

namespace DanfossSPGroup7.Domain
{
    public class MaintenanceCalculation
    {
        public void CreateMaintenanceForBoiler(string boilerName, int duration, List<ProductionUnit> units)
        {
            DateTime fixedMaintenanceStart = new DateTime(2026, 1, 8, 0, 0, 0);
            ProductionUnit chosenUnit = null;

            foreach (var unit in units)
            {
                if (unit.Name == boilerName)
                {
                    chosenUnit = unit;
                    break;
                }
            }

            if (chosenUnit == null)
                throw new ArgumentException($"Boiler '{boilerName}' is not found");

            var maintenance = new MaintenancePeriod(
                fixedMaintenanceStart, fixedMaintenanceStart.AddHours(duration)
            );
            chosenUnit.ScheduleMaintenance(maintenance);

        }
    }
}