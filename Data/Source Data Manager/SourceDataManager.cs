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
public Dictionary<DateTime, DataPoint> LoadScenario(string fileName)
{
    var data = new Dictionary<DateTime, DataPoint>();

    foreach (var line in File.ReadLines(fileName).Skip(1))
    {
        var parts = line.Split(',');

        var time = DateTime.Parse(parts[0]);
        var heat = double.Parse(parts[1]);
        var price = double.Parse(parts[2]);

        data[time] = new DataPoint
        {
            HeatDemand = heat,
            ElectricityPrice = price
        };
    }

    return data;
}
}



/*            var manager = new SourceDataManager();

                var winter = manager.LoadScenario("WinterSourceDataSheet.csv");
                var summer = manager.LoadScenario("SummerSourceDataSheet.csv");

                System.Console.WriteLine(winter.Count);
                System.Console.WriteLine(summer.Count);

 This loads the data from the csv files and prints the number of entries in each scenario to the console.
 Not sure where to put it.
*/
}