using System;
using System.Collections.Generic;
using System.Linq;

namespace DanfossSPGroup7.Domain
{
    public class ProductionUnit
    {
        public string Name { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public double MaxHeatMW { get; set; }
        public double ElectricityMW { get; set; }
        public double EnergyConsumption { get; set; }
        public double ProductionCost { get; set; }
        public double CO2Emissions { get; set; }
        
        private readonly List<MaintenancePeriod> _maintenancePeriods = new();
        public IReadOnlyList<MaintenancePeriod> MaintenancePeriods => _maintenancePeriods.AsReadOnly();

        public bool IsAvailable(DateTime time)
        {
            foreach (var maintenance in _maintenancePeriods)
            {
                if (maintenance.Contains(time))
                {
                    return false;
                }
            }
            return true;
        }

        public void ScheduleMaintenance(MaintenancePeriod period)
        {

            _maintenancePeriods.Add(period);
        }
    }

}
