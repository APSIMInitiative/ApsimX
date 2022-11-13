using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Markdown;
using MigraDocCore.DocumentObjectModel;
using System;
using MigraDocCore.DocumentObjectModel.Tables;
using System.Data;

namespace UnitTests.Interop
{
    [TestFixture]
    public class PdfBuilderTableTests
    {
        /// <summary>
        /// The pdf builder instance used for testing.
        /// </summary>
        private PdfBuilder builder;

        /// <summary>
        /// This is the MigraDoc document which will be modified by
        /// <see cref="builder"/>.
        /// </summary>
        private Document doc;

        /// <summary>
        /// Initialise the PDF buidler and its document.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            doc = new Document();
            builder = new PdfBuilder(doc, PdfOptions.Default);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTable(int)"/> does indeed
        /// start a new table.
        /// </summary>
        [Test]
        public void TestStartTable()
        {
            builder.StartTable(2);
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Assert.AreEqual(typeof(Table), doc.LastSection.Elements[0].GetType());
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTable(int)"/> creates a table
        /// with the correct number of columns.
        /// </summary>
        [TestCase(0)]
        [TestCase(456)]
        public void TestStartTableNCols(int numColumns)
        {
            builder.StartTable(numColumns);
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Table table = (Table)doc.LastSection.Elements[0];
            Assert.AreEqual(numColumns, table.Columns.Count);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTable(int)"/> will throw
        /// if already editing a table. (Nested tables not implemented.)
        /// </summary>
        [Test]
        public void EnsureStartTableCanThrow()
        {
            builder.StartTableCell();
            Assert.Throws<NotImplementedException>(() => builder.StartTable(0));
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTable(int)"/> will insert
        /// the table at the correct location - at the end of the document,
        /// after any existing content.
        /// </summary>
        [Test]
        public void TestStartTableLocation()
        {
            doc.AddSection().AddParagraph("content before the table.");
            builder.StartTable(1);
            Assert.AreEqual(2, doc.LastSection.Elements.Count);
            Assert.AreEqual(typeof(Paragraph), doc.LastSection.Elements[0].GetType());
            Assert.AreEqual(typeof(Table), doc.LastSection.Elements[1].GetType());
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTable(int)"/> applies the
        /// correct style/formatting to the table.
        /// </summary>
        [Test]
        public void TestStartTableAppliesStyle()
        {
            builder.StartTable(1);
            Table table = (Table)doc.LastSection.Elements[0];
            Assert.True(table.KeepTogether);
            Assert.AreEqual(Colors.Black, table.Borders.Color);
            Assert.Greater(table.Borders.Width, 0);
        }

        /// <summary>
        /// Test the width partitioning algorithm in
        /// <see cref="PdfBuilder.FinishTable()"/>.
        /// </summary>
        [Test]
        public void TestTableSizePartitioning()
        {
            string longString = new string('x', 50);

            // Good luck.
            builder.StartTable(1);

            builder.StartTableRow(false);
            builder.StartTableCell();
            builder.AppendText(longString, TextStyle.Normal);
            builder.FinishTableCell();

            builder.FinishTable();

            double width = ((Table)doc.LastSection.Elements[0]).Columns[0].Width.Point;
            // fixme
            Assert.GreaterOrEqual(width, 250);
        }

        /// <summary>
        /// Ensure that any content added after calling
        /// <see cref="PdfBuilder.FinishTable()"/> will be added after the table.
        /// </summary>
        [Test]
        public void TestContentAfterTable()
        {
            builder.StartTable(1);
            builder.FinishTable();
            builder.AppendText("Text after table", TextStyle.Normal);
            Assert.AreEqual(2, doc.LastSection.Elements.Count);
            Assert.AreEqual(typeof(Table), doc.LastSection.Elements[0].GetType());
            Assert.AreEqual(typeof(Paragraph), doc.LastSection.Elements[1].GetType());
        }


        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTableRow(bool)"/> will
        /// create a new row in the table.
        /// </summary>
        [Test]
        public void TestStartTableRow()
        {
            builder.StartTable(1);

            Table table = doc.LastSection.Elements[0] as Table;
            Assert.NotNull(table);
            Assert.AreEqual(0, table.Rows.Count);

            builder.StartTableRow(false);
            Assert.AreEqual(1, table.Rows.Count);

            builder.StartTableRow(false);
            Assert.AreEqual(2, table.Rows.Count);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTableRow(bool)"/> will
        /// create a new row only in the last table in the document.
        /// </summary>
        [Test]
        public void TestStartTableRowMultipleTables()
        {
            DataTable input = CreateDataTable();
            builder.AppendTable(input);

            builder.StartTable(1);
            builder.StartTableRow(false);

            Assert.AreEqual(2, doc.LastSection.Elements.Count);
            Table table0 = (Table)doc.LastSection.Elements[0];
            Table table1 = (Table)doc.LastSection.Elements[1];

            AssertEqual(input, table0);
            Assert.AreEqual(1, table1.Rows.Count);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTableRow(bool)"/> will throw
        /// if it does not follow a call to <see cref="PdfBuilder.FinishTableCell"/>.
        /// </summary>
        [Test]
        public void EnsureStartTableRowCanThrow()
        {
            builder.StartTable(1);
            builder.StartTableCell();
            Exception error = Assert.Throws<Exception>(() => builder.StartTableRow(false));
            Assert.AreEqual(typeof(InvalidOperationException), error.InnerException.GetType());
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTableRow(bool)"/> will throw
        /// if the table has no columns.
        /// </summary>
        [Test]
        public void EnsureStartTableRowThrowsNoCols()
        {
            builder.StartTable(0);
            Exception error = Assert.Throws<Exception>(() => builder.StartTableRow(false));
            Assert.AreEqual(typeof(InvalidOperationException), error.InnerException.GetType());
        }

        /// <summary>
        /// Ensure that header rows have appropriate style applied.
        /// </summary>
        [Test]
        public void TestStartHeaderRow()
        {
            builder.StartTable(3);
            builder.StartTableRow(true);

            Table table = (Table)doc.LastSection.Elements[0];
            Row row = table.Rows[0];
            Assert.True(row.HeadingFormat);
            Assert.AreEqual(Colors.LightBlue, row.Shading.Color);
            Assert.AreEqual(ParagraphAlignment.Left, row.Format.Alignment);
            Assert.AreEqual(VerticalAlignment.Center, row.VerticalAlignment);
            Assert.True(row.Format.Font.Bold);
            for (int i = 0; i < row.Table.Columns.Count; i++)
            {
                Cell cell = row.Cells[i];
                Assert.True(cell.Format.Font.Bold);
                Assert.AreEqual(ParagraphAlignment.Left, cell.Format.Alignment);
                Assert.AreEqual(VerticalAlignment.Center, cell.VerticalAlignment);
            }
        }

        /// <summary>
        /// Ensure that after starting a new row, any new content is added
        /// to the new row (not the old one).
        /// </summary>
        [Test]
        public void EnsureContentWrittenToNewRow()
        {
            builder.StartTable(2);
            builder.StartTableRow(false);

            string text00 = "row0, cell0";
            string text10 = "row1, cell0";

            builder.StartTableCell();
            builder.AppendText(text00, TextStyle.Normal);
            builder.FinishTableCell();

            builder.StartTableRow(false);

            builder.StartTableCell();
            builder.AppendText(text10, TextStyle.Normal);
            builder.FinishTableCell();

            Table table = (Table)doc.LastSection.Elements[0];
            Assert.AreEqual(2, table.Rows.Count);

            AssertTextEqual(text00, table.Rows[0].Cells[0]);
            AssertTextEqual(null, table.Rows[0].Cells[1]);
            AssertTextEqual(text10, table.Rows[1].Cells[0]);
            AssertTextEqual(null, table.Rows[1].Cells[1]);
        }

        /// <summary>
        /// Ensure that after calling <see cref="PdfBuilder.StartTableCell()"/>,
        /// any new content is added to a table cell.
        /// </summary>
        /// <remarks>
        /// Should consider starting 1st cell vs other cells?
        /// </remarks>
        [Test]
        public void TestStartTableCell()
        {
            builder.StartTable(1);
            builder.StartTableRow(false);
            builder.StartTableCell();

            builder.AppendText("text in the cell", TextStyle.Normal);
            builder.AppendText("more cell contents", TextStyle.Normal);

            Table table = (Table)doc.LastSection.Elements[0];
            Assert.AreEqual(1, table.Rows.Count);
            Row row = table.Rows[0];
            Assert.AreEqual(1, row.Cells.Count);
            Cell cell = row.Cells[0];
            Assert.AreEqual(1, cell.Elements.Count);
            Paragraph paragraph = cell.Elements[0] as Paragraph;
            Assert.NotNull(paragraph);
            Assert.AreEqual(2, paragraph.Elements.Count);
            Assert.AreEqual(typeof(FormattedText), paragraph.Elements[0].GetType());
            Assert.AreEqual(typeof(FormattedText), paragraph.Elements[1].GetType());
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTableCell()"/> will throw
        /// if it is called serially without calling <see cref="PdfBuilder.FinishTableCell()"/>
        /// in between.
        /// </summary>
        [Test]
        public void EnsureStartTableCellCanThrow()
        {
            builder.StartTable(2);
            builder.StartTableRow(false);
            builder.StartTableCell();
            Assert.Throws<InvalidOperationException>(() => builder.StartTableCell());
        }

        /// <summary>
        /// In this test we create a table with N columns and attempt to start
        /// a new cell (N + 1) times without starting a new row, and expected an
        /// exception to be thrown.
        /// </summary>
        [Test]
        public void TestTableOverflow()
        {
            int numColumns = 1;
            builder.StartTable(numColumns);
            builder.StartTableRow(false);
            for (int i = 0; i < numColumns; i++)
            {
                builder.StartTableCell();
                builder.AppendText($"cell{i}", TextStyle.Normal);
                builder.FinishTableCell();
            }

            // At this point, we've written to all N cells in this row. If we try
            // to write to another cell without calling StartNewRow(), an exception
            // should be thrown.
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.StartTableCell();
                builder.AppendText($"cell{numColumns}", TextStyle.Normal);
            });
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTableCell()"/> starts a new
        /// cell in the last table in the document.
        /// </summary>
        [Test]
        public void EnsureStartCellAffectsLastTable()
        {
            DataTable table1 = CreateDataTable();
            builder.AppendTable(table1);
            builder.StartTable(1);
            builder.StartTableRow(false);

            builder.StartTableCell();
            builder.AppendText("text in new table", TextStyle.Normal);

            Assert.AreEqual(2, doc.LastSection.Elements.Count);
            AssertEqual(table1, (Table)doc.LastSection.Elements[0]);
            Table table2 = (Table)doc.LastSection.Elements[1];
            Assert.AreEqual(1, table2.Rows.Count);
            Assert.AreEqual(1, table2.Rows[0].Cells.Count);
        }

        /// <summary>
        /// Ensure that any content written after calling
        /// <see cref="PdfBuilder.FinishTableCell()"/> is not written to the previous
        /// table cell.
        /// </summary>
        [Test]
        public void TestFinishtableCell()
        {
            builder.StartTable(2);
            builder.StartTableRow(false);

            builder.StartTableCell();
            builder.AppendText("cell 0", TextStyle.Normal);
            builder.FinishTableCell();

            builder.StartTableCell();
            builder.AppendText("cell 1", TextStyle.Normal);
            builder.FinishTableCell();

            Table table = (Table)doc.LastSection.Elements[0];
            Assert.AreEqual(1, table.Rows.Count);
            Row row = table.Rows[0];
            Assert.AreEqual(2, row.Cells.Count);
        }

        /// <summary>
        /// Ensure that calling <see cref="PdfBuilder.FinishTableCell()"/> without
        /// first calling <see cref="PdfBuilder.StartTableCell()"/> will trigger
        /// an exception.
        /// </summary>
        [Test]
        public void EnsureFinishTableCellCanThrow()
        {
            Assert.Throws<InvalidOperationException>(() => builder.FinishTableCell());
        }

        /// <summary>
        /// Ensure that serial calls to <see cref="PdfBuilder.FinishTableCell()"/>
        /// without calling <see cref="PdfBuilder.StartTableCell()"/> in-between
        /// results in an exception.
        /// </summary>
        [Test]
        public void TestSerialFinishTableCell()
        {
            builder.StartTable(1);
            builder.StartTableRow(false);
            builder.StartTableCell();
            builder.FinishTableCell();
            Assert.Throws<InvalidOperationException>(() => builder.FinishTableCell());
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendTable(System.Data.DataTable)"/>
        /// will indeed insert all data in the table.
        /// </summary>
        [Test]
        public void TestAppendTable()
        {
            DataTable input = CreateDataTable();
            builder.AppendTable(input);
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Table table = doc.LastSection.Elements[0] as Table;
            Assert.NotNull(table);

            AssertEqual(input, table);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendTable(System.Data.DataTable)"/>
        /// will insert the content at the end of the document.
        /// </summary>
        [Test]
        public void TestAppendTableLocation()
        {
            doc.AddSection().AddParagraph("this is above the table");
            builder.AppendTable(CreateDataTable());
            Assert.AreEqual(2, doc.LastSection.Elements.Count);
            Assert.AreEqual(typeof(Paragraph), doc.LastSection.Elements[0].GetType());
            Assert.AreEqual(typeof(Table), doc.LastSection.Elements[1].GetType());
        }

        /// <summary>
        /// Ensure that any content added after a table (using 
        /// <see cref="PdfBuilder.AppendTable(System.Data.DataTable)"/>)
        /// is written after the table.
        /// </summary>
        [Test]
        public void TestAppendAfterTable()
        {
            builder.AppendTable(CreateDataTable());
            builder.AppendText("this is above the table", TextStyle.Normal);
            Assert.AreEqual(2, doc.LastSection.Elements.Count);
            Assert.AreEqual(typeof(Table), doc.LastSection.Elements[0].GetType());
            Assert.AreEqual(typeof(Paragraph), doc.LastSection.Elements[1].GetType());
        }

        /// <summary>
        /// Ensure that the given MigraDoc table is "equivalent" to the
        /// given DataTable, in that it has the correct number of rows
        /// and columns, and has the correct cell contents.
        /// </summary>
        /// <param name="expected">A DataTable.</param>
        /// <param name="actual">A MigraDoc table, which should be equivalent to the datatable.</param>
        private void AssertEqual(DataTable expected, Table actual)
        {
            // The inserted table will have 1 extra row, for the headings.
            Assert.AreEqual(expected.Rows.Count + 1, actual.Rows.Count);
            Assert.AreEqual(expected.Columns.Count, actual.Columns.Count);

            // Verify that table contents were inserted correctly.
            for (int i = 0; i < actual.Rows.Count; i++)
            {
                for (int j = 0; j < actual.Columns.Count; j++)
                {
                    object cellExpected;
                    // We expect the column names in the first row.
                    if (i == 0)
                        cellExpected = expected.Columns[j].ColumnName;
                    else
                        cellExpected = expected.Rows[i - 1][j];
                    string cellActual = ((Paragraph)actual.Rows[i].Cells[j].Elements[0]).GetRawText();
                    Assert.AreEqual(cellExpected, cellActual);
                }
            }
        }

        /// <summary>
        /// Ensure that a cell contains only the given text in a single paragraph.
        /// </summary>
        /// <param name="expected">Expected text.</param>
        /// <param name="cell">Cell text.</param>
        private void AssertTextEqual(string expected, Cell cell)
        {
            if (expected == null)
                Assert.AreEqual(0, cell.Elements.Count);
            else
            {
                Assert.AreEqual(1, cell.Elements.Count);
                Paragraph paragraph = cell.Elements[0] as Paragraph;
                Assert.NotNull(paragraph);
                Assert.AreEqual(1, paragraph.Elements.Count);
                FormattedText formatted = (FormattedText)paragraph.Elements[0];
                Assert.AreEqual(1, formatted.Elements.Count);
                Text plain = (Text)formatted.Elements[0];
                Assert.AreEqual(expected, plain.Content);
            }
        }

        /// <summary>
        /// Create a simple DataTable.
        /// </summary>
        private DataTable CreateDataTable()
        {
            DataTable result = new DataTable("A simple table");
            result.Columns.Add("t", typeof(string));
            result.Columns.Add("x", typeof(string));
            result.Rows.Add("0", "1");
            result.Rows.Add("1", "2");
            result.Rows.Add("2", "4");
            result.Rows.Add("3", "8");
            return result;
        }
    }
}
