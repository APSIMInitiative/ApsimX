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
using System;
using OxyPlot;
using TextStyle = APSIM.Interop.Markdown.TextStyle;

namespace UnitTests.Interop.Documentation.TagRenderers
{
    /// <summary>
    /// Tests for <see cref="GraphPageTagRenderer"/>.
    /// </summary>
    /// <remarks>
    /// There is some ugly in this test fixture, mainly due to the renderer class
    /// implementation not being overly testable. We're able to work around this,
    /// but the tests here are probably pretty fragile. Would be good to have
    /// another look at this at some point.
    /// 
    /// todo: also need to mock out PdfBuilder API.
    /// </remarks>
    [TestFixture]
    public class GraphPageTagRendererTests
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
        private GraphPageTagRenderer renderer;

        /// <summary>
        /// A mock graph instance (remember, we're not testing Graph here,
        /// we're testing the renderer).
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
            mockExporter.Setup<SkiaSharp.SKImage>(e => e.Export(It.IsAny<IPlotModel>(), It.IsAny<double>(), It.IsAny<double>())).Returns(() => image);

            renderer = new GraphPageTagRenderer(mockExporter.Object);
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
        /// Ensure that all graphs are written to the document.
        /// </summary>
        /// <remarks>
        /// This test is more complicated because it doesn't make use of the
        /// objects initialised during the Setup() function, which are really
        /// geared more towards a graph page with a single graph child.
        /// </remarks>
        /// <param name="numGraphs">Number of graphs to test.</param>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EnsureAllGraphsAreWritten(int numGraphs)
        {
            List<IGraph> graphs = new List<IGraph>(numGraphs);
            Mock<IGraphExporter> mockExporter = new Mock<IGraphExporter>();
            List<SkiaSharp.SKImage> images = new List<SkiaSharp.SKImage>();
            for (int i = 0; i < numGraphs; i++)
            {
                // This is a little tricky because I want to have each graph
                // generate a unique image, so we need to mock out the graph,
                // the intermediary plot model, and the graph exporter as well.
                Mock<IGraph> mockGraph = new Mock<IGraph>();
                IGraph graph = mockGraph.Object;
                SkiaSharp.SKImage graphImage = CreateImage(i + 1);
                Mock<IPlotModel> mockModel = new Mock<IPlotModel>();
                IPlotModel graphModel = mockModel.Object;
                mockExporter.Setup<IPlotModel>(e => e.ToPlotModel(graph)).Returns(() => graphModel);
                mockExporter.Setup<SkiaSharp.SKImage>(e => e.Export(graphModel, It.IsAny<double>(), It.IsAny<double>())).Returns(() => graphImage);
                graphs.Add(graph);
                images.Add(graphImage);
            }
            GraphPage page = new GraphPage(graphs);
            renderer = new GraphPageTagRenderer(mockExporter.Object);
            renderer.Render(page, pdfBuilder);

            if (numGraphs < 1)
                // No child graphs - document should be empty.
                Assert.AreEqual(0, document.LastSection.Elements.Count);
            else
            {
                // There should be a single paragraph, containing all graphs.
                Assert.AreEqual(1, document.LastSection.Elements.Count);
                Paragraph paragraph = document.LastSection.Elements[0] as Paragraph;
                Assert.NotNull(paragraph);

                // The paragraph should contain n images.
                Assert.AreEqual(numGraphs, paragraph.Elements.Count);

                // Ensure that all images have been renderered correctly.
                for (int i = 0; i < numGraphs; i++)
                {
                    MigraDocImage actual = paragraph.Elements[i] as MigraDocImage;
                    AssertEqual(images[i], actual);
                    images[i].Dispose();
                }
                images.Clear();
            }
        }

        /// <summary>
        /// Ensure that no graphs are written to a previous paragraph (if one exists).
        /// </summary>
        [Test]
        public void EnsureGraphsAreWrittenToNewParagraph()
        {
            // Create a paragraph with some text.
            document.LastSection.AddParagraph("paragraph content");

            // Render the graph page - should not go into previous paragraph.
            GraphPage page = new GraphPage(new[] { graph });
            renderer.Render(page, pdfBuilder);

            // There should be two paragraphs.
            Assert.AreEqual(2, document.LastSection.Elements.Count);

            // Let's also double check that the image was added correctly.
            Paragraph graphsParagraph = (Paragraph)document.LastSection.Elements[1];
            MigraDocImage actual = (MigraDocImage)graphsParagraph.Elements[0];
            AssertEqual(image, actual);
        }

        /// <summary>
        /// Ensure that any content written after the graph page does not go into
        /// the same paragraph as the graphs.
        /// </summary>
        [Test]
        public void EnsureSubsequentContentGoesToNewParagraph()
        {
            // Render the graph page.
            GraphPage page = new GraphPage(new[] { graph });
            renderer.Render(page, pdfBuilder);

            // Create a paragraph with some text.
            pdfBuilder.AppendText("paragraph content", TextStyle.Normal);

            // There should be two paragraphs.
            Assert.AreEqual(2, document.LastSection.Elements.Count);

            // Let's also double check that the image was added correctly.
            Paragraph graphsParagraph = (Paragraph)document.LastSection.Elements[0];
            MigraDocImage actual = (MigraDocImage)graphsParagraph.Elements[0];
            AssertEqual(image, actual);
        }

