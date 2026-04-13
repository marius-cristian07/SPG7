using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DanfossSPGroup7.Domain;

namespace DanfossSPGroup7.Data
{
    public interface IAssetManager
    {
        List<ProductionUnit> GetProductionUnits();
        
    }
}
