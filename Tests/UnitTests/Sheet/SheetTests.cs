namespace UnitTests.Sheet
{
    using APSIM.Shared.Utilities;
    using Cairo;
    using Models;
    using Models.Core;
    using Models.Interfaces;
    using Models.Soils;
    using Models.Soils.Nutrients;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using UserInterface.Views;

    [TestFixture]
    class SheetTests
    {
        class MockTextExtents : ITextExtents
        {
            public TextExtents TextExtents(string text)
            {
                return new TextExtents() { Width = 10, Height = 20 };
            }
        }

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
            var sheet = new SheetView(new DataTableProvider(data, units), new MockTextExtents(), 70, 80, 1, 0, null);
            
            // assumes cellpadding of 2 x 10, rowHeight = 35
            // Row 0
            Assert.AreEqual(new Rectangle(0, 0, 30, 35), sheet.CalculateBounds(0, 0).ToRectangle()); 
            Assert.AreEqual(new Rectangle(30, 0, 30, 35), sheet.CalculateBounds(1, 0).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 0, 30, 35), sheet.CalculateBounds(2, 0).ToRectangle());
            Assert.IsNull(sheet.CalculateBounds(3, 0));

            // Row 1
            Assert.AreEqual(new Rectangle(0, 35, 30, 35), sheet.CalculateBounds(0, 1).ToRectangle());
            Assert.AreEqual(new Rectangle(30, 35, 30, 35), sheet.CalculateBounds(1, 1).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 35, 30, 35), sheet.CalculateBounds(2, 1).ToRectangle());
            Assert.IsNull(sheet.CalculateBounds(3, 1));

            // Row 2
            Assert.AreEqual(new Rectangle(0, 70, 30, 35), sheet.CalculateBounds(0, 2).ToRectangle());
            Assert.AreEqual(new Rectangle(30, 70, 30, 35), sheet.CalculateBounds(1, 2).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 70, 30, 35), sheet.CalculateBounds(2, 2).ToRectangle());
            Assert.IsNull(sheet.CalculateBounds(3, 2));

            // Row 3
            Assert.IsNull(sheet.CalculateBounds(0, 3));
            Assert.IsNull(sheet.CalculateBounds(1, 3));
            Assert.IsNull(sheet.CalculateBounds(2, 3));
            Assert.IsNull(sheet.CalculateBounds(3, 3));
        }

        /// <summary>Ensure sheet view can be scrolled one cell to the right with no frozen columns.</summary>
        [Test]
        public void ScrollRightWithNoFrozenColumns()
        {
            var data = Utilities.CreateTable(new string[] { "A", "B", "C", "D" },
                                      new List<object[]> { new object[] {   "a1", "b1",  "c1", "d1" },
                                                           new object[] {   "a2", "b2",  "c2", "d2" },
                                                           new object[] {   "a3", "b3",  "c3", "d3" },
                                                           new object[] {   "a4", "b4",  "c4", "d4" }});
            var units = new string[] { null, "g/m2", null, null };
            var sheet = new SheetView(new DataTableProvider(data, units), new MockTextExtents(), 70, 80, 1, 0, null);
            
            sheet.ScrollRight();

            // assumes cellpadding of 2 x 10, rowHeight = 35
            // Row 0
            Assert.IsNull(sheet.CalculateBounds(0, 0));
            Assert.AreEqual(new Rectangle(0, 0, 30, 35), sheet.CalculateBounds(1, 0).ToRectangle());
            Assert.AreEqual(new Rectangle(30, 0, 30, 35), sheet.CalculateBounds(2, 0).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 0, 30, 35), sheet.CalculateBounds(3, 0).ToRectangle());

            // Row 1
            Assert.IsNull(sheet.CalculateBounds(0, 1));
            Assert.AreEqual(new Rectangle(0, 35, 30, 35), sheet.CalculateBounds(1, 1).ToRectangle());
            Assert.AreEqual(new Rectangle(30, 35, 30, 35), sheet.CalculateBounds(2, 1).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 35, 30, 35), sheet.CalculateBounds(3, 1).ToRectangle());

            // Row 2
            Assert.IsNull(sheet.CalculateBounds(0, 2));
            Assert.AreEqual(new Rectangle(0, 70, 30, 35), sheet.CalculateBounds(1, 2).ToRectangle());
            Assert.AreEqual(new Rectangle(30, 70, 30, 35), sheet.CalculateBounds(2, 2).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 70, 30, 35), sheet.CalculateBounds(3, 2).ToRectangle());

            // Row 3
            Assert.IsNull(sheet.CalculateBounds(0, 3));
            Assert.IsNull(sheet.CalculateBounds(1, 3));
            Assert.IsNull(sheet.CalculateBounds(2, 3));
            Assert.IsNull(sheet.CalculateBounds(3, 3));
        }

        /// <summary>Ensure sheet view can be scrolled one cell to the right with one frozen columns.</summary>
        [Test]
        public void ScrollRightWithOneFrozenColumn()
        {
            var data = Utilities.CreateTable(new string[] { "A", "B", "C", "D" },
                                      new List<object[]> { new object[] {   "a1", "b1",  "c1", "d1" },
                                                           new object[] {   "a2", "b2",  "c2", "d2" },
                                                           new object[] {   "a3", "b3",  "c3", "d3" },
                                                           new object[] {   "a4", "b4",  "c4", "d4" }});
            var units = new string[] { null, "g/m2", null, null };
            var sheet = new SheetView(new DataTableProvider(data, units), new MockTextExtents(), 70, 80, 1, 1, null);

            sheet.ScrollRight();

            // assumes cellpadding of 2 x 10, rowHeight = 35
            // Row 0
            Assert.AreEqual(new Rectangle(0, 0, 30, 35), sheet.CalculateBounds(0, 0).ToRectangle());
            Assert.IsNull(sheet.CalculateBounds(1, 0));  // hidden column
            Assert.AreEqual(new Rectangle(30, 0, 30, 35), sheet.CalculateBounds(2, 0).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 0, 30, 35), sheet.CalculateBounds(3, 0).ToRectangle());

            // Row 1
            Assert.AreEqual(new Rectangle(0, 35, 30, 35), sheet.CalculateBounds(0, 1).ToRectangle());
            Assert.IsNull(sheet.CalculateBounds(1, 1));  // hidden column
            Assert.AreEqual(new Rectangle(30, 35, 30, 35), sheet.CalculateBounds(2, 1).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 35, 30, 35), sheet.CalculateBounds(3, 1).ToRectangle());

            // Row 2
            Assert.AreEqual(new Rectangle(0, 70, 30, 35), sheet.CalculateBounds(0, 2).ToRectangle());
            Assert.IsNull(sheet.CalculateBounds(1, 2));  // hidden column
            Assert.AreEqual(new Rectangle(30, 70, 30, 35), sheet.CalculateBounds(2, 2).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 70, 30, 35), sheet.CalculateBounds(3, 2).ToRectangle());

            // Row 3
            Assert.IsNull(sheet.CalculateBounds(0, 3));
            Assert.IsNull(sheet.CalculateBounds(1, 3));
            Assert.IsNull(sheet.CalculateBounds(2, 3));
            Assert.IsNull(sheet.CalculateBounds(3, 3));
        }

        /// <summary>Ensure sheet view can be scrolled one cell to the left with one frozen columns.</summary>
        [Test]
        public void ScrollLeftWithOneFrozenColumn()
        {
            var data = Utilities.CreateTable(new string[] { "A", "B", "C", "D" },
                                      new List<object[]> { new object[] {   "a1", "b1",  "c1", "d1" },
                                                           new object[] {   "a2", "b2",  "c2", "d2" },
                                                           new object[] {   "a3", "b3",  "c3", "d3" },
                                                           new object[] {   "a4", "b4",  "c4", "d4" }});
            var units = new string[] { null, "g/m2", null, null };
            var sheet = new SheetView(new DataTableProvider(data, units), new MockTextExtents(), 70, 80, 1, 1, null);

            sheet.ScrollRight();
            sheet.ScrollLeft();

            // assumes cellpadding of 2 x 10, rowHeight = 35
            Assert.AreEqual(new Rectangle(0, 0, 30, 35), sheet.CalculateBounds(0, 0).ToRectangle());
            Assert.AreEqual(new Rectangle(30, 0, 30, 35), sheet.CalculateBounds(1, 0).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 0, 30, 35), sheet.CalculateBounds(2, 0).ToRectangle());
            Assert.IsNull(sheet.CalculateBounds(3, 0));

            // Row 1
            Assert.AreEqual(new Rectangle(0, 35, 30, 35), sheet.CalculateBounds(0, 1).ToRectangle());
            Assert.AreEqual(new Rectangle(30, 35, 30, 35), sheet.CalculateBounds(1, 1).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 35, 30, 35), sheet.CalculateBounds(2, 1).ToRectangle());
            Assert.IsNull(sheet.CalculateBounds(3, 1));

            // Row 2
            Assert.AreEqual(new Rectangle(0, 70, 30, 35), sheet.CalculateBounds(0, 2).ToRectangle());
            Assert.AreEqual(new Rectangle(30, 70, 30, 35), sheet.CalculateBounds(1, 2).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 70, 30, 35), sheet.CalculateBounds(2, 2).ToRectangle());
            Assert.IsNull(sheet.CalculateBounds(3, 2));

            // Row 3
            Assert.IsNull(sheet.CalculateBounds(0, 3));
            Assert.IsNull(sheet.CalculateBounds(1, 3));
            Assert.IsNull(sheet.CalculateBounds(2, 3));
            Assert.IsNull(sheet.CalculateBounds(3, 3));
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
            var sheet = new SheetView(new DataTableProvider(data, units), new MockTextExtents(), 70, 80, 1, 1, null);

            sheet.ScrollDown();

            // assumes cellpadding of 2 x 10, rowHeight = 35
            Assert.AreEqual(new Rectangle(0, 0, 30, 35), sheet.CalculateBounds(0, 0).ToRectangle());
            Assert.AreEqual(new Rectangle(30, 0, 30, 35), sheet.CalculateBounds(1, 0).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 0, 30, 35), sheet.CalculateBounds(2, 0).ToRectangle());
            Assert.IsNull(sheet.CalculateBounds(3, 0));

            // Row 1 - hidden
            Assert.IsNull(sheet.CalculateBounds(0, 1));
            Assert.IsNull(sheet.CalculateBounds(1, 1));
            Assert.IsNull(sheet.CalculateBounds(2, 1));
            Assert.IsNull(sheet.CalculateBounds(3, 1));

            // Row 2
            Assert.AreEqual(new Rectangle(0, 35, 30, 35), sheet.CalculateBounds(0, 2).ToRectangle());
            Assert.AreEqual(new Rectangle(30, 35, 30, 35), sheet.CalculateBounds(1, 2).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 35, 30, 35), sheet.CalculateBounds(2, 2).ToRectangle());
            Assert.IsNull(sheet.CalculateBounds(3, 2));


            // Row 3
            Assert.AreEqual(new Rectangle(0, 70, 30, 35), sheet.CalculateBounds(0, 3).ToRectangle());
            Assert.AreEqual(new Rectangle(30, 70, 30, 35), sheet.CalculateBounds(1, 3).ToRectangle());
            Assert.AreEqual(new Rectangle(60, 70, 30, 35), sheet.CalculateBounds(2, 3).ToRectangle());
            Assert.IsNull(sheet.CalculateBounds(3, 3));
        }
    }
}
