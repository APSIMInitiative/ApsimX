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

namespace UnitTests.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// Tests for <see cref="HtmlInlineRenderer"/>.
    /// </summary>
    [TestFixture]
    public class HtmlInlineRendererTests
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
        /// The <see cref="HtmlInlineRenderer"/> instance being tested.
        /// </summary>
        private HtmlInlineRenderer renderer;

        /// <summary>
        /// Sample html inline which may be used by tests.
        /// </summary>
        private HtmlInline inline;

        /// <summary>
        /// Text in the sample html inline.
        /// </summary>
        private string sampleHtml;

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
            renderer = new HtmlInlineRenderer();
            sampleHtml = "<td>";
            inline = new HtmlInline(sampleHtml);
        }

        /// <summary>
        /// Ensure that the html inline's tag is written.
        /// </summary>
        /// <remarks>
        /// I'm not 100% sure what we want this renderer to do. For now, I'm
        /// going to ensure that this is what it does. Feel free to change.
        /// </remarks>
        [Test]
        public void EnsureTagIsWritten()
        {
            renderer.Write(pdfBuilder, inline);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(1));
            FormattedText text = (FormattedText)paragraph.Elements[0];
            Assert.That(text.Elements.Count, Is.EqualTo(1));
            Text rawText = (Text)text.Elements[0];
            Assert.That(rawText.Content, Is.EqualTo(sampleHtml));
        }

        /// <summary>
        /// Ensure that the inserted html object has no style.
        /// </summary>
        [Test]
        public void EnsureNoStyle()
        {
            Mock<PdfBuilder> mockBuilder = new Mock<PdfBuilder>(document, PdfOptions.Default);
            mockBuilder.Setup(b => b.AppendText(It.IsAny<string>(), It.IsAny<TextStyle>()))
                       .Callback<string, TextStyle>((_, style) => Assert.That(style, Is.EqualTo(TextStyle.Normal)))
                       .CallBase();
            renderer.Write(mockBuilder.Object, inline);
        }

        /// <summary>
        /// Ensure that any subsequent additions go into the same paragraph.
        /// </summary>
        [Test]
        public void EnsureSubsequentAdditionsInSameParagraph()
        {
            renderer.Write(pdfBuilder, inline);
            pdfBuilder.AppendText("subsequent addition", TextStyle.Normal);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Ensure that subsequent additions are not included in the text element.
        /// </summary>
        [Test]
        public void EnsureSubsequentAdditionsNotInSameElement()
        {
            renderer.Write(pdfBuilder, inline);
            pdfBuilder.AppendText("this was inserted after the code inline", TextStyle.Normal);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Ensure that the entity text is written to an existing
        /// paragraph (if there is one).
        /// </summary>
        [Test]
        public void EnsureContentGoesInExistingParagraph()
        {
            pdfBuilder.AppendText("pre-existing material", TextStyle.Normal);
            renderer.Write(pdfBuilder, inline);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(2));
        }
    }
}
