using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Interop.Markdown.Renderers.Blocks;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Markdown;
using MigraDocCore.DocumentObjectModel;
using APSIM.Interop.Markdown.Renderers.Inlines;
using Moq;
using Markdig.Parsers.Inlines;
using System;
using System.Drawing;
using System.IO;
using APSIM.Services.Documentation;

namespace APSIM.Tests.Interop
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTable(int)"/> creates a table
        /// with the correct number of columns.
        /// </summary>
        [Test]
        public void TestStartTableNCols()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTable(int)"/> will throw
        /// if already editing a table. (Nested tables not implemented.)
        /// </summary>
        [Test]
        public void EnsureStartTableCanThrow()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTable(int)"/> will insert
        /// the table at the correct location - at the end of the document,
        /// after any existing content.
        /// </summary>
        [Test]
        public void TestStartTableLocation()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTable(int)"/> applies the
        /// correct style/formatting to the table.
        /// </summary>
        [Test]
        public void TestStartTableAppliesStyle()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test the width partitioning algorithm in
        /// <see cref="PdfBuilder.FinishTable()"/>.
        /// </summary>
        [Test]
        public void TestFinishTable()
        {
            // Good luck.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that any content added after calling
        /// <see cref="PdfBuilder.FinishTable()"/> will be added after the table.
        /// </summary>
        [Test]
        public void TestContentAfterTable()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTableRow(bool)"/> will
        /// create a new row in the most recent table.
        /// </summary>
        [Test]
        public void TestStartTableRow()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTableRow(bool)"/> will throw
        /// if it does not follow a call to <see cref="PdfBuilder.FinishTableCell"/>.
        /// </summary>
        [Test]
        public void EnsureStartTableRowCanThrow()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTableRow(bool)"/> will throw
        /// if the table has no columns.
        /// </summary>
        [Test]
        public void EnsureStartTableRowThrowsNoCols()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that header rows have appropriate style applied.
        /// </summary>
        [Test]
        public void TestStartHeaderRow()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that after starting a new row, any new content is added
        /// to the new row (not the old one).
        /// </summary>
        [Test]
        public void EnsureContentWrittenToNewRow()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that after calling <see cref="PdfBuilder.StartTableCell()"/>,
        /// any new content is added to the new cell.
        /// </summary>
        /// <remarks>
        /// Should consider starting 1st cell vs other cells?
        /// </remarks>
        [Test]
        public void TestStartTableCell()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartTableCell()"/> will throw
        /// if it is called serially without calling <see cref="PdfBuilder.FinishTableCell()"/>
        /// in between.
        /// </summary>
        [Test]
        public void EnsureStartTableCellCanThrow()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that any content written after calling
        /// <see cref="PdfBuilder.FinishTableCell()"/> is not written to the previous
        /// table cell.
        /// </summary>
        [Test]
        public void TestFinishtableCell()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that calling <see cref="PdfBuilder.FinishTableCell()"/> without
        /// first calling <see cref="PdfBuilder.StartTableCell()"/> will trigger
        /// an exception.
        /// </summary>
        [Test]
        public void EnsureFinishTableCellCanThrow()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendTable(System.Data.DataTable)"/>
        /// will indeed insert all data in the table.
        /// </summary>
        [Test]
        public void TestAppendTable()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendTable(System.Data.DataTable)"/>
        /// will insert the content at the end of the document.
        /// </summary>
        [Test]
        public void TestAppendTableLocation()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that any content added after a table (using 
        /// <see cref="PdfBuilder.AppendTable(System.Data.DataTable)"/>)
        /// is written after the table.
        /// </summary>
        [Test]
        public void TestAppendAfterTable()
        {
            throw new NotImplementedException();
        }
    }
}
