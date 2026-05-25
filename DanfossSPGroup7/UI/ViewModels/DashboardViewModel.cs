using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using DanfossSPGroup7.Data;
using DanfossSPGroup7.Domain;
namespace DanfossSPGroup7.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    public ObservableCollection<ProductionUnit> AllUnits {get;}

    public DashboardViewModel(IAssetManager assetManager)
    {
        // get all units from the data manager
        var units = assetManager.GetProductionUnits();

        // adds the units into a collection so its visible for the ui
        AllUnits = new ObservableCollection<ProductionUnit>(units);
    }
}
