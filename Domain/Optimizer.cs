using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DanfossSPGroup7.Data;

namespace DanfossSPGroup7.Domain
{
    internal class Optimizer
    {
        public static Optimizer? Instance { get; private set; }
        public Dictionary<DateTime, DataPoint> Summer { get; private set; }
        public Optimizer(ISourceDataManager sourceDataManager)
        {
            Instance = this;
            Summer = sourceDataManager.summer;
        }
    }
}
