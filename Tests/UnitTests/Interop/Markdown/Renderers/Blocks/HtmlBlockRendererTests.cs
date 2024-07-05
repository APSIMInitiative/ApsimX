using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;
using APSIM.Interop.Documentation.Renderers;
using Document = MigraDocCore.DocumentObjectModel.Document;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using FormattedText = MigraDocCore.DocumentObjectModel.FormattedText;
using APSIM.Interop.Markdown.Renderers.Blocks;
using Markdig.Syntax;
using Moq;
using System;
using Markdig.Parsers;
using Markdig.Syntax.Inlines;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Markdown;
using Markdig.Helpers;

namespace UnitTests.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// Tests for <see cref="HtmlBlockRenderer"/>.
    /// </summary>
    /// <remarks>
    /// I'm not 100% sure what a html block renderer class should do,
    /// or even whether we actually need it. From what I can tell
    /// from the markdig docs, we *should* have it. For now, I've
    /// just got a very simple test to see if it does something vaguely
    /// sensible.
    /// </remarks>
    [TestFixture]
    public class HtmlBlockRendererTests
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
        /// The <see cref="HtmlBlockRenderer"/> instance being tested.
        /// </summary>
        private HtmlBlockRenderer renderer;

        /// <summary>
        /// Initialise the testing environment.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            document = new MigraDocCore.DocumentObjectModel.Document();
            // Workaround for a quirk in the migradoc API.
            _ = document.AddSection().Elements;
            pdfBuilder = new PdfBuilder(document, PdfOptions.Default);
            renderer = new HtmlBlockRenderer();
        }

        /// <summary>
        /// Ensure that children are written to the document.
        /// </summary>
        [Test]
        public void EnsureChildrenAreWritten()
        {
            string text = "some text";
            HtmlBlock block = new HtmlBlock(new HtmlBlockParser())
            {
                Inline = new ContainerInline().AppendChild(new LiteralInline(text))
            };
            renderer.Write(pdfBuilder, block);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Assert.That(((Paragraph)document.LastSection.Elements[0]).GetRawText(), Is.EqualTo(text));
        }
    }
}
