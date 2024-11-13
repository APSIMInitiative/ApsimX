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
    /// Tests for <see cref="CodeBlockRenderer"/>.
    /// </summary>
    [TestFixture]
    public class CodeBlockRendererTests
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
        /// The <see cref="CodeBlockRenderer"/> instance being tested.
        /// </summary>
        private CodeBlockRenderer renderer;

        /// <summary>
        /// A code block which is initialised prior to each test being run.
        /// </summary>
        private CodeBlock block;

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
            renderer = new CodeBlockRenderer();
            block = CreateCodeBlock("this is source code");
        }

        /// <summary>
        /// Ensure that an empty code block is not rendered.
        /// </summary>
        [Test]
        public void EnsureEmptyBlockIsWritten()
        {
            CodeBlock block = new FencedCodeBlock(new FencedCodeBlockParser());
            renderer.Write(pdfBuilder, block);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Ensure that a code block containing no actual text (ie all
        /// space characters) *is* written to the document.
        /// </summary>
        /// <param name="empty">Contents of the code block.</param>
        [TestCase("")]
        [TestCase("\n")]
        [TestCase(" ")]
        public void EnsureBlockWithNoTextIsWritten(string empty)
        {
            CodeBlock block = CreateCodeBlock(empty);
            renderer.Write(pdfBuilder, block);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Ensure that children of the code block are written to the document.
        /// </summary>
        [Test]
        public void EnsureChildrenAreWritten()
        {
            string text = "contents";
            CodeBlock block = CreateCodeBlock(text);
            renderer.Write(pdfBuilder, block);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.That(paragraph.GetRawText(), Is.EqualTo(text));
        }

        /// <summary>
        /// Ensure that the rendered contents of the code block are formatted
        /// correctly in the document.
        /// </summary>
        /// <remarks>
        /// Currently the only thing I'm checking is that it's monospace.
        /// </remarks>
        [Test]
        public void CheckFormatting()
        {
            renderer.Write(pdfBuilder, block);
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            FormattedText text = (FormattedText)paragraph.Elements[0];

            // todo: need to check if this is really the best way.
            Assert.That(document.Styles[text.Style].Font.Name, Is.EqualTo("courier"));
        }

        /// <summary>
        /// Ensure that any content added to the document after the code block
        /// doesn't have the code formatting applied.
        /// </summary>
        [Test]
        public void CheckFormattingOfSubsequentContent()
        {
            // Write a code block, and then some plain text.
            renderer.Write(pdfBuilder, block);
            pdfBuilder.AppendText("a new paragraph after the code block", TextStyle.Normal);

            // Ensure that the contents of the two paragraphs have different fonts.
            Paragraph codeParagraph = (Paragraph)document.LastSection.Elements[0];
            FormattedText codeText = (FormattedText)codeParagraph.Elements[0];
            Paragraph plainParagraph = (Paragraph)document.LastSection.Elements[1];
            FormattedText plainText = (FormattedText)plainParagraph.Elements[0];

            string codeFont = document.Styles[codeText.Style].Font.Name;
            string plainFont = document.Styles[plainText.Style].Font.Name;
            Assert.That(plainFont, Is.Not.EqualTo(codeFont));
        }

        /// <summary>
        /// Ensure that a code block is not written to a previous paragraph.
        /// </summary>
        [Test]
        public void EnsureCodeBlockIsWrittenToNewParagraph()
        {
            pdfBuilder.AppendText("a pre-existing, non-empty paragraph", TextStyle.Normal);
            renderer.Write(pdfBuilder, block);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Ensure that, after writing a code block, any subsequent additions to
        /// the document go into a new paragraph.
        /// </summary>
        [Test]
        public void EnsureSubsequentAdditionsGoToNewParagraph()
        {
            renderer.Write(pdfBuilder, block);
            pdfBuilder.AppendText("This should go into a new paragraph.", TextStyle.Normal);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Create a code block containing the given text contents.
        /// </summary>
        private CodeBlock CreateCodeBlock(string contents)
        {
            return new FencedCodeBlock(new FencedCodeBlockParser())
            {
                IndentCount = 4,
                FencedChar = '`',
                OpeningFencedCharCount = 3,
                Lines = new StringLineGroup(contents)
            };
        }
    }
}
