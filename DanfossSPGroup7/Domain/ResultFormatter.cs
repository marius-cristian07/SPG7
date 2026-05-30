using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DanfossSPGroup7.Domain
{
    public static class ResultFormatter
    {
        public static string BuildTextReport(
            int scenarioNumber,
            bool isSummer,
            IReadOnlyList<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results,
            IReadOnlyList<ProductionUnit> productionUnits)
        {
            var sb = new StringBuilder();

            // add the title for the selected scenario and season
            sb.AppendLine($"Scenario {scenarioNumber} - {(isSummer ? "Summer" : "Winter")}");
            AppendMaintenanceSummary(sb, productionUnits);
            sb.AppendLine();

            // only show the first hours so the text report is not too long
            foreach (var hour in results.Take(96))
            {
                sb.AppendLine($"{hour.Hour:yyyy-MM-dd HH:mm}");

                foreach (var item in hour.Schedule)
                {
                    sb.AppendLine($"  {item.Unit.Name}: {item.HeatMW:F2} MW");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static void AppendMaintenanceSummary(StringBuilder sb, IReadOnlyList<ProductionUnit> productionUnits)
        {
            // find all units that have maintenance planned
            var maintenanceUnits = productionUnits
                .Where(unit => unit.MaintenancePeriods.Any())
                .ToList();

            if (maintenanceUnits.Count == 0)
            {
                // show this when no unit has maintenance
                sb.AppendLine("Maintenance: none");
                return;
            }

            sb.AppendLine("Maintenance:");
            foreach (var unit in maintenanceUnits)
            {
                // add every maintenance period for the unit
                foreach (var maintenance in unit.MaintenancePeriods)
                {
                    sb.AppendLine(
                        $"  {unit.Name}: {maintenance.Start:yyyy-MM-dd HH:mm} to {maintenance.End:yyyy-MM-dd HH:mm}");
                }
            }
        }
    }
}
