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
        // Get all units from the data manager
        var units = assetManager.GetProductionUnits();

        // Put the units in a collection so the UI can show them
        AllUnits = new ObservableCollection<ProductionUnit>(units);
    }
}
