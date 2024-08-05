using Models.Core;
using NUnit.Framework;
using Gtk.Sheet;
using System.Data;

namespace UnitTests.Sheet;

[TestFixture]
class ClassWithDataTableTests
{
    class ClassWithDataTable
    {
        [Display]
        public DataTable Data { get; set; }
    }

    /// <summary>Ensure a datatable property is found.</summary>
    [Test]
    public void TestDataTable()
    {
        var obj = new ClassWithDataTable();
        obj.Data = new DataTable();
        obj.Data.Columns.Add(new DataColumn("A", typeof(double)));
        var row = obj.Data.NewRow();
        row["A"] = 1.0;
        obj.Data.Rows.Add(row);

        var dataProvider = DataProviderFactory.Create(obj);

        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(1));
        Assert.That(dataProvider.GetColumnName(0), Is.EqualTo("A"));
        Assert.That(dataProvider.GetColumnUnits(0), Is.Null);
        Assert.That(dataProvider.GetCellContents( 0, 0), Is.EqualTo("1.000"));
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetCellState.Normal));
    }
}