using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DanfossSPGroup7.Domain;

namespace DanfossSPGroup7.Data
{
    public class AssetManager : IAssetManager
    {
        private readonly List<ProductionUnit> _units = new()
        {
            new ProductionUnit
            {
                Name = "GB1",
                ImagePath = "Assets/Unit1.png",
                MaxHeatMW = 3.0,
                ElectricityMW = 0,
                EnergyConsumption = 1.05,
                ProductionCost = 510,
                CO2Emissions = 132,
                IsInMaintenance = false
            },
            new ProductionUnit
            {
                Name = "GB2",
                ImagePath = "Assets/Unit2.png",
                MaxHeatMW = 2.0,
                ElectricityMW = 0,
                EnergyConsumption = 1.08,
                ProductionCost = 540,
                CO2Emissions = 134,
                IsInMaintenance = false
            },
            new ProductionUnit
            {
                Name = "GB3",
                ImagePath = "Assets/Unit3.png",
                MaxHeatMW = 4.0,
                ElectricityMW = 0,
                EnergyConsumption = 1.09,
                ProductionCost = 580,
                CO2Emissions = 136,
                IsInMaintenance = false
            },
            new ProductionUnit
            {
                Name = "OB1",
                ImagePath = "Assets/Unit4.png",
                MaxHeatMW = 6.0,
                ElectricityMW = 0,
                EnergyConsumption = 1.18,
                ProductionCost = 690,
                CO2Emissions = 147,
                IsInMaintenance = false
            },
            new ProductionUnit
            {
                Name = "GM1",
                ImagePath = "Assets/Unit5.png",
                MaxHeatMW = 5.3,
                ElectricityMW = 3.9,
                EnergyConsumption = 1.82,
                ProductionCost = 975,
                CO2Emissions = 227,
                IsInMaintenance = false
            },
            new ProductionUnit
            {
                Name = "EB1",
                ImagePath = "Assets/Unit6.png",
                MaxHeatMW = 6.0,
                ElectricityMW = -6.0,
                EnergyConsumption = 0,
                ProductionCost = 15,
                CO2Emissions = 0,
                IsInMaintenance = false
            }

        };
        public List<ProductionUnit> GetProductionUnits()
        {
            return _units;
        }

    }
}
