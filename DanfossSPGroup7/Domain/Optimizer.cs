using System;
using System.Collections.Generic;
using System.Linq;
using DanfossSPGroup7.Data;

namespace DanfossSPGroup7.Domain
{
    public class Optimizer
    {
        public Dictionary<DateTime, DataPoint> Summer { get; private set; }
        public Dictionary<DateTime, DataPoint> Winter { get; private set; }
        private readonly List<ProductionUnit> _productionUnits;
        public List<ProductionUnit> ProductionUnits => _productionUnits;

        public Optimizer(ISourceDataManager sourceDataManager, IAssetManager assetManager)
        {
            Summer = sourceDataManager.Summer;
            Winter = sourceDataManager.Winter;
            _productionUnits = assetManager.GetProductionUnits();
        }

        public List<OptimizationResult> GetUnitsByNetProductionCost(DateTime dateTime, bool isSummer)
        {
            // choose the data for the selected season
            var dict = isSummer ? Summer : Winter;
            if (!dict.TryGetValue(dateTime, out var dataPoint))
            {
                throw new ArgumentException("No source data available for the provided time.", nameof(dateTime));
            }

            // find the available units and sort them by cheapest net cost
            return _productionUnits
                .Where(u => u.IsAvailable(dateTime))
                .Select(unit => new OptimizationResult
                {
                    Unit = unit,
                    NetProductionCost = CalculateNetProductionCost(unit, dataPoint.ElectricityPrice)
                })
                .OrderBy(result => result.NetProductionCost)
                .ToList();
        }

        public static double CalculateNetProductionCost(ProductionUnit unit, double electricityPrice)
        {
            // a unit cannot make heat if the max heat is zero or lower
            if (unit.MaxHeatMW <= 0)
            {
                throw new ArgumentException($"Unit {unit.Name} has invalid MaxHeatMW ({unit.MaxHeatMW}).");
            }

            // convert electricity use to cost per heat unit
            var electricityPerMWhHeat = unit.ElectricityMW / unit.MaxHeatMW;
            return unit.ProductionCost - (electricityPerMWhHeat * electricityPrice);
        }

        // run scenario 1 using normal production cost
        public List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> RunScenario1(bool isSummer, IEnumerable<string>? allowedUnitNames = null)
        {
            // pick summer or winter source data
            var dict = isSummer ? Summer : Winter;
            var results = new List<(DateTime, List<(ProductionUnit, double, double)>)>();

            var allowedSet = allowedUnitNames != null ? new HashSet<string>(allowedUnitNames): null;

            foreach (var kvp in dict)
            {
                double demand = kvp.Value.HeatDemand;
                // Sort units so the cheapest units are used first
                var orderedUnits = _productionUnits
                    .Where(u => u.IsAvailable(kvp.Key))
                    .Where(u => allowedSet == null || allowedSet.Contains(u.Name))
                    .OrderBy(u => u.ProductionCost) 
                    .ToList();

                var schedule = new List<(ProductionUnit, double, double)>();

                foreach (var unit in orderedUnits)
                {
                    if (demand <= 0) break;

                    // use as much heat as needed but never more than the unit can produce
                    var heat = Math.Min(unit.MaxHeatMW, demand);
                    schedule.Add((unit, heat, unit.CO2Emissions));
                    demand -= heat;
                }

                results.Add((kvp.Key, schedule));
            }
            return results;
        }

        // run scenario 2 using net production cost
        public List<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> RunScenario2(bool isSummer, IEnumerable<string>? allowedUnitNames = null)
        {
            // Pick summer or winter source data
            var dict = isSummer ? Summer : Winter;
            var results = new List<(DateTime, List<(ProductionUnit, double, double)>)>();

            var allowedSet = allowedUnitNames != null ? new HashSet<string>(allowedUnitNames): null;

            foreach (var kvp in dict)
            {
                double demand = kvp.Value.HeatDemand;
                double price = kvp.Value.ElectricityPrice;

                // sort units by price after electricity has been included
                var orderedUnits = _productionUnits
                    .Where(u => u.IsAvailable(kvp.Key))
                    .Where(u => allowedSet == null || allowedSet.Contains(u.Name))
                    .Select(u => new { Unit = u, Cost = CalculateNetProductionCost(u, price) })
                    .OrderBy(x => x.Cost)
                    .ToList();

                var schedule = new List<(ProductionUnit, double, double)>();

                foreach (var item in orderedUnits)
                {
                    if (demand <= 0) break;

                    // use each unit until demand is covered
                    var heat = Math.Min(item.Unit.MaxHeatMW, demand);
                    schedule.Add((item.Unit, heat, item.Unit.CO2Emissions));
                    demand -= heat;
                }

                results.Add((kvp.Key, schedule));
            }
            return results;
        }
    }
}
