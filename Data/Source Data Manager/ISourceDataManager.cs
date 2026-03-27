using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanfossSPGroup7.Data
{
    public interface ISourceDataManager
    {
        public Dictionary<DateTime, DataPoint> LoadScenario(string fileName);
        public Dictionary<DateTime, DataPoint> summer { get; }
        public Dictionary<DateTime, DataPoint> winter { get; }
    }
}