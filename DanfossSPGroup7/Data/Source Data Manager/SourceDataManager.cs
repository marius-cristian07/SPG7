using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DanfossSPGroup7.Data
{


    public class SourceDataManager : ISourceDataManager
    {
        public Dictionary<DateTime, DataPoint> Summer { get; }
        public Dictionary<DateTime, DataPoint> Winter { get; }
        // Load the summer and winter data files
        public SourceDataManager()
        {
            string basePath = AppContext.BaseDirectory;

            string summerPath = ResolveSourceDataPath(basePath, "SummerSourceDataSheet.csv");
            string winterPath = ResolveSourceDataPath(basePath, "WinterSourceDataSheet.csv");

            Summer = LoadScenario(summerPath);
            Winter = LoadScenario(winterPath);
        }

        private static string ResolveSourceDataPath(string basePath, string fileName)
        {
            // First try to find the file next to the program
            string rootPath = Path.Combine(basePath, fileName);
            if (File.Exists(rootPath))
                return rootPath;

            // try to find the file inside the data folder
            string nestedPath = Path.Combine(basePath, "Data", "Source Data Manager", fileName);
            if (File.Exists(nestedPath))
                return nestedPath;

            throw new FileNotFoundException($"CSV file not found: {fileName}. Tried: {rootPath} and {nestedPath}");
        }

        // read one CSV file and turn each row into a data point
        private static Dictionary<DateTime, DataPoint> LoadScenario(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException($"CSV file not found: {fileName}");

            Dictionary<DateTime, DataPoint> data = new();

            using StreamReader reader = new StreamReader(fileName);

            reader.ReadLine(); // skip the header row

            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // each row has time heat demand and electricity price
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
