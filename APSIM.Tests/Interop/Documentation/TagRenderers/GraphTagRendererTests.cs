using System;
using NUnit.Framework;
using System.Linq;
using System.Text;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Services.Documentation;
using System.Collections.Generic;
using APSIM.Interop.Documentation.Renderers;
using TextStyle = APSIM.Interop.Markdown.TextStyle;
using Document = MigraDocCore.DocumentObjectModel.Document;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using Section = APSIM.Services.Documentation.Section;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Services.Graphing;
using Image = System.Drawing.Image;
using APSIM.Interop.Graphing;
using Moq;
using System.Drawing;
using APSIM.Shared.Utilities;

namespace APSIM.Tests.Interop.Documentation.TagRenderers
{
    /// <summary>
    /// Tests for <see cref="GraphTagRenderer"/>.
    /// </summary>
    [TestFixture]
    public class GraphTagRendererTests
    {
        private PdfBuilder pdfBuilder;
        private Document document;
        private GraphTagRenderer renderer;
        private IGraph graph;
        private static Image image;

        [SetUp]
        public void SetUp()
        {
            document = new MigraDocCore.DocumentObjectModel.Document();
            pdfBuilder = new PdfBuilder(document, PdfOptions.Default);
            pdfBuilder.UseTagRenderer(new MockTagRenderer());
            image = new Bitmap(4, 4); // fixme

            Mock<IGraph> mockGraph = new Mock<IGraph>();
            graph = mockGraph.Object;

            // Mock graph exporter - this will just return the image field of this class.
            Mock<IGraphExporter> mockExporter = new Mock<IGraphExporter>();
            mockExporter.Setup<Image>(e => e.Export(It.IsAny<IGraph>(), It.IsAny<double>(), It.IsAny<double>())).Returns(() => image);

            renderer = new GraphTagRenderer(mockExporter.Object);
        }

        [TearDown]
        public void TearDown()
        {
            image.Dispose();
        }

        [Test]
        public void EnsureGraphImageIsAdded()
        {
            renderer.Render(graph, pdfBuilder);

            List<Paragraph> paragraphs = document.LastSection.Elements.OfType<Paragraph>().ToList();
            Assert.AreEqual(1, paragraphs.Count);
            var actual = paragraphs[0].Elements.OfType<MigraDocCore.DocumentObjectModel.Shapes.Image>().First();
            Assert.NotNull(actual);

            // Note: actual.Width is not the actual width in this case (that would be too easy);
            // instead, it represents a custom user-settable width. We're more interested in the
            // width of the underlying image.
            Assert.AreEqual(image.Width, actual.Source.Width);
            Assert.AreEqual(image.Height, actual.Source.Height);
        }
    }
}
