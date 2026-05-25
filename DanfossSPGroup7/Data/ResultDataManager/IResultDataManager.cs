using System;
using System.Collections.Generic;
using DanfossSPGroup7.Domain;

namespace DanfossSPGroup7.Data
{
    public interface IResultDataManager
    {
        string LastSavedCsv { get; }

        string SaveResults(
            int scenarioNumber,
            bool isSummer,
            IReadOnlyList<(DateTime Hour, List<(ProductionUnit Unit, double HeatMW, double Co2)> Schedule)> results);
    }
}
