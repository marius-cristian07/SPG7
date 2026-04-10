using System;
using System.Collections.Generic;
using System.Linq;
using DanfossSPGroup7.Data;

namespace DanfossSPGroup7.Domain
{
    internal class Optimizer
    {
        public static Optimizer? Instance { get; private set; }
        public Dictionary<DateTime, DataPoint> Summer { get; private set; }
        public Dictionary<DateTime, DataPoint> Winter { get; private set; }
        private readonly List<ProductionUnit> _productionUnits;

        public Optimizer(ISourceDataManager sourceDataManager, IAssetManager assetManager)
        {
            Instance = this;
            Summer = sourceDataManager.summer;
            Winter = sourceDataManager.winter;
            _productionUnits = assetManager.GetProductionUnits();
        }

        public List<ResultDM> GetUnitsByNetProductionCost(DateTime dateTime, bool isSummer)
        {
            var dict = isSummer ? Summer : Winter;
            if (!dict.TryGetValue(dateTime, out var dataPoint))
            {
                throw new ArgumentException("No source data available for the provided time.", nameof(dateTime));
            }

            return _productionUnits
                .Where(u => u.IsAvailable(dateTime))
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

        // Scenario 1
        public List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW)> Schedule)> RunScenario1(bool isSummer)
        {
            var dict = isSummer ? Summer : Winter;
            var results = new List<(DateTime, List<(ProductionUnit, double)>)>();

            foreach (var kvp in dict)
            {
                double demand = kvp.Value.HeatDemand;
                var orderedUnits = _productionUnits
                    .Where(u => u.IsAvailable(kvp.Key))
                    .OrderBy(u => u.ProductionCost) 
                    .ToList();

                var schedule = new List<(ProductionUnit, double)>();

                foreach (var unit in orderedUnits)
                {
                    if (demand <= 0) break;

                    var heat = Math.Min(unit.MaxHeatMW, demand);
                    schedule.Add((unit, heat));
                    demand -= heat;
                }

                results.Add((kvp.Key, schedule));
            }
            return results;
        }

        // Scenario 2
        public List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW)> Schedule)> RunScenario2(bool isSummer)
        {
            var dict = isSummer ? Summer : Winter;
            var results = new List<(DateTime, List<(ProductionUnit, double)>)>();

            foreach (var kvp in dict)
            {
                double demand = kvp.Value.HeatDemand;
                double price = kvp.Value.ElectricityPrice;

                var orderedUnits = _productionUnits
                    .Where(u => u.IsAvailable(kvp.Key))
                    .Select(u => new { Unit = u, Cost = CalculateNetProductionCost(u, price) })
                    .OrderBy(x => x.Cost)
                    .ToList();

                var schedule = new List<(ProductionUnit, double)>();

                foreach (var item in orderedUnits)
                {
                    if (demand <= 0) break;

                    var heat = Math.Min(item.Unit.MaxHeatMW, demand);
                    schedule.Add((item.Unit, heat));
                    demand -= heat;
                }

                results.Add((kvp.Key, schedule));
            }
            return results;
        }
    }
}

