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

namespace UnitTests.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// Tests for <see cref="QuoteBlockRenderer"/>.
    /// </summary>
    [TestFixture]
    public class QuoteBlockRendererTests
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
        /// The <see cref="QuoteBlockRenderer"/> instance being tested.
        /// </summary>
        private QuoteBlockRenderer renderer;

        /// <summary>
        /// Sample quote which may be used by tests.
        /// </summary>
        private QuoteBlock quote;

        /// <summary>
        /// Text in the sample quote.
        /// </summary>
        private string quoteText;

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
            renderer = new QuoteBlockRenderer();

            // Setup the sample quote block.
            quote = new QuoteBlock(new QuoteBlockParser());
            quoteText = "some text";
            ParagraphBlock paragraph = new ParagraphBlock(new ParagraphBlockParser());
            paragraph.Inline = new ContainerInline().AppendChild(new LiteralInline(quoteText));
            quote.Add(paragraph);
        }

        /// <summary>
        /// Ensure that the contents of this quote are never written to
        /// an existing paragraph.
        /// </summary>
        [Test]
        public void DontWriteToExistingParagraph()
        {
            pdfBuilder.AppendText("existing paragraph", TextStyle.Normal);
            renderer.Write(pdfBuilder, quote);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Ensure that any content added to the document after the quote
        /// goes into a new paragraph.
        /// </summary>
        [Test]
        public void EnsureSubsequentAdditionsGoToNewParagraph()
        {
            renderer.Write(pdfBuilder, quote);
            pdfBuilder.AppendText("additional content", TextStyle.Normal);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Ensure children of the quote are written.
        /// </summary>
        [Test]
        public void EnsureChildrenAreWritten()
        {
            renderer.Write(pdfBuilder, quote);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph inserted = (Paragraph)document.LastSection.Elements[0];
            Assert.That(inserted.GetRawText(), Is.EqualTo(quoteText));
        }

        /// <summary>
        /// Ensure all children have the appropriate quote style applied.
        /// </summary>
        [Test]
        public void EnsureChildrenHaveQuoteStyle()
        {
            renderer.Write(pdfBuilder, quote);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph inserted = (Paragraph)document.LastSection.Elements[0];
            Assert.That(inserted.Elements.Count, Is.EqualTo(1));
            FormattedText text = (FormattedText)inserted.Elements[0];
            Assert.That(HasQuoteStyle(document.Styles[text.Style]), Is.True);
        }

        /// <summary>
        /// Ensure that any additions to the document after the quote
        /// do not have the quote style applied.
        /// </summary>
        [Test]
        public void EnsureSubsequentAdditionsDonotHaveQuoteStyle()
        {
            renderer.Write(pdfBuilder, quote);
            pdfBuilder.AppendText("suplementary addition to the doc", TextStyle.Normal);

            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
            Paragraph supplementary = (Paragraph)document.LastSection.Elements[1];
            Assert.That(supplementary.Elements.Count, Is.EqualTo(1));
            FormattedText text = (FormattedText)supplementary.Elements[0];
            Assert.That(HasQuoteStyle(document.Styles[text.Style]), Is.False);
        }

        /// <summary>
        /// Assert that the given style has quote styles applied.
        /// This is rather rudimentary to avoid certain assumptions
        /// about styling.
        /// </summary>
        /// <param name="style"></param>
        private bool HasQuoteStyle(Style style)
        {
            return style.ParagraphFormat.Borders.Left.Visible;
        }
    }
}
