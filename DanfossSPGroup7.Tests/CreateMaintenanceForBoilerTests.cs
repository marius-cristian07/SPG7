using System;
using System.Collections.Generic;
using DanfossSPGroup7.Domain;
using DanfossSPGroup7.Data;
namespace DanfossSPGroup7.Tests;


public class CreateMaintenanceForBoilerTests
{
    // test when the boiler exists
    [Fact]
    public void CreateMaintenanceForBoiler_WhenNameExistsInTheList_SetCorrectEndDate()
    {
        // Arrange
        var testUnitObj = new ProductionUnit { Name = "GB1"};
        var testUnitList = new List<ProductionUnit> { testUnitObj };
        var sut = new MaintenanceCalculation();
        var testNameBoiler = "GB1";
        var testDuration = 59;
        var testEndDate = new DateTime(2026, 1, 10, 11, 0, 0);
        var startDate = testEndDate.AddHours(-testDuration);

        // Act
        sut.CreateMaintenanceForBoiler(testNameBoiler, testDuration, testUnitList, startDate);

        // Assert
        Assert.Equal(testEndDate, testUnitObj.MaintenancePeriods[0].End);
    }
    // test when the boiler does not exist
    [Fact]
    public void CreateMaintenanceForBoiler_WhenNoNameExist_ThrowErrorMessage()
    {
        // Arrange
        var testUnitObj = new ProductionUnit { Name = "GB1"};
        var testUnitList = new List<ProductionUnit> { testUnitObj };
        var sut = new MaintenanceCalculation();
        var testNameBoiler = "Banana";
        var testDuration = 59;
        var startDate = new DateTime(2026, 1, 1, 0, 0, 0);

        // Act
        var exception = Assert.Throws<ArgumentException>(() =>
        sut.CreateMaintenanceForBoiler(testNameBoiler, testDuration, testUnitList, startDate));

        // Assert
        Assert.Equal($"Boiler '{testNameBoiler}' is not found", exception.Message);
    }

    // test when two boilers have the same name
    [Fact]
    public void CreateMaintenanceForBoiler_UnitExistTwice_FirstUnitShouldAdapt()
    {
        // Arrange
        var testUnitObj = new ProductionUnit { Name = "GB1"};
        var secoundTestObj = new ProductionUnit {Name = "GB1"};
        var testUnitList = new List<ProductionUnit> { testUnitObj, secoundTestObj };
        var sut = new MaintenanceCalculation();
        var testNameBoiler = "GB1";
        var testDuration = 70;
        var testEndDate = new DateTime(2026, 1, 10, 22, 0, 0);
        var startDate = testEndDate.AddHours(-testDuration);

        // Act
        sut.CreateMaintenanceForBoiler(testNameBoiler, testDuration, testUnitList, startDate);

        // Assert
        Assert.Equal(testEndDate, testUnitObj.MaintenancePeriods[0].End);
        Assert.Empty(secoundTestObj.MaintenancePeriods);    
        }
}
