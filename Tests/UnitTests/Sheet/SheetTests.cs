namespace UnitTests.Sheet
{
    using NUnit.Framework;
    using System.Collections.Generic;
    using UserInterface.Views;
    using Gtk.Sheet;
    using System.Drawing;
    using Models.Core;

    [TestFixture]
    class SheetTests
    {
        /// <summary>Ensure the cell positions correctly with no scrolling.</summary>
        [Test]
        public void SheetCellPositingOkForNoScrolling()
        {
            var data = Utilities.CreateTable(new string[] {                  "A",  "B",   "C",  "D" },
                                      new List<object[]> { new object[] {   "a1", "b1",  "c1", "d1" },
                                                           new object[] {   "a2", "b2",  "c2", "d2" },
                                                           new object[] {   "a3", "b3",  "c3", "d3" },
                                                           new object[] {   "a4", "b4",  "c4", "d4" }});
            var units = new string[] { null, "g/m2", null, null };
            var sheet = new Sheet(new DataTableProvider(data, units),
                                  numberFrozenRows: 1,
                                  numberFrozenColumns: 0,
                                  columnWidths: new int[] { 30, 40, 50, 60 },
                                  blankRowAtBottom: false); 
            sheet.Width = 80;
            sheet.Height = 80;

            // assumes cellpadding of 2 x 10, rowHeight = 35
            // Row 0
            Assert.That(sheet.CalculateBounds(0, 0), Is.EqualTo(new Rectangle(0, 0, 30, 35))); 
            Assert.That(sheet.CalculateBounds(1, 0), Is.EqualTo(new Rectangle(30, 0, 40, 35)));
            Assert.That(sheet.CalculateBounds(2, 0), Is.EqualTo(new Rectangle(70, 0, 50, 35)));
            Assert.That(sheet.CalculateBounds(3, 0), Is.EqualTo(Rectangle.Empty));

            // Row 1
            Assert.That(sheet.CalculateBounds(0, 1), Is.EqualTo(new Rectangle(0, 35, 30, 35)));
            Assert.That(sheet.CalculateBounds(1, 1), Is.EqualTo(new Rectangle(30, 35, 40, 35)));
            Assert.That(sheet.CalculateBounds(2, 1), Is.EqualTo(new Rectangle(70, 35, 50, 35)));
            Assert.That(sheet.CalculateBounds(3, 1), Is.EqualTo(Rectangle.Empty));

            // Row 2
            Assert.That(sheet.CalculateBounds(0, 2), Is.EqualTo(new Rectangle(0, 70, 30, 35)));
            Assert.That(sheet.CalculateBounds(1, 2), Is.EqualTo(new Rectangle(30, 70, 40, 35)));
            Assert.That(sheet.CalculateBounds(2, 2), Is.EqualTo(new Rectangle(70, 70, 50, 35)));
            Assert.That(sheet.CalculateBounds(3, 2), Is.EqualTo(Rectangle.Empty));

            // Row 3
            Assert.That(sheet.CalculateBounds(0, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(1, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(2, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(3, 3), Is.EqualTo(Rectangle.Empty));
        }

        /// <summary>Ensure sheet view can be scrolled one cell to the right with no frozen columns.</summary>
        [Test]
        public void ScrollRightWithNoFrozenColumnsColumnsDontFit()
        {
            var data = Utilities.CreateTable(new string[] { "A", "B", "C", "D" },
                                      new List<object[]> { new object[] {   "a1", "b1",  "c1", "d1" },
                                                           new object[] {   "a2", "b2",  "c2", "d2" },
                                                           new object[] {   "a3", "b3",  "c3", "d3" },
                                                           new object[] {   "a4", "b4",  "c4", "d4" }});
            var units = new string[] { null, "g/m2", null, null };
            var sheet = new Sheet(new DataTableProvider(data, units),
                                  numberFrozenRows: 1,
                                  numberFrozenColumns: 0,
                                  columnWidths: new int[] { 20, 20, 20, 20 },
                                  blankRowAtBottom: false);
            sheet.Width = 60;
            sheet.Height = 80;

            sheet.ScrollRight();

            // assumes cellpadding of 2 x 10, rowHeight = 35
            // Row 0
            Assert.That(sheet.CalculateBounds(0, 0), Is.EqualTo(Rectangle.Empty)); // hidden
            Assert.That(sheet.CalculateBounds(1, 0), Is.EqualTo(new Rectangle(0, 0, 20, 35)));
            Assert.That(sheet.CalculateBounds(2, 0), Is.EqualTo(new Rectangle(20, 0, 20, 35)));
            Assert.That(sheet.CalculateBounds(3, 0), Is.EqualTo(new Rectangle(40, 0, 20, 35)));

            // Row 1
            Assert.That(sheet.CalculateBounds(0, 1), Is.EqualTo(Rectangle.Empty)); // hidden
            Assert.That(sheet.CalculateBounds(1, 1), Is.EqualTo(new Rectangle(0, 35, 20, 35)));
            Assert.That(sheet.CalculateBounds(2, 1), Is.EqualTo(new Rectangle(20, 35, 20, 35)));
            Assert.That(sheet.CalculateBounds(3, 1), Is.EqualTo(new Rectangle(40, 35, 20, 35)));

            // Row 2
            Assert.That(sheet.CalculateBounds(0, 2), Is.EqualTo(Rectangle.Empty));  // hidden
            Assert.That(sheet.CalculateBounds(1, 2), Is.EqualTo(new Rectangle(0, 70, 20, 35)));
            Assert.That(sheet.CalculateBounds(2, 2), Is.EqualTo(new Rectangle(20, 70, 20, 35)));
            Assert.That(sheet.CalculateBounds(3, 2), Is.EqualTo(new Rectangle(40, 70, 20, 35)));

            // Row 3
            Assert.That(sheet.CalculateBounds(0, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(1, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(2, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(3, 3), Is.EqualTo(Rectangle.Empty));
        }

        [Test]
        public void ScrollRightWithNothingToScroll()
        {
            var data = Utilities.CreateTable(new string[] { "A", "B", "C", "D" },
                                      new List<object[]> { new object[] {   "a1", "b1",  "c1", "d1" },
                                                           new object[] {   "a2", "b2",  "c2", "d2" },
                                                           new object[] {   "a3", "b3",  "c3", "d3" },
                                                           new object[] {   "a4", "b4",  "c4", "d4" }});
            var units = new string[] { null, "g/m2", null, null };
            var sheet = new Sheet(new DataTableProvider(data, units),
                                  numberFrozenRows: 1,
                                  numberFrozenColumns: 0,
                                  columnWidths: new int[] { 10, 20, 20, 20 },
                                  blankRowAtBottom: false);
            sheet.Width = 80;
            sheet.Height = 80;

            sheet.ScrollRight();

            // assumes cellpadding of 2 x 10, rowHeight = 35
            // Row 0
            Assert.That(sheet.CalculateBounds(0, 0), Is.EqualTo(new Rectangle(0, 0, 10, 35)));
            Assert.That(sheet.CalculateBounds(1, 0), Is.EqualTo(new Rectangle(10, 0, 20, 35)));
            Assert.That(sheet.CalculateBounds(2, 0), Is.EqualTo(new Rectangle(30, 0, 20, 35)));
            Assert.That(sheet.CalculateBounds(3, 0), Is.EqualTo(new Rectangle(50, 0, 20, 35)));

            // Row 1
            Assert.That(sheet.CalculateBounds(0, 1), Is.EqualTo(new Rectangle(0, 35, 10, 35)));
            Assert.That(sheet.CalculateBounds(1, 1), Is.EqualTo(new Rectangle(10, 35, 20, 35)));
            Assert.That(sheet.CalculateBounds(2, 1), Is.EqualTo(new Rectangle(30, 35, 20, 35)));
            Assert.That(sheet.CalculateBounds(3, 1), Is.EqualTo(new Rectangle(50, 35, 20, 35)));

            // Row 2
            Assert.That(sheet.CalculateBounds(0, 2), Is.EqualTo(new Rectangle(0, 70, 10, 35)));
            Assert.That(sheet.CalculateBounds(1, 2), Is.EqualTo(new Rectangle(10, 70, 20, 35)));
            Assert.That(sheet.CalculateBounds(2, 2), Is.EqualTo(new Rectangle(30, 70, 20, 35)));
            Assert.That(sheet.CalculateBounds(3, 2), Is.EqualTo(new Rectangle(50, 70, 20, 35)));

            // Row 3
            Assert.That(sheet.CalculateBounds(0, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(1, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(2, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(3, 3), Is.EqualTo(Rectangle.Empty));
        }

        /// <summary>
        /// Ensure sheet view can be scrolled one cell to the right with one frozen columns.
        /// </summary>
        [Test]
        public void ScrollRightWithOneFrozenColumnColumnsDontFit()
        {
            var data = Utilities.CreateTable(new string[] { "A", "B", "C", "D" },
                                      new List<object[]> { new object[] {   "a1", "b1",  "c1", "d1" },
                                                           new object[] {   "a2", "b2",  "c2", "d2" },
                                                           new object[] {   "a3", "b3",  "c3", "d3" },
                                                           new object[] {   "a4", "b4",  "c4", "d4" }});
            var units = new string[] { null, "g/m2", null, null };
            var sheet = new Sheet(new DataTableProvider(data, units),
                                  numberFrozenRows: 1,
                                  numberFrozenColumns: 1,
                                  columnWidths: new int[] { 10, 20, 30, 40 },
                                  blankRowAtBottom: false);
            sheet.Width = 80;
            sheet.Height = 80;

            sheet.ScrollRight(); // This will have to scroll right 2 columns because column indexes 1,2,3 won't fit into a width of 80 pixels.

            // assumes cellpadding of 2 x 10, rowHeight = 35
            // Row 0
            Assert.That(sheet.CalculateBounds(0, 0), Is.EqualTo(new Rectangle(0, 0, 10, 35)));
            Assert.That(sheet.CalculateBounds(1, 0), Is.EqualTo(Rectangle.Empty));  // hidden column
            Assert.That(sheet.CalculateBounds(2, 0), Is.EqualTo(Rectangle.Empty));  // hidden column
            Assert.That(sheet.CalculateBounds(3, 0), Is.EqualTo(new Rectangle(10, 0, 40, 35)));

            // Row 1
            Assert.That(sheet.CalculateBounds(0, 1), Is.EqualTo(new Rectangle(0, 35, 10, 35)));
            Assert.That(sheet.CalculateBounds(1, 1), Is.EqualTo(Rectangle.Empty));  // hidden column
            Assert.That(sheet.CalculateBounds(2, 1), Is.EqualTo(Rectangle.Empty));  // hidden column
            Assert.That(sheet.CalculateBounds(3, 1), Is.EqualTo(new Rectangle(10, 35, 40, 35)));

            // Row 2
            Assert.That(sheet.CalculateBounds(0, 2), Is.EqualTo(new Rectangle(0, 70, 10, 35)));
            Assert.That(sheet.CalculateBounds(1, 2), Is.EqualTo(Rectangle.Empty));  // hidden column
            Assert.That(sheet.CalculateBounds(2, 2), Is.EqualTo(Rectangle.Empty));  // hidden column
            Assert.That(sheet.CalculateBounds(3, 2), Is.EqualTo(new Rectangle(10, 70, 40, 35)));

            // Row 3
            Assert.That(sheet.CalculateBounds(0, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(1, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(2, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(3, 3), Is.EqualTo(Rectangle.Empty));
        }

        /// <summary>Ensure sheet view can be scrolled one cell to the left with one frozen columns.</summary>
        [Test]
        public void ScrollLeftWithOneFrozenColumnColumnsDontFit()
        {
            var data = Utilities.CreateTable(new string[] { "A", "B", "C", "D" },
                                      new List<object[]> { new object[] {   "a1", "b1",  "c1", "d1" },
                                                           new object[] {   "a2", "b2",  "c2", "d2" },
                                                           new object[] {   "a3", "b3",  "c3", "d3" },
                                                           new object[] {   "a4", "b4",  "c4", "d4" }});
            var units = new string[] { null, "g/m2", null, null };
            var sheet = new Sheet(new DataTableProvider(data, units),
                                  numberFrozenRows: 1,
                                  numberFrozenColumns: 1,
                                  columnWidths: new int[] { 10, 20, 30, 40 },
                                  blankRowAtBottom: false);
            sheet.Width = 80;
            sheet.Height = 80;

            sheet.ScrollRight(); // This will have to scroll right 2 columns because column indexes 1,2,3 won't fit into a width of 80 pixels.
            sheet.ScrollLeft();
            sheet.ScrollLeft();

            // assumes cellpadding of 2 x 10, rowHeight = 35
            Assert.That(sheet.CalculateBounds(0, 0), Is.EqualTo(new Rectangle(0, 0, 10, 35)));
            Assert.That(sheet.CalculateBounds(1, 0), Is.EqualTo(new Rectangle(10, 0, 20, 35)));
            Assert.That(sheet.CalculateBounds(2, 0), Is.EqualTo(new Rectangle(30, 0, 30, 35)));
            Assert.That(sheet.CalculateBounds(3, 0), Is.EqualTo(new Rectangle(60, 0, 40, 35)));

            // Row 1
            Assert.That(sheet.CalculateBounds(0, 1), Is.EqualTo(new Rectangle(0, 35, 10, 35)));
            Assert.That(sheet.CalculateBounds(1, 1), Is.EqualTo(new Rectangle(10, 35, 20, 35)));
            Assert.That(sheet.CalculateBounds(2, 1), Is.EqualTo(new Rectangle(30, 35, 30, 35)));
            Assert.That(sheet.CalculateBounds(3, 1), Is.EqualTo(new Rectangle(60, 35, 40, 35)));

            // Row 2
            Assert.That(sheet.CalculateBounds(0, 2), Is.EqualTo(new Rectangle(0, 70, 10, 35)));
            Assert.That(sheet.CalculateBounds(1, 2), Is.EqualTo(new Rectangle(10, 70, 20, 35)));
            Assert.That(sheet.CalculateBounds(2, 2), Is.EqualTo(new Rectangle(30, 70, 30, 35)));
            Assert.That(sheet.CalculateBounds(3, 2), Is.EqualTo(new Rectangle(60, 70, 40, 35)));

            // Row 3
            Assert.That(sheet.CalculateBounds(0, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(1, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(2, 3), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(3, 3), Is.EqualTo(Rectangle.Empty));
        }

        /// <summary>Ensure sheet view can be scrolled one row down with one heading.</summary>
        [Test]
        public void ScrollDownOneRowWithOneHeading()
        {
            var data = Utilities.CreateTable(new string[] { "A", "B", "C", "D" },
                                      new List<object[]> { new object[] {   "a1", "b1",  "c1", "d1" },
                                                           new object[] {   "a2", "b2",  "c2", "d2" },
                                                           new object[] {   "a3", "b3",  "c3", "d3" },
                                                           new object[] {   "a4", "b4",  "c4", "d4" }});
            var units = new string[] { null, "g/m2", null, null };
            var dataProvider = new DataTableProvider(data, units);
            var sheet = new Sheet(dataProvider,
                                  numberFrozenRows: 1,
                                  numberFrozenColumns: 1,
                                  columnWidths: new int[] { 30, 40, 50, 60 },
                                  blankRowAtBottom: false);
            sheet.Width = 80;
            sheet.Height = 80;

            sheet.ScrollDown();

            // assumes cellpadding of 2 x 10, rowHeight = 35
            Assert.That(sheet.CalculateBounds(0, 0), Is.EqualTo(new Rectangle(0, 0, 30, 35)));
            Assert.That(sheet.CalculateBounds(1, 0), Is.EqualTo(new Rectangle(30, 0, 40, 35)));
            Assert.That(sheet.CalculateBounds(2, 0), Is.EqualTo(new Rectangle(70, 0, 50, 35)));
            Assert.That(sheet.CalculateBounds(3, 0), Is.EqualTo(Rectangle.Empty));

            // Row 1 - hidden
            Assert.That(sheet.CalculateBounds(0, 1), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(1, 1), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(2, 1), Is.EqualTo(Rectangle.Empty));
            Assert.That(sheet.CalculateBounds(3, 1), Is.EqualTo(Rectangle.Empty));

            // Row 2
            Assert.That(sheet.CalculateBounds(0, 2), Is.EqualTo(new Rectangle(0, 35, 30, 35)));
            Assert.That(sheet.CalculateBounds(1, 2), Is.EqualTo(new Rectangle(30, 35, 40, 35)));
            Assert.That(sheet.CalculateBounds(2, 2), Is.EqualTo(new Rectangle(70, 35, 50, 35)));
            Assert.That(sheet.CalculateBounds(3, 2), Is.EqualTo(Rectangle.Empty));


            // Row 3
            Assert.That(sheet.CalculateBounds(0, 3), Is.EqualTo(new Rectangle(0, 70, 30, 35)));
            Assert.That(sheet.CalculateBounds(1, 3), Is.EqualTo(new Rectangle(30, 70, 40, 35)));
            Assert.That(sheet.CalculateBounds(2, 3), Is.EqualTo(new Rectangle(70, 70, 50, 35)));
            Assert.That(sheet.CalculateBounds(3, 3), Is.EqualTo(Rectangle.Empty));
        }

        /// <summary>Ensure can delete entire row of grid with data table as backend..</summary>
        [Test]
        public void DeleteEntireRowOfSheetDataTable()
        {
            var data = Utilities.CreateTable(new string[] { "A", "B", "C", "D" },
                                      new List<object[]> { new object[] {   "a1", "b1",  "c1", "d1" },
                                                           new object[] {   "a2", "b2",  "c2", "d2" },
                                                           new object[] {   "a3", "b3",  "c3", "d3" },
                                                           new object[] {   "a4", "b4",  "c4", "d4" }});
            var dataProvider = new DataTableProvider(data, null);
            var sheet = new Sheet(dataProvider,
                                  numberFrozenRows: 1,
                                  numberFrozenColumns: 0,
                                  columnWidths: new int[] { 30, 40, 50, 60 },
                                  blankRowAtBottom: false);
            var multiCellSelector = new MultiCellSelect(sheet);
            sheet.CellSelector = multiCellSelector;

            // select the 2nd and 3rd rows
            multiCellSelector.SetSelection(0, 2, 3, 3); 

            // delete the selection.
            multiCellSelector.Delete(); 
            
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
}
