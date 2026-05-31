using System;
using System.Collections.Generic;
using System.Linq;
namespace DanfossSPGroup7.Domain
{
    public class MaintenanceCalculation
    {
        // create a maintenance period for one boiler
        public void CreateMaintenanceForBoiler(string unitName, int duration, List<ProductionUnit> allUnits, DateTime startDate)
        {   
            // find the boiler that the user selected
            ProductionUnit? chosenUnit = allUnits.FirstOrDefault(u => u.Name == unitName);

            if (chosenUnit == null)
                throw new ArgumentException($"Boiler '{unitName}' is not found");

            // the maintenance starts on the chosen date and lasts for the chosen hours
            var maintenance = new MaintenancePeriod(
                startDate, 
                startDate.AddHours(duration)
            );
        
            chosenUnit.ScheduleMaintenance(maintenance);

        }
    }
}
