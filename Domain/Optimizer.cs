using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DanfossSPGroup7.Data;

namespace DanfossSPGroup7.Domain
{
    internal class Optimizer
    {
        public static Optimizer? Instance { get; private set; }
        public Dictionary<DateTime, DataPoint> Summer { get; private set; }
        private readonly List<ProductionUnit> _productionUnits;

        public Optimizer(ISourceDataManager sourceDataManager, IAssetManager assetManager)
        {
            Instance = this;
            Summer = sourceDataManager.summer;
            _productionUnits = assetManager.GetProductionUnits();
        }

        public List<ResultDM> GetUnitsByNetProductionCost(DateTime dateTime)
        {
            if (!Summer.TryGetValue(dateTime, out var dataPoint))
            {
                throw new ArgumentException("No source data available for the provided time.", nameof(dateTime));
            }

            return _productionUnits
                .Where(unit => !unit.IsInMaintenance)
                .Select(unit => new ResultDM
                {
                    Unit = unit,
                    NetProductionCost = CalculateNetProductionCost(unit, dataPoint.ElectricityPrice)
                })
                .OrderBy(result => result.NetProductionCost)
                .ToList();
        }

        private static double CalculateNetProductionCost(ProductionUnit unit, double electricityPrice)
        {
            if (unit.MaxHeatMW <= 0)
            {
                throw new ArgumentException($"Unit {unit.Name} has invalid MaxHeatMW ({unit.MaxHeatMW}).");
            }

            // Convert electrical power at full load to MWh_el per MWh_heat.
            var electricityPerMWhHeat = unit.ElectricityMW / unit.MaxHeatMW;
            return unit.ProductionCost - (electricityPerMWhHeat * electricityPrice);
        }
    }
}