        /// <summary>
        /// Ensure that all graphs are given the correct sizing (half page width,
        /// one-third page height).
        /// </summary>
        [Test]
        public void TestGraphSizing()
        {
            Mock<IGraphExporter> mockExporter = new Mock<IGraphExporter>();
            mockExporter.Setup(e => e.Export(It.IsAny<IPlotModel>(), It.IsAny<double>(), It.IsAny<double>())).Returns((IPlotModel plot, double width, double height) =>
            {
                // This should be 1/2 page width and 1/3 page height in px.
                // fixme: this isn't really the best way to verify this but it'll do for now.
                Assert.AreEqual(302.362204724, width, 1e-2);
                Assert.AreEqual(317.4803405921916, height, 1e-2);
                return image;
            });
            IGraphExporter exporter = mockExporter.Object;
            renderer = new GraphPageTagRenderer(exporter);

            GraphPage page = new GraphPage(new[] { graph });
            renderer.Render(page, pdfBuilder);
        }

        /// <summary>
        /// Test that certain elements on the graph are resized appropriately.
        /// </summary>
        [Test]
        public void TestGraphElementSizing()
        {
            PlotModel graphModel = new PlotModel();
            graphModel.Series.Add(new OxyPlot.Series.LineSeries());
            graphModel.Axes.Add(new OxyPlot.Axes.LinearAxis());

            Mock<IGraphExporter> mockExporter = new Mock<IGraphExporter>();
            mockExporter.Setup<IPlotModel>(e => e.ToPlotModel(graph)).Returns(() => graphModel);
            mockExporter.Setup(e => e.Export(It.IsAny<IPlotModel>(), It.IsAny<double>(), It.IsAny<double>())).Returns((IPlotModel plot, double width, double height) =>
            {
                PlotModel model = plot as PlotModel;
                Assert.AreEqual(2, ((OxyPlot.Series.LineSeries)model.Series[0]).MarkerSize);
                Assert.AreEqual(10, model.DefaultFontSize);
                Assert.AreEqual(10, model.Axes[0].FontSize);
                Assert.AreEqual(10, model.TitleFontSize);
                return image;
            });
            IGraphExporter exporter = mockExporter.Object;
            renderer = new GraphPageTagRenderer(exporter);

            GraphPage page = new GraphPage(new[] { graph });
            renderer.Render(page, pdfBuilder);
        }

        /// <summary>
        /// Ensure that graphs in a page of graphs have their legend removed.
        /// </summary>
        [Test]
        public void EnsureNoLegend()
        {
            PlotModel graphModel = new PlotModel();
            graphModel.Series.Add(new OxyPlot.Series.LineSeries());
            graphModel.Axes.Add(new OxyPlot.Axes.LinearAxis());
            graphModel.Legends.Add(new OxyPlot.Legends.Legend());

            Mock<IGraphExporter> mockExporter = new Mock<IGraphExporter>();
            mockExporter.Setup<IPlotModel>(e => e.ToPlotModel(graph)).Returns(() => graphModel);
            mockExporter.Setup(e => e.Export(It.IsAny<IPlotModel>(), It.IsAny<double>(), It.IsAny<double>())).Returns((IPlotModel plot, double width, double height) =>
            {
                PlotModel model = plot as PlotModel;
                Assert.AreEqual(0, model.Legends.Count);
                return image;
            });
            IGraphExporter exporter = mockExporter.Object;
            renderer = new GraphPageTagRenderer(exporter);

            GraphPage page = new GraphPage(new[] { graph });
            renderer.Render(page, pdfBuilder);

            // (Sanity check, just to make sure that the above plumbing actually worked.)
            Assert.AreEqual(1, TestContext.CurrentContext.AssertCount);
        }

        /// <summary>
        /// Get a square image with the specified size.
        /// </summary>
        /// <param name="i">Image size (height and width) in px.</param>
        private SkiaSharp.SKImage CreateImage(int i)
        {
            return SkiaSharp.SKImage.Create(new SkiaSharp.SKImageInfo(i, i));
        }

        /// <summary>
        /// Ensure that a System.Drawing.Image and a MigraDoc image are equivalent.
        /// </summary>
        /// <param name="expected">Expected image.</param>
        /// <param name="actual">Actual image.</param>
        private void AssertEqual(SkiaSharp.SKImage expected, MigraDocImage actual)
        {
            if (expected == null)
                Assert.Null(actual);
            else
                Assert.NotNull(actual);

            // Note: actual.Width is not the actual width (that would be too easy);
            // instead, it represents a custom user-settable width. We're more
            // interested in the width of the underlying image.
            Assert.AreEqual(expected.Width, actual.Source.Width);
            Assert.AreEqual(expected.Height, actual.Source.Height);
        }
    }
}
