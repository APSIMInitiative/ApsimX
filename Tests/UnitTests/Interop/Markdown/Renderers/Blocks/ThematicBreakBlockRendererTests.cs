using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Interop.Markdown.Renderers.Blocks;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Markdown;
using MigraDocCore.DocumentObjectModel;
using Markdig.Parsers;
using Moq;

namespace UnitTests.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// Tests for <see cref="ThematicBreakBlockRenderer"/>.
    /// </summary>
    [TestFixture]
    public class ThematicBreakBlockRendererTests
    {
        /// <summary>
        /// PDF Builder API instance.
        /// </summary>
        private PdfBuilder pdfBuilder;

        /// <summary>
        /// MigraDoc document to which the renderer will write.
        /// </summary>
        private Document document;

        /// <summary>
        /// The <see cref="ThematicBreakBlockRenderer"/> instance being tested.
        /// </summary>
        private ThematicBreakBlockRenderer renderer;

        /// <summary>
        /// Sample thematic break which may be used by tests.
        /// </summary>
        private ThematicBreakBlock hr;
        /// <summary>
        /// Initialise the testing environment.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            document = new Document();
            // Workaround for a quirk in the migradoc API.
            _ = document.AddSection().Elements;
            pdfBuilder = new PdfBuilder(document, PdfOptions.Default);
            renderer = new ThematicBreakBlockRenderer();
            hr = new ThematicBreakBlock(new ThematicBreakParser());
        }

        /// <summary>
        /// Ensure the that thematic breaks generate a horizontal rule.
        /// </summary>
        [Test]
        public void EnsureHRIsWritten()
        {
            Mock<PdfBuilder> mockBuilder = new Mock<PdfBuilder>(document, PdfOptions.Default);
            bool hasHR = false;
            mockBuilder.Setup(b => b.AppendHorizontalRule()).Callback(() => hasHR = true);
            renderer.Write(mockBuilder.Object, hr);
            Assert.That(hasHR, Is.True);
        }
    }
}
