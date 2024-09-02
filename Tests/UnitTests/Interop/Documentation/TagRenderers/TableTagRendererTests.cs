using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;
using APSIM.Interop.Documentation.Renderers;
using Document = MigraDocCore.DocumentObjectModel.Document;
using MigraDocTable = MigraDocCore.DocumentObjectModel.Tables.Table;
using System.Data;

namespace UnitTests.Interop.Documentation.TagRenderers
{
    /// <summary>
    /// Tests for the <see cref="Table"/> class.
    /// </summary>
    /// <remarks>
    /// The renderer currently just calls a single method in the
    /// <see cref="PdfBuilder"/> API. This fixture is not intended
    /// to test the PDF Builder API, so I'm just going to have one
    /// simple test and leave the more detailed tests for the pdf
    /// builder test fixtures.
    /// </remarks>
    [TestFixture]
    public class TableTagRendererTests
    {
        private PdfBuilder pdfBuilder;
        private Document document;
        private TableTagRenderer renderer;

        [SetUp]
        public void SetUp()
        {
            document = new MigraDocCore.DocumentObjectModel.Document();
            pdfBuilder = new PdfBuilder(document, PdfOptions.Default);
            pdfBuilder.UseTagRenderer(new MockTagRenderer());
            renderer = new TableTagRenderer();
        }

        /// <summary>
        /// Test the simple case.
        /// </summary>
        [Test]
        public void TestSimpleCase()
        {
            DataTable data = new DataTable("title");
            data.Columns.Add("t", typeof(double));
            data.Columns.Add("x", typeof(double));
            data.Rows.Add(0, 1);
            data.Rows.Add(1, 15000);
            renderer.Render(new Table(data), pdfBuilder);

            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            MigraDocTable table = document.LastSection.Elements[0] as MigraDocTable;
            // We will just check that the inserted table looks to be the right shape.
            // Note taht the inserted table will have one extra row, in which the
            // headings are written. More detailed tests of the data can be found
            // in <see cref="PdfBuilderTableTests"/>
            Assert.That(table.Rows.Count, Is.EqualTo(data.Rows.Count + 1), "Wrong number of rows");
            Assert.That(table.Columns.Count, Is.EqualTo(data.Columns.Count), "Wrong number of columns");
        }
    }
}
