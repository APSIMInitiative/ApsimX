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
using System;

namespace UnitTests.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// Tests for <see cref="LineBreakInlineRenderer"/>.
    /// </summary>
    [TestFixture]
    public class LineBreakInlineRendererTests
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
        /// The <see cref="LineBreakInlineRenderer"/> instance being tested.
        /// </summary>
        private LineBreakInlineRenderer renderer;

        /// <summary>
        /// Sample line break inline which may be used by tests.
        /// </summary>
        private LineBreakInline inline;

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
            renderer = new LineBreakInlineRenderer();
            inline = new LineBreakInline();
            inline.IsHard = true;
        }

        /// <summary>
        /// Ensure that a newline character is inserted if the linebreak
        /// is a hard line break.
        /// </summary>
        [Test]
        public void EnsureLinefeedInserted()
        {
            Mock<PdfBuilder> builder = new Mock<PdfBuilder>(document, PdfOptions.Default);
            builder.Setup(b => b.AppendText(It.IsAny<string>(), It.IsAny<TextStyle>()))
                   .Callback<string, TextStyle>((text, _) => Assert.That(text, Is.EqualTo(Environment.NewLine)));
            inline.IsHard = true;
            renderer.Write(builder.Object, inline);
            Assert.That(TestContext.CurrentContext.AssertCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Ensure that soft line breaks are rendered as a space character.
        /// </summary>
        [Test]
        public void TestSoftLineBreak()
        {
            Mock<PdfBuilder> builder = new Mock<PdfBuilder>(document, PdfOptions.Default);
            builder.Setup(b => b.AppendText(It.IsAny<string>(), It.IsAny<TextStyle>()))
                   .Callback<string, TextStyle>((text, __) => Assert.That(text, Is.EqualTo(" ")));
            inline.IsHard = false;
            renderer.Write(builder.Object, inline);
            Assert.That(TestContext.CurrentContext.AssertCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Ensure that the inserted line break object has no style.
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
        /// Ensure that subsequent additions are not included in the newline
        /// text element.
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
        /// Ensure that the newline is written to an existing paragraph.
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
