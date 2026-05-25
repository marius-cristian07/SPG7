using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DanfossSPGroup7.Domain;

namespace DanfossSPGroup7.Data
{
    public class ResultDataManager : IResultDataManager
    {
        public string LastSavedCsv { get; private set; } = string.Empty;

        public string SaveResults(
            int scenarioNumber,
            bool isSummer,
            IReadOnlyList<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Scenario,Season,Hour,Unit,HeatMW,Co2KgPerMWh");

            var season = isSummer ? "Summer" : "Winter";

            foreach (var hour in results)
            {
                foreach (var entry in hour.Schedule)
                {
                    csv.AppendLine(
                        string.Join(
                            ",",
                            scenarioNumber,
                            season,
                            hour.Hour.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                            entry.Unit.Name,
                            entry.HeatMW.ToString("F2", CultureInfo.InvariantCulture),
                            entry.Co2.ToString("F2", CultureInfo.InvariantCulture)));
                }
            }

            LastSavedCsv = csv.ToString();
            return LastSavedCsv;
        }
    }
}
