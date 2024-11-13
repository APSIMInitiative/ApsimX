using NUnit.Framework;
using System.Linq;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;
using System.Collections.Generic;
using APSIM.Interop.Documentation.Renderers;
using Document = MigraDocCore.DocumentObjectModel.Document;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using Image = System.Drawing.Image;
using MigraDocImage = MigraDocCore.DocumentObjectModel.Shapes.Image;
using APSIM.Interop.Graphing;
using Moq;
using System.Drawing;

namespace UnitTests.Interop.Documentation.TagRenderers
{
    /// <summary>
    /// Tests for <see cref="GraphTagRenderer"/>.
    /// </summary>
    /// <remarks>
    /// todo: mock out PdfBuilder API.
    /// </remarks>
    [TestFixture]
    public class GraphTagRendererTests
    {
        /// <summary>
        /// A pdf builder instance.
        /// </summary>
        /// <remarks>
        /// As with all of these tag renderer test classes, this API really
        /// ought to be mocked out.
        /// </remarks>
        private PdfBuilder pdfBuilder;

        /// <summary>
        /// Pdf document which will be modified by the pdfBuilder.
        /// </summary>
        private Document document;

        /// <summary>
        /// The graph tag renderer being tested.
        /// </summary>
        private GraphTagRenderer renderer;

        /// <summary>
        /// A mock graph instance (remember, we're not testing Graph here,
        /// we're testing the graph renderer).
        /// </summary>
        private IGraph graph;

        /// <summary>
        /// Our mock graph will use this image as its generated graph.
        /// </summary>
        private static SkiaSharp.SKImage image;

        [SetUp]
        public void SetUp()
        {
            document = new MigraDocCore.DocumentObjectModel.Document();
            // Workaround for a quirk in the migradoc API.
            _ = document.AddSection().Elements;
            pdfBuilder = new PdfBuilder(document, PdfOptions.Default);
            pdfBuilder.UseTagRenderer(new MockTagRenderer());
            image = SkiaSharp.SKImage.Create(new SkiaSharp.SKImageInfo(4, 4));

            Mock<IGraph> mockGraph = new Mock<IGraph>();
            graph = mockGraph.Object;

            // Mock graph exporter - this will just return the image field of this class.
            Mock<IGraphExporter> mockExporter = new Mock<IGraphExporter>();
            mockExporter.Setup<SkiaSharp.SKImage>(e => e.Export(It.IsAny<IGraph>(), It.IsAny<double>(), It.IsAny<double>())).Returns(() => image);
            mockExporter.Setup<SkiaSharp.SKImage>(e => e.Export(It.IsAny<OxyPlot.IPlotModel>(), It.IsAny<double>(), It.IsAny<double>())).Returns(() => image);

            renderer = new GraphTagRenderer(mockExporter.Object);
        }

        /// <summary>
        /// Dispose of the created image.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            image.Dispose();
        }

        /// <summary>
        /// Ensure that GraphTagRenderer writes the graph image to the PDF document.
        /// </summary>
        [Test]
        public void EnsureGraphImageIsAdded()
        {
            renderer.Render(graph, pdfBuilder);

            List<Paragraph> paragraphs = document.LastSection.Elements.OfType<Paragraph>().ToList();
            Assert.That(paragraphs.Count, Is.EqualTo(1));
            MigraDocImage actual = paragraphs[0].Elements.OfType<MigraDocImage>().First();
            AssertEqual(image, actual);
        }

        /// <summary>
        /// Ensure that the graph image is not written to a previous paragraph
        /// (if one exists).
        /// </summary>
        [Test]
        public void EnsureImageIsAddedToNewParagraph()
        {
            // Write a non-empty paragraph to the document.
            document.LastSection.AddParagraph("paragraph text");

            // Write the graph.
            renderer.Render(graph, pdfBuilder);

            // Ensure that the image was not written to the paragraph.
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[1];
            MigraDocImage actual = paragraph.Elements[0] as MigraDocImage;
            AssertEqual(image, actual);
        }

        /// <summary>
        /// Ensure that content written after the graph image is not added to the
        /// same paragraph as the graph image.
        /// </summary>
        [Test]
        public void EnsureLaterContentIsInNewParagraph()
        {
            // Write the graph.
            renderer.Render(graph, pdfBuilder);

            // Write a non-empty paragraph to the document.
            document.LastSection.AddParagraph("paragraph text");

            // Ensure that the image was not written to the paragraph.
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            MigraDocImage actual = paragraph.Elements[0] as MigraDocImage;
            AssertEqual(image, actual);
        }

        /// <summary>
        /// This test ensures that the graph tag renderer exports an image at an
        /// appropriate size.
        /// </summary>
        [Test]
        public void TestExportedSize()
        {
            Mock<IGraphExporter> exporter = new Mock<IGraphExporter>();
            exporter.Setup<SkiaSharp.SKImage>(e => e.Export(It.IsAny<OxyPlot.IPlotModel>(), It.IsAny<double>(), It.IsAny<double>())).Returns<IGraph, double, double>((graph, width, height) =>
            {
                // This should be the page width in px, with height calculated from an aspect ratio of 16:9.
                // fixme: this isn't really the best way to verify this but it'll do for now.
                Assert.That(width, Is.EqualTo(604.7244094488187).Within(1e-2));
                Assert.That(height, Is.EqualTo(340.15748031496054).Within(1e-2));
                return image;
            });

            renderer = new GraphTagRenderer(exporter.Object);
            renderer.Render(graph, pdfBuilder);
            Assert.That(TestContext.CurrentContext.AssertCount, Is.EqualTo(2));
        }

        /// <summary>
        /// Ensure that a System.Drawing.Image and a MigraDoc image are equivalent.
        /// </summary>
        /// <param name="expected">Expected image.</param>
        /// <param name="actual">Actual image.</param>
        private void AssertEqual(SkiaSharp.SKImage expected, MigraDocImage actual)
        {
            if (expected == null)
                Assert.That(actual, Is.Null);
            else
                Assert.That(actual, Is.Not.Null);

            // Note: actual.Width is not the actual width (that would be too easy);
            // instead, it represents a custom user-settable width. We're more
            // interested in the width of the underlying image.
            Assert.That(actual.Source.Width, Is.EqualTo(expected.Width));
            Assert.That(actual.Source.Height, Is.EqualTo(expected.Height));
        }
    }
}
