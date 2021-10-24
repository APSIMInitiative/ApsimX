using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using Markdig.Syntax.Inlines;
using APSIM.Interop.Markdown;
using MigraDocCore.DocumentObjectModel;
using APSIM.Interop.Markdown.Renderers.Inlines;
using Moq;
using Markdig.Helpers;

namespace UnitTests.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// Tests for <see cref="HtmlEntityInlineRenderer"/>.
    /// </summary>
    [TestFixture]
    public class HtmlEntityInlineRendererTests
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
        /// The <see cref="HtmlEntityInlineRenderer"/> instance being tested.
        /// </summary>
        private HtmlEntityInlineRenderer renderer;

        /// <summary>
        /// Sample html entity inline which may be used by tests.
        /// </summary>
        private HtmlEntityInline inline;

        /// <summary>
        /// Text in the sample html entity inline.
        /// </summary>
        private string sampleText;

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
            renderer = new HtmlEntityInlineRenderer();
            sampleText = "sample html entity";
            inline = new HtmlEntityInline();
            inline.Transcoded = new StringSlice(sampleText);
        }

        /// <summary>
        /// Ensure that the code text is inserted.
        /// </summary>
        [Test]
        public void EnsureEntityTextIsInserted()
        {
            renderer.Write(pdfBuilder, inline);
            Assert.AreEqual(1, document.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.AreEqual(1, paragraph.Elements.Count);
            FormattedText text = (FormattedText)paragraph.Elements[0];
            Assert.AreEqual(1, text.Elements.Count);
            Text rawText = (Text)text.Elements[0];
            Assert.AreEqual(sampleText, rawText.Content);
        }

        /// <summary>
        /// Ensure that the inserted entity has no style.
        /// </summary>
        [Test]
        public void EnsureNoStyle()
        {
            Mock<PdfBuilder> mockBuilder = new Mock<PdfBuilder>(document, PdfOptions.Default);
            mockBuilder.Setup(b => b.AppendText(It.IsAny<string>(), It.IsAny<TextStyle>()))
                       .Callback<string, TextStyle>((_, style) => Assert.AreEqual(TextStyle.Normal, style))
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
            Assert.AreEqual(1, document.LastSection.Elements.Count);
        }

        /// <summary>
        /// Ensure that subsequent additions are not included in the text element.
        /// </summary>
        [Test]
        public void EnsureSubsequentAdditionsNotInSameElement()
        {
            renderer.Write(pdfBuilder, inline);
            pdfBuilder.AppendText("this was inserted after the code inline", TextStyle.Normal);
            Assert.AreEqual(1, document.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.AreEqual(2, paragraph.Elements.Count);
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
            Assert.AreEqual(1, document.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.AreEqual(2, paragraph.Elements.Count);
        }
    }
}
