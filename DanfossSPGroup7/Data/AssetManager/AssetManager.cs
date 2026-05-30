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
        private readonly List<ProductionUnit> _units;

        public AssetManager()
        {
            // points to the images inside the app resources
            const string imagePath = "avares://DanfossSPGroup7/Data/AssetManager/Assets";
            
            // create all production units used in the project
            _units = new()
            {
                new ProductionUnit
                {
                    Name = "GB1",
                    ImagePath = $"{imagePath}/unit1.png",
                    MaxHeatMW = 3.0,
                    ElectricityMW = 0,
                    EnergyConsumption = 1.05,
                    ProductionCost = 510,
                    CO2Emissions = 132
                },
                new ProductionUnit
                {
                    Name = "GB2",
                    ImagePath = $"{imagePath}/unit2.png",
                    MaxHeatMW = 2.0,
                    ElectricityMW = 0,
                    EnergyConsumption = 1.08,
                    ProductionCost = 540,
                    CO2Emissions = 134
                },
                new ProductionUnit
                {
                    Name = "GB3",
                    ImagePath = $"{imagePath}/unit3.png",
                    MaxHeatMW = 4.0,
                    ElectricityMW = 0,
                    EnergyConsumption = 1.09,
                    ProductionCost = 580,
                    CO2Emissions = 136
                },
                new ProductionUnit
                {
                    Name = "OB1",
                    ImagePath = $"{imagePath}/unit4.png",
                    MaxHeatMW = 6.0,
                    ElectricityMW = 0,
                    EnergyConsumption = 1.18,
                    ProductionCost = 690,
                    CO2Emissions = 147
                },
                new ProductionUnit
                {
                    Name = "GM1",
                    ImagePath = $"{imagePath}/unit5.png",
                    MaxHeatMW = 5.3,
                    ElectricityMW = 3.9,
                    EnergyConsumption = 1.82,
                    ProductionCost = 975,
                    CO2Emissions = 227
                },
                new ProductionUnit
                {
                    Name = "EB1",
                    ImagePath = $"{imagePath}/unit6.png",
                    MaxHeatMW = 6.0,
                    ElectricityMW = -6.0,
                    EnergyConsumption = 0,
                    ProductionCost = 15,
                    CO2Emissions = 0
                }
            };
        }

        public List<ProductionUnit> GetProductionUnits()
        {
            // return the same units so maintenance changes are kept
            return _units;
        }

    }
}
