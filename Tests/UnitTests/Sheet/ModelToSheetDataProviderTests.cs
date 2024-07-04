using Models.Core;
using NUnit.Framework;
using UnitTests.Interop.Documentation.TagRenderers;
using UserInterface.Views;
using Gtk.Sheet;

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
    public void TestStaticUnits()
    {
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(new ModelWithUnits());
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(4));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("Depth"));
        Assert.That(dataProvider.GetCellContents(0, 1), Is.EqualTo("mm"));
        Assert.That(dataProvider.GetCellContents(0, 2), Is.EqualTo("0-100"));
        Assert.That(dataProvider.GetCellContents(0, 3), Is.EqualTo("100-200"));
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetDataProviderCellState.ReadOnly));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetDataProviderCellState.ReadOnly));
        Assert.That(dataProvider.GetCellState(0, 2), Is.EqualTo(SheetDataProviderCellState.Normal));
        Assert.That(dataProvider.GetCellState(0, 3), Is.EqualTo(SheetDataProviderCellState.Normal));
    }

    class ModelWithDynamicUnits : Model
    {
        [Display]
        public string[] Depth { get; set; } = new string[] { "0-100", "100-200" };

        public string DepthUnits => "mm";
    }

    /// <summary>Ensure units are found.</summary>
    [Test]
    public void TestDynamicUnits()
    {
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(new ModelWithDynamicUnits());
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(4));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("Depth"));
        Assert.That(dataProvider.GetCellContents(0, 1), Is.EqualTo("mm"));
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
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(3));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("Value"));
        Assert.That(dataProvider.GetCellContents(0, 1), Is.EqualTo("1.00"));
        Assert.That(dataProvider.GetCellContents(0, 2), Is.EqualTo("2.00"));
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetDataProviderCellState.ReadOnly));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetDataProviderCellState.Normal));
        Assert.That(dataProvider.GetCellState(0, 2), Is.EqualTo(SheetDataProviderCellState.Normal));
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
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetDataProviderCellState.ReadOnly));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetDataProviderCellState.ReadOnly));
        Assert.That(dataProvider.GetCellState(0, 2), Is.EqualTo(SheetDataProviderCellState.ReadOnly));
    }    

    class ModelWithMetadata : Model
    {
        [Display()]
        public double[] Value { get; set; } = new double[] { 1.0, 2.0 };

        public string[] ValueMetadata { get; set; } = new string[] { "Calculated", null };

    }
    
    /// <summary>Ensure metadata properties are found and used.</summary>
    [Test]
    public void TestMetadata()
    {
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(new ModelWithMetadata());
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetDataProviderCellState.ReadOnly));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetDataProviderCellState.Calculated));
        Assert.That(dataProvider.GetCellState(0, 2), Is.EqualTo(SheetDataProviderCellState.Normal));
    }       

    class ModelWithAlias : Model
    {
        [Display(DisplayName = "Alias")]
        public double[] Value { get; set; } = new double[] { 1.0, 2.0 };
    }
    
    /// <summary>Ensure alias is found and used.</summary>
    [Test]
    public void TestDisplayName()
    {
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(new ModelWithAlias());
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("Alias"));
    }     

    class ModelWithNull : Model
    {
        [Display]
        public double[] Value { get; set; } = null;
    }
    
    /// <summary>Ensure a null property can be handled.</summary>
    [Test]
    public void TestNullProperty()
    {
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(new ModelWithNull());
        Assert.That(dataProvider.RowCount, Is.EqualTo(1));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("Value"));
    }         
    
    /// <summary>Ensure cannot change the value of a readonly property.</summary>
    [Test]
    public void TestSetValuesDoesntChangeReadonlyProperties()
    {
        ModelWithUnits model = new();
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(model);

        dataProvider.SetCellContents(colIndices: new int[] { 0 }, 
                                     rowIndices: new int[] { 0 },   // Try changing the header cell.
                                     values: new string[] { "ZZZZ" });
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(4));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("Depth"));
    }      

    /// <summary>Ensure can change the value of a property.</summary>
    [Test]
    public void TestSetValuesWorks()
    {
        ModelWithUnits model = new();
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(model);

        dataProvider.SetCellContents(colIndices: new int[] { 0 }, 
                                     rowIndices: new int[] { 2 },
                                     values: new string[] { "ZZZZ" });
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(4));
        Assert.That(model.Depth, Is.EqualTo(new string[] { "ZZZZ", "100-200" }));
    }   

    /// <summary>Ensure we can add values to property.</summary>
    [Test]
    public void TestSetValuesWillExpandArray()
    {
        ModelWithUnits model = new();
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(model);

        dataProvider.SetCellContents(colIndices: new int[] { 0 }, 
                                     rowIndices: new int[] { 4 },
                                     values: new string[] { "ZZZZ" });
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(5));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("Depth"));
        Assert.That(dataProvider.GetCellContents(0, 1), Is.EqualTo("mm"));
        Assert.That(dataProvider.GetCellContents(0, 2), Is.EqualTo("0-100"));
        Assert.That(dataProvider.GetCellContents(0, 3), Is.EqualTo("100-200"));
        Assert.That(dataProvider.GetCellContents(0, 4), Is.EqualTo("ZZZZ"));
        Assert.That(model.Depth, Is.EqualTo(new string[] { "0-100", "100-200", "ZZZZ" }));
    } 

    /// <summary>Ensure 'calculated' metadata properties change when data is set.</summary>
    [Test]
    public void TestSetValueWithNullProperty()
    {
        ModelWithMetadata model = new();
        model.Value = null;
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(model);
        dataProvider.SetCellContents(colIndices: new int[] { 0 }, 
                                     rowIndices: new int[] { 1, 2 },
                                     values: new string[] { "3.0", "4.0" });
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetDataProviderCellState.ReadOnly));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetDataProviderCellState.Normal));
        Assert.That(dataProvider.GetCellState(0, 2), Is.EqualTo(SheetDataProviderCellState.Normal));
        Assert.That(model.ValueMetadata, Is.EqualTo(new string[] { null, null }));
    }      

    /// <summary>Ensure 'calculated' metadata properties change when data is set.</summary>
    [Test]
    public void TestSetValueChangesMetadata()
    {
        ModelWithMetadata model = new();
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(model);
        dataProvider.SetCellContents(colIndices: new int[] { 0 }, 
                                     rowIndices: new int[] { 1, 2 },
                                     values: new string[] { "3.0", "4.0" });
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetDataProviderCellState.ReadOnly));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetDataProviderCellState.Normal));
        Assert.That(dataProvider.GetCellState(0, 2), Is.EqualTo(SheetDataProviderCellState.Normal));
        Assert.That(model.ValueMetadata, Is.EqualTo(new string[] { null, null }));
    }    

    /// <summary>Ensure the CellChanged event is invoked when data is changed.</summary>
    [Test]
    public void TestSetValueInvokesDataChangedNotification()
    {
        ModelWithUnits model = new();
        var dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(model);
        bool invoked = false;
        dataProvider.CellChanged += (s, c, r, v) => invoked = true;

        dataProvider.SetCellContents(colIndices: new int[] { 0 }, 
                                     rowIndices: new int[] { 2 },
                                     values: new string[] { "ZZZZ" });

        Assert.That(invoked, Is.True);
    }
}
