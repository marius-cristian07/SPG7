using System;
using System.Collections.Generic;
using System.Linq;

namespace DanfossSPGroup7.Domain
{
    public class ProductionUnit
    {
        public string Name { get; init; } = "";
        public string ImagePath { get; init; } = "";
        public double MaxHeatMW { get; init; }
        public double ElectricityMW { get; init; }
        public double EnergyConsumption { get; init; }
        public double ProductionCost { get; init; }
        public double CO2Emissions { get; init; }
        
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

        public void ClearMaintenance()
        {
            _maintenancePeriods.Clear();
        }

        public void ScheduleMaintenance(MaintenancePeriod period)
        {

            _maintenancePeriods.Add(period);
        }
    }

}
