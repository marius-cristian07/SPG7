using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanfossSPGroup7.Domain
{
    public class ProductionUnit
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public double MaxHeatMW { get; set; }
        public double ElectricityMW { get; set; }
        public double EnergyConsumption { get; set; }
        public double ProductionCost { get; set; }
        public double CO2Emissions { get; set; }
        public bool IsInMaintenance { get; set; }
    }
}
