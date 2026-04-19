using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DanfossSPGroup7.Data
{


    public class SourceDataManager : ISourceDataManager
    {

        public Dictionary<DateTime, DataPoint> summer { get; }
        public Dictionary<DateTime, DataPoint> winter { get; }
        // Loading Data from the file
        public SourceDataManager()
        {
            string basePath = AppContext.BaseDirectory;

            string summerPath = ResolveSourceDataPath(basePath, "SummerSourceDataSheet.csv");
            string winterPath = ResolveSourceDataPath(basePath, "WinterSourceDataSheet.csv");

            summer = LoadScenario(summerPath);
            winter = LoadScenario(winterPath);
        }

        private static string ResolveSourceDataPath(string basePath, string fileName)
        {
            string rootPath = Path.Combine(basePath, fileName);
            if (File.Exists(rootPath))
                return rootPath;

            string nestedPath = Path.Combine(basePath, "Data", "Source Data Manager", fileName);
            if (File.Exists(nestedPath))
                return nestedPath;

            throw new FileNotFoundException($"CSV file not found: {fileName}. Tried: {rootPath} and {nestedPath}");
        }

        // Logic behind loading the Data
        public Dictionary<DateTime, DataPoint> LoadScenario(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException($"CSV file not found: {fileName}");

            Dictionary<DateTime, DataPoint> data = new();

            using StreamReader reader = new StreamReader(fileName);

            reader.ReadLine(); // skip header

            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split(',');

                DateTime time = DateTime.Parse(parts[0], CultureInfo.InvariantCulture);
                double heat = double.Parse(parts[1], CultureInfo.InvariantCulture);
                double price = double.Parse(parts[2], CultureInfo.InvariantCulture);

                data[time] = new DataPoint
                {
                    HeatDemand = heat,
                    ElectricityPrice = price
                };
            }

            return data;
        }
    }

}