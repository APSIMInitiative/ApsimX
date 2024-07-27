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
    /// Tests for <see cref="EmphasisInlineRenderer"/>.
    /// </summary>
    [TestFixture]
    public class EmphasisInlineRendererTests
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
        /// The <see cref="EmphasisInlineRenderer"/> instance being tested.
        /// </summary>
        private EmphasisInlineRenderer renderer;

        /// <summary>
        /// Sample emphasis inline which may be used by tests.
        /// </summary>
        private EmphasisInline inline;

        /// <summary>
        /// Text in the sample emphasis inline.
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
            renderer = new EmphasisInlineRenderer();
            sampleText = "sample emphasis";
            inline = new EmphasisInline();
            inline.DelimiterChar = '*';
            inline.DelimiterCount = 1;
            inline.AppendChild(new LiteralInline(sampleText));
        }

        /// <summary>
        /// Ensure that the  text is inserted.
        /// </summary>
        [Test]
        public void EnsureChildrenAreInserted()
        {
            renderer.Write(pdfBuilder, inline);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(1));
            FormattedText text = (FormattedText)paragraph.Elements[0];
            Assert.That(text.Elements.Count, Is.EqualTo(1));
            Text rawText = (Text)text.Elements[0];
            Assert.That(rawText.Content, Is.EqualTo(sampleText));
        }

        /// <summary>
        /// Ensure that inserted text has appropriate style applied.
        /// </summary>
        [TestCase('^', 1, TextStyle.Superscript)]
        [TestCase('^', 2000, TextStyle.Superscript)]
        [TestCase('~', 1, TextStyle.Subscript)]
        [TestCase('~', 2, TextStyle.Strikethrough)]
        [TestCase('~', 24, TextStyle.Strikethrough)]
        [TestCase('*', 1, TextStyle.Italic)]
        [TestCase('*', 2, TextStyle.Strong)]
        [TestCase('*', 333, TextStyle.Strong)]
        public void EnsureEmphasisStyleIsApplied(char delimiter, int count, TextStyle expected)
        {
            Mock<PdfBuilder> mockBuilder = new Mock<PdfBuilder>(document, PdfOptions.Default);
            mockBuilder.Setup(b => b.PushStyle(It.IsAny<TextStyle>()))
                       .Callback<TextStyle>(style => Assert.That(style, Is.EqualTo(expected)))
                       .CallBase();
            inline.DelimiterChar = delimiter;
            inline.DelimiterCount = count;
            renderer.Write(mockBuilder.Object, inline);
        }

        /// <summary>
        /// Ensure that the emphasis style is applied to all children.
        /// </summary>
        [Test]
        public void EnsureStyleAppliedToAllChildren()
        {
            inline.AppendChild(new LiteralInline("second child"));
            EnsureEmphasisStyleIsApplied('*', 1, TextStyle.Italic);
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
        /// Ensure that the emphasis style is not applied to subsequent additions.
        /// </summary>
        [Test]
        public void EnsureStyleNotAppliedToSubsequentAdditions()
        {
            renderer.Write(pdfBuilder, inline);
            pdfBuilder.AppendText("this was inserted after the emphasis inline", TextStyle.Normal);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Ensure that the emphasis contents are written to an existing
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
