using Models.Core;
using NUnit.Framework;
using Gtk.Sheet;
using System.Collections.Generic;

namespace UnitTests.Sheet;

[TestFixture]
class ClassWithArrayPropertiesTests
{
    class ClassWithUnits 
    {
        [Display()]
        [Units("mm")]
        public string[] Depth { get; set; } = new string[] { "0-100", "100-200" };
    }

    /// <summary>Ensure units are found.</summary>
    [Test]
    public void TestStaticUnits()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithUnits());
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(2));
        Assert.That(dataProvider.GetColumnName(0), Is.EqualTo("Depth"));
        Assert.That(dataProvider.GetColumnUnits(0), Is.EqualTo("mm"));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("0-100"));
        Assert.That(dataProvider.GetCellContents(0, 1), Is.EqualTo("100-200"));
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetCellState.Normal));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetCellState.Normal));
    }

    class ClassWithDynamicUnits 
    {
        [Display]
        public string[] Depth { get; set; } = new string[] { "0-100", "100-200" };

        public string DepthUnits => "mm";
    }

    /// <summary>Ensure units are found.</summary>
    [Test]
    public void TestDynamicUnits()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithDynamicUnits());
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(2));
        Assert.That(dataProvider.GetColumnName(0), Is.EqualTo("Depth"));
        Assert.That(dataProvider.GetColumnUnits(0), Is.EqualTo("mm"));
    }

    class ClassWithValidUnits 
    {
        public enum UnitsEnum
        {
            X,
            Y
        }

        [Display]
        public string[] Depth { get; set; } = new string[] { "0-100", "100-200" };

        public UnitsEnum DepthUnits => UnitsEnum.X;
    }

    /// <summary>Ensure valid units are found.</summary>
    [Test]
    public void TestValidUnits()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithValidUnits());
        Assert.That(dataProvider.GetColumnUnits(0), Is.EqualTo("X"));
        Assert.That(dataProvider.GetColumnValidUnits(0), Is.EqualTo(new string[] { "X", "Y" }));
    }    

    class ClassWithFormat 
    {
        [Display(Format = "N2")]
        public double[] Value { get; set; } = new double[] { 1.0, 2.0 };
    }

    /// <summary>Ensure format is found.</summary>
    [Test]
    public void TestFormat()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithFormat());
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(2));
        Assert.That(dataProvider.GetColumnName(0), Is.EqualTo("Value"));
        Assert.That(dataProvider.GetColumnUnits(0), Is.Null);
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("1.00"));
        Assert.That(dataProvider.GetCellContents(0, 1), Is.EqualTo("2.00"));
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetCellState.Normal));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetCellState.Normal));
    }

    class ClassWithReadonly 
    {
        [Display()]
        public double[] Value { get; } = new double[] { 1.0, 2.0 };
    }
    
    /// <summary>Ensure readonly properties are found.</summary>
    [Test]
    public void TestReadonly()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithReadonly());
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetCellState.ReadOnly));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetCellState.ReadOnly));
    }    

    class ClassWithMetadata 
    {
        [Display()]
        public double[] Value { get; set; } = new double[] { 1.0, 2.0 };

        public string[] ValueMetadata { get; set; } = new string[] { "Calculated", null };

    }
    
    /// <summary>Ensure metadata properties are found and used.</summary>
    [Test]
    public void TestMetadata()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithMetadata());
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetCellState.Calculated));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetCellState.Normal));
    }       

    class ClassWithAlias 
    {
        [Display(DisplayName = "Alias")]
        public double[] Value { get; set; } = new double[] { 1.0, 2.0 };
    }
    
    /// <summary>Ensure alias is found and used.</summary>
    [Test]
    public void TestDisplayName()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithAlias());
        Assert.That(dataProvider.GetColumnName(0), Is.EqualTo("Alias"));
    }     

    class ClassWithNull 
    {
        [Display]
        public double[] Value { get; set; } = null;
    }
    
    /// <summary>Ensure a null property can be handled.</summary>
    [Test]
    public void TestNullProperty()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithNull());
        Assert.That(dataProvider.RowCount, Is.EqualTo(0));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.Null);
    }         
    
    /// <summary>Ensure cannot change the value of a readonly property.</summary>
    [Test]
    public void TestSetValuesDoesntChangeReadonlyProperties()
    {
        ClassWithReadonly model = new();
        var dataProvider = DataProviderFactory.Create(model);

        dataProvider.SetCellContents(colIndices: new int[] { 0 }, 
                                     rowIndices: new int[] { 0 },
                                     values: new string[] { "ZZZZ" });
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("1"));
    }      

    /// <summary>Ensure can change the value of a property.</summary>
    [Test]
    public void TestSetValuesWorks()
    {
        ClassWithUnits model = new();
        var dataProvider = DataProviderFactory.Create(model);

        dataProvider.SetCellContents(colIndices: new int[] { 0 }, 
                                     rowIndices: new int[] { 0 },
                                     values: new string[] { "ZZZZ" });
        Assert.That(model.Depth, Is.EqualTo(new string[] { "ZZZZ", "100-200" }));
    }   

    /// <summary>Ensure we can add values to property.</summary>
    [Test]
    public void TestSetValuesWillExpandArray()
    {
        ClassWithUnits model = new();
        var dataProvider = DataProviderFactory.Create(model);

        dataProvider.SetCellContents(colIndices: new int[] { 0 }, 
                                     rowIndices: new int[] { 2 },
                                     values: new string[] { "ZZZZ" });
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(3));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("0-100"));
        Assert.That(dataProvider.GetCellContents(0, 1), Is.EqualTo("100-200"));
        Assert.That(dataProvider.GetCellContents(0, 2), Is.EqualTo("ZZZZ"));
        Assert.That(model.Depth, Is.EqualTo(new string[] { "0-100", "100-200", "ZZZZ" }));
    } 

    /// <summary>Ensure 'calculated' metadata properties change when data is set.</summary>
    [Test]
    public void TestSetValueChangesMetadata()
    {
        ClassWithMetadata model = new();
        model.Value = null;
        var dataProvider = DataProviderFactory.Create(model);
        dataProvider.SetCellContents(colIndices: new int[] { 0 }, 
                                     rowIndices: new int[] { 0, 1 },
                                     values: new string[] { "3.0", "4.0" });
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetCellState.Normal));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetCellState.Normal));
        Assert.That(model.ValueMetadata, Is.EqualTo(new string[] { null, null }));
    }

    /// <summary>Ensure the CellChanged event is invoked when data is changed.</summary>
    [Test]
    public void TestSetValueInvokesDataChangedNotification()
    {
        ClassWithUnits model = new();
        var dataProvider = DataProviderFactory.Create(model);
        bool invoked = false;
        dataProvider.CellChanged += (s, c, r, v) => invoked = true;

        dataProvider.SetCellContents(colIndices: new int[] { 0 }, 
                                     rowIndices: new int[] { 1 },
                                     values: new string[] { "ZZZZ" });

        Assert.That(invoked, Is.True);
    }

    /// <summary>
    /// Ensure data provider can handle setting a value to a blank. This is what the sheet control does
    /// when the user hits 'Delete' on a cell.
    /// </summary>
    [Test]
    public void TestCanSetValueToBlank()
    {
        ClassWithFormat model = new();
        var dataProvider = DataProviderFactory.Create(model);
        dataProvider.SetCellContents(colIndices: new int[] { 0 }, 
                                     rowIndices: new int[] { 1 },
                                     values: new string[] { "" });

        Assert.That(model.Value[1], Is.NaN);
    }

    class ClassWithMultipleProperties 
    {
        [Display]
        public string[] A { get; set; } = new string[] { "a1", "a2", "a3", "a4" };

        [Display]
        public string[] B { get; set; } = new string[] { "b1", "b2", "b3", "b4" };

        [Display]
        public string[] C { get; set; } = new string[] { "c1", "c2", "c3", "c4" };

        [Display]
        public string[] D { get; set; } = new string[] { "d1", "d2", "d3", "d4" };
        }

    /// <summary>Ensure can delete entire row of grid.</summary>
    [Test]
    public void DeleteEntireRow()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithMultipleProperties());

        dataProvider.DeleteRows(new int[] { 1, 2 }); 
        
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(4));
        Assert.That(dataProvider.RowCount, Is.EqualTo(2));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("a1"));
        Assert.That(dataProvider.GetCellContents(1, 0), Is.EqualTo("b1"));
        Assert.That(dataProvider.GetCellContents(2, 0), Is.EqualTo("c1"));
        Assert.That(dataProvider.GetCellContents(3, 0), Is.EqualTo("d1"));
        Assert.That(dataProvider.GetCellContents(0, 1), Is.EqualTo("a4"));
        Assert.That(dataProvider.GetCellContents(1, 1), Is.EqualTo("b4"));
        Assert.That(dataProvider.GetCellContents(2, 1), Is.EqualTo("c4"));
        Assert.That(dataProvider.GetCellContents(3, 1), Is.EqualTo("d4"));
    }    
}
