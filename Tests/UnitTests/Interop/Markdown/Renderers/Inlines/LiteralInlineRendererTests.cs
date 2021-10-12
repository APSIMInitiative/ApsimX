using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using Markdig.Syntax.Inlines;
using APSIM.Interop.Markdown;
using MigraDocCore.DocumentObjectModel;
using APSIM.Interop.Markdown.Renderers.Inlines;
using Moq;

namespace UnitTests.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// Tests for <see cref="LiteralInlineRenderer"/>.
    /// </summary>
    [TestFixture]
    public class LiteralInlineRendererTests
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
        /// The <see cref="LiteralInlineRenderer"/> instance being tested.
        /// </summary>
        private LiteralInlineRenderer renderer;

        /// <summary>
        /// Sample literal inline which may be used by tests.
        /// </summary>
        private LiteralInline inline;

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
            renderer = new LiteralInlineRenderer();
            inline = new LiteralInline("sample literal");
        }

        /// <summary>
        /// Ensure that the inline content is written to an existing paragraph
        /// (if one exists).
        /// </summary>
        [Test]
        public void EnsureContentGoesInExistingParagraph()
        {
            pdfBuilder.AppendText("some previous content in this paragraph", TextStyle.Normal);
            renderer.Write(pdfBuilder, inline);
            Assert.AreEqual(1, document.LastSection.Elements.Count);
        }

        /// <summary>
        /// Ensure that any content added after a literal inline will go into
        /// the same paragraph as the literal inline.
        /// </summary>
        [Test]
        public void EnsureSubsequentContentInSameParagraph()
        {
            renderer.Write(pdfBuilder, inline);
            pdfBuilder.AppendText("content added after the inline", TextStyle.Normal);
            Assert.AreEqual(1, document.LastSection.Elements.Count);
        }

        /// <summary>
        /// Ensure that the literal inline's content is written to the document.
        /// </summary>
        [Test]
        public void EnsureContentIsWritten()
        {
            renderer.Write(pdfBuilder, inline);
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.AreEqual(1, paragraph.Elements.Count);
            FormattedText formatted = (FormattedText)paragraph.Elements[0];
            Assert.AreEqual(1, formatted.Elements.Count);
            Text plainText = (Text)formatted.Elements[0];
            Assert.AreEqual(inline.Content.ToString(), plainText.Content);
        }

        /// <summary>
        /// Ensure that no style is applied to the literal inline content.
        /// </summary>
        [Test]
        public void EnsureNoStyle()
        {
            Mock<PdfBuilder> buidler = new Mock<PdfBuilder>(document, PdfOptions.Default);
            buidler.Setup(b => b.AppendText(It.IsAny<string>(), It.IsAny<TextStyle>()))
                   .Callback<string, TextStyle>((_, style) => Assert.AreEqual(TextStyle.Normal, style));
            renderer.Write(buidler.Object, inline);
        }
    }
}
