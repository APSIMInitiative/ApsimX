using Models.Core;
using NUnit.Framework;
using UserInterface.Views;

namespace UnitTests.Sheet;

[TestFixture]
class ModelToSheetDataProviderTests
{
    class ModelWithUnits : Model
    {
        [Display()]
        [Units("mm")]
        public string[] Depth { get; set; } = new string[] { "0-100", "100-200" };
    }

    /// <summary>Ensure units are found.</summary>
    [Test]
    public void TestUnits()
    {
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(new ModelWithUnits());
        Assert.AreEqual(1, dataProvider.ColumnCount);
        Assert.AreEqual(4, dataProvider.RowCount);
        Assert.AreEqual("Depth", dataProvider.GetCellContents(0, 0));
        Assert.AreEqual("mm", dataProvider.GetCellContents(0, 1));
        Assert.AreEqual("0-100", dataProvider.GetCellContents(0, 2));
        Assert.AreEqual("100-200", dataProvider.GetCellContents(0, 3));
        Assert.AreEqual(SheetDataProviderCellState.ReadOnly, dataProvider.GetCellState(0, 0));
        Assert.AreEqual(SheetDataProviderCellState.ReadOnly, dataProvider.GetCellState(0, 1));
        Assert.AreEqual(SheetDataProviderCellState.Normal, dataProvider.GetCellState(0, 2));        
        Assert.AreEqual(SheetDataProviderCellState.Normal, dataProvider.GetCellState(0, 3));        
    }


    class ModelWithFormat : Model
    {
        [Display(Format = "N2")]
        public double[] Value { get; set; } = new double[] { 1.0, 2.0 };
    }

    /// <summary>Ensure format is found.</summary>
    [Test]
    public void TestFormat()
    {
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(new ModelWithFormat());
        Assert.AreEqual(1, dataProvider.ColumnCount);
        Assert.AreEqual(3, dataProvider.RowCount);
        Assert.AreEqual("Value", dataProvider.GetCellContents(0, 0));
        Assert.AreEqual("1.00", dataProvider.GetCellContents(0, 1));
        Assert.AreEqual("2.00", dataProvider.GetCellContents(0, 2));
        Assert.AreEqual(SheetDataProviderCellState.ReadOnly, dataProvider.GetCellState(0, 0));
        Assert.AreEqual(SheetDataProviderCellState.Normal, dataProvider.GetCellState(0, 1));
        Assert.AreEqual(SheetDataProviderCellState.Normal, dataProvider.GetCellState(0, 2));        
    }

    class ModelWithReadonly : Model
    {
        [Display()]
        public double[] Value { get; } = new double[] { 1.0, 2.0 };
    }
    
    /// <summary>Ensure readonly properties are found.</summary>
    [Test]
    public void TestReadonly()
    {
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(new ModelWithReadonly());
        Assert.AreEqual(SheetDataProviderCellState.ReadOnly, dataProvider.GetCellState(0, 0));
        Assert.AreEqual(SheetDataProviderCellState.ReadOnly, dataProvider.GetCellState(0, 1));
        Assert.AreEqual(SheetDataProviderCellState.ReadOnly, dataProvider.GetCellState(0, 2));
    }    

    class ModelWithMetadata : Model
    {
        [Display()]
        public double[] Value { get; set; } = new double[] { 1.0, 2.0 };

        public string[] ValueMetadata { get; } = new string[] { "Calculated", null };

    }
    
    /// <summary>Ensure metadata properties are found and used.</summary>
    [Test]
    public void TestMetadata()
    {
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(new ModelWithMetadata());
        Assert.AreEqual(SheetDataProviderCellState.ReadOnly, dataProvider.GetCellState(0, 0));
        Assert.AreEqual(SheetDataProviderCellState.Calculated, dataProvider.GetCellState(0, 1));
        Assert.AreEqual(SheetDataProviderCellState.Normal, dataProvider.GetCellState(0, 2));
    }       

    class ModelWithAlias : Model
    {
        [Display(DisplayName = "Alias")]
        public double[] Value { get; set; } = new double[] { 1.0, 2.0 };
    }
    
    /// <summary>Ensure metadata properties are found and used.</summary>
    [Test]
    public void TestDisplayName()
    {
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(new ModelWithAlias());
        Assert.AreEqual("Alias", dataProvider.GetCellContents(0, 0));
    }     
}
