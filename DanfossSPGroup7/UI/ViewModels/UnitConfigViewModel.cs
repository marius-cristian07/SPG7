using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using DanfossSPGroup7.Domain;

namespace DanfossSPGroup7.UI.ViewModels;

public partial class UnitConfigViewModel : ObservableObject
{
    public event Action? ConfigChanged;

    public ProductionUnit Unit { get; set; }
    public Bitmap UnitImage { get; }

    [ObservableProperty] private bool _isSelectedForMaintenance;
    [ObservableProperty] private int _maintenanceDuration = 30;
    [ObservableProperty] private int _maintenanceStartDayIndex;

    private int _lastValidMaintenanceStartDayIndex;

    public UnitConfigViewModel(ProductionUnit unit)
    {
        Unit = unit;
        UnitImage = new Bitmap(AssetLoader.Open(new Uri(unit.ImagePath)));
    }

    partial void OnIsSelectedForMaintenanceChanged(bool value)
    {
        ConfigChanged?.Invoke();
    }

    partial void OnMaintenanceStartDayIndexChanged(int value)
    {
        if (value < 0)
        {
            MaintenanceStartDayIndex = _lastValidMaintenanceStartDayIndex;
            return;
        }

        _lastValidMaintenanceStartDayIndex = Math.Clamp(value, 0, 3);
        ConfigChanged?.Invoke();
    }

    partial void OnMaintenanceDurationChanged(int value)
    {
        ConfigChanged?.Invoke();
    }
}
