using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Interop.Markdown.Renderers.Blocks;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Markdown;
using MigraDocCore.DocumentObjectModel;

namespace UnitTests.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// Tests for <see cref="ParagraphBlockRenderer"/>.
    /// </summary>
    [TestFixture]
    public class ParagraphBlockRendererTests
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
        /// The <see cref="ParagraphBlockRenderer"/> instance being tested.
        /// </summary>
        private ParagraphBlockRenderer renderer;

        /// <summary>
        /// Sample paragraph which may be used by tests.
        /// </summary>
        private ParagraphBlock paragraph;

        /// <summary>
        /// Text in the sample paragraph.
        /// </summary>
        private string paragraphText;

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
            renderer = new ParagraphBlockRenderer();
            paragraph = new ParagraphBlock();
            paragraphText = "some text";
            paragraph.Inline = new ContainerInline().AppendChild(new LiteralInline(paragraphText));
        }

        /// <summary>
        /// Ensure that the contents of this paragraph are never written to
        /// an existing paragraph.
        /// </summary>
        [Test]
        public void DontWriteToExistingParagraph()
        {
            pdfBuilder.AppendText("existing paragraph", TextStyle.Normal);
            renderer.Write(pdfBuilder, paragraph);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Ensure that any content added to the document after the paragraph goes
        /// into a new paragraph.
        /// </summary>
        [Test]
        public void EnsureSubsequentAdditionsGoToNewParagraph()
        {
            renderer.Write(pdfBuilder, paragraph);
            pdfBuilder.AppendText("additional content", TextStyle.Normal);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Ensure children of the paragraph are written.
        /// </summary>
        [Test]
        public void EnsureChildrenAreWritten()
        {
            renderer.Write(pdfBuilder, paragraph);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph inserted = (Paragraph)document.LastSection.Elements[0];
            Assert.That(inserted.GetRawText(), Is.EqualTo(paragraphText));
        }
    }
}
