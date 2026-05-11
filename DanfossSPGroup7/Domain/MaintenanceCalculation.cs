using System;
using System.Collections.Generic;
using System.Linq;
namespace DanfossSPGroup7.Domain
{
    public class MaintenanceCalculation
    {
        // Setting Maintenance Period for a Boiler. 
        public void CreateMaintenanceForBoiler(string unitName, int duration, List<ProductionUnit> allUnits, DateTime startDate)
        {   
            // Use the parameters passed into the method
            ProductionUnit? chosenUnit = allUnits.FirstOrDefault(u => u.Name == unitName);

            if (chosenUnit == null)
                throw new ArgumentException($"Boiler '{unitName}' is not found");

            // Use the dynamic startDate from the UI
            var maintenance = new MaintenancePeriod(
                startDate, 
                startDate.AddHours(duration)
            );
        
            chosenUnit.ScheduleMaintenance(maintenance);

        }
    }
}