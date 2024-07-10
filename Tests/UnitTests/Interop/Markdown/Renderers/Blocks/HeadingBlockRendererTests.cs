using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using Document = MigraDocCore.DocumentObjectModel.Document;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using FormattedText = MigraDocCore.DocumentObjectModel.FormattedText;
using APSIM.Interop.Markdown.Renderers.Blocks;
using Markdig.Syntax;
using System;
using Markdig.Parsers;
using Markdig.Syntax.Inlines;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Markdown;
using APSIM.Interop.Markdown.Renderers.Inlines;

namespace UnitTests.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// Tests for <see cref="HeadingBlockRenderer"/>.
    /// </summary>
    [TestFixture]
    public class HeadingBlockRendererTests
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
        /// The <see cref="HeadingBlockRenderer"/> instance being tested.
        /// </summary>
        private HeadingBlockRenderer renderer;

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
            renderer = new HeadingBlockRenderer();
        }

        /// <summary>
        /// Ensure that an empty heading block is not written to the document.
        /// </summary>
        [Test]
        public void EnsureEmptyHeadingNotWritten()
        {
            HeadingBlock block = new HeadingBlock(new HeadingBlockParser()) { Level = 1, Inline = new ContainerInline() };
            renderer.Write(pdfBuilder, block);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Ensure that passing a heading block with a negative heading level into
        /// the HeadingBlockRenderer will trigger an exception.
        /// </summary>
        [Test]
        public void EnsureNegativeHeadingLevelThrows()
        {
            HeadingBlock block = CreateHeading("heading", -1);
            Assert.Throws<InvalidOperationException>(() => renderer.Write(pdfBuilder, block));
        }

        /// <summary>
        /// Ensure that all children of the heading are written as part of the heading.
        /// </summary>
        /// <remarks>
        /// This test relies on <see cref="LiteralInlineRenderer"/>. It would be better
        /// to mock out this dependency.
        /// </remarks>
        [Test]
        public void EnsureChildrenIsWritten()
        {
            string text = "heading text";
            renderer.Write(pdfBuilder, CreateHeading(text));
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.That(paragraph.GetRawText(), Is.EqualTo($"1 {text}"));
        }

        /// <summary>
        /// Ensure that heading text is not added to the previous paragraph of text.
        /// </summary>
        [Test]
        public void EnsureHeadingGoesIntoNewParagraph()
        {
            // Append text to the document - this will cause a new paragraph to be created.
            pdfBuilder.AppendText("some paragraph", TextStyle.Normal);

            // Write the heading block.
            renderer.Write(pdfBuilder, CreateHeading("contents"));

            // There should be two paragraphs in the document.
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Ensure that any content added after the heading does not go into the same
        /// paragraph as the heading.
        /// </summary>
        [Test]
        public void EnsureSubsequentContentGoesIntoNewParagraph()
        {
            // Write a heaing block.
            renderer.Write(pdfBuilder, CreateHeading("a heading"));

            // Write some text - should go in a new paragraph.
            pdfBuilder.AppendText("this should be in a new paragraph", TextStyle.Normal);

            // There should be two paragarphs in the document.
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Ensure that the headings are added with the correct heading level.
        /// </summary>
        [Test]
        public void EnsureHeadingLevelIsRespected()
        {
            renderer.Write(pdfBuilder, CreateHeading("heading level 1", 1));
            renderer.Write(pdfBuilder, CreateHeading("heading level 2", 2));

            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));

            Paragraph paragraph0 = (Paragraph)document.LastSection.Elements[0];
            Paragraph paragraph1 = (Paragraph)document.LastSection.Elements[1];

            FormattedText text0 = (FormattedText)paragraph0.Elements[0];
            FormattedText text1 = (FormattedText)paragraph1.Elements[0];

            double fontSize0 = document.Styles[text0.Style].Font.Size.Point;
            double fontSize1 = document.Styles[text1.Style].Font.Size.Point;

            // heading0 is heading level 1, so should haved larger font size than
            // heading1, which is a level 2 heading.
            Assert.That(fontSize0, Is.GreaterThan(fontSize1));
        }

        /// <summary>
        /// Ensure that any content added to the document after the heading block
        /// doesn't retain the style applied to the heading block.
        /// </summary>
        [Test]
        public void EnsureHeadingStyleNotAppliedToSubsequentInsertions()
        {
            renderer.Write(pdfBuilder, CreateHeading("sample heading"));
            pdfBuilder.AppendText("a new paragraph", TextStyle.Normal);

            Paragraph headingParagraph = (Paragraph)document.LastSection.Elements[0];
            FormattedText headingText = (FormattedText)headingParagraph.Elements[0];
            double headingTextSize = document.Styles[headingText.Style].Font.Size.Point;

            Paragraph plainParagraph = (Paragraph)document.LastSection.Elements[1];
            FormattedText plainText = (FormattedText)plainParagraph.Elements[0];
            double plainTextSize = document.Styles[plainText.Style].Font.Size.Point;

            Assert.That(headingTextSize, Is.GreaterThan(plainTextSize));
        }

        /// <summary>
        /// Create a HeadingBlock object which contains the specified plaintext.
        /// </summary>
        /// <param name="text">Text to go into the heading.</param>
        private HeadingBlock CreateHeading(string text)
        {
            return CreateHeading(text, 1);
        }

        /// <summary>
        /// Create a HeadingBlock object which contains the specified plaintext.
        /// </summary>
        /// <param name="text">Text to go into the heading.</param>
        /// <param name="headingLevel">Heading level.</param>
        private HeadingBlock CreateHeading(string text, int headingLevel)
        {
            return new HeadingBlock(new HeadingBlockParser())
            {
                HeaderChar = '#',
                Level = headingLevel,
                Inline = new ContainerInline().AppendChild(new LiteralInline(text))
            };
        }
    }
}