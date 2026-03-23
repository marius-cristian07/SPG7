using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DanfossSPGroup7.Data
{


    public class SourceDataManager : ISourceDataManager
    {

        public Dictionary<DateTime, DataPoint> summer { get; }
        public Dictionary<DateTime, DataPoint> winter { get; }

        public SourceDataManager()
        {
            summer = LoadScenario("SummerSourceDataSheet.csv");
            winter = LoadScenario("WinterSourceDataSheet.csv");
        }
        public Dictionary<DateTime, DataPoint> LoadScenario(string fileName)
        {
            Dictionary<DateTime, DataPoint> data = new Dictionary<DateTime, DataPoint>();

            StreamReader reader = new StreamReader(fileName);
            reader.ReadLine().Skip(1); 

            while (!reader.EndOfStream)
            {
                string? line = reader.ReadLine();
                string[] parts = line.Split(',');
    
                DateTime time = DateTime.Parse(parts[0]);
                Double heat = double.Parse(parts[1]);
                Double price = double.Parse(parts[2]);

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