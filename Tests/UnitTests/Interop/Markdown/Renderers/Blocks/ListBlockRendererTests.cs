using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using MigraDocCore.DocumentObjectModel;
using APSIM.Interop.Markdown.Renderers.Blocks;
using Markdig.Syntax;
using Markdig.Parsers;
using Markdig.Syntax.Inlines;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Markdown;

namespace UnitTests.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// Tests for <see cref="ListBlockRenderer"/>.
    /// </summary>
    [TestFixture]
    public class ListBlockRendererTests
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
        /// The <see cref="ListBlockRenderer"/> instance being tested.
        /// </summary>
        private ListBlockRenderer renderer;

        /// <summary>
        /// A sample list block which may be used by tests which don't care
        /// much about the contents of the list block.
        /// </summary>
        private ListBlock sampleBlock;

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
            renderer = new ListBlockRenderer();
            sampleBlock = CreateListBlock('-', "a list item");
        }

        /// <summary>
        /// Ensure that the list contents don't go into an existing paragraph.
        /// </summary>
        [Test]
        public void EnsureListGoesIntoNewParagraph()
        {
            // Setup the document with an existing paragraph.
            pdfBuilder.AppendText("existing p/graph", TextStyle.Normal);

            // Now write the list block.
            renderer.Write(pdfBuilder, sampleBlock);

            // Document should contain 2 paragraphs.
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Ensure that any content added to the document after the list goes
        /// into a new paragraph.
        /// </summary>
        [Test]
        public void EnsureSubsequentAdditionsGoToNewParagraph()
        {
            // Write the list block.
            renderer.Write(pdfBuilder, sampleBlock);

            // Add some additional content into the document.
            pdfBuilder.AppendText("existing p/graph", TextStyle.Normal);

            // Document should contain 2 paragraphs.
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Test the numbering in an ordered list.
        /// </summary>
        [Test]
        public void TestOrderedList()
        {
            ListBlock block = CreateOrderedListBlock("item1", "item2");
            renderer.Write(pdfBuilder, block);

            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            string expected = " 1. item1\n 2. item2\n";
            Assert.That(paragraph.GetRawText(), Is.EqualTo(expected));
        }

        /// <summary>
        /// Ensure that an unordered list's bullet character is used
        /// when rendering the list.
        /// </summary>
        /// <param name="bulletType">Bullet symbol specified in markdown.</param>
        [TestCase('-')]
        [TestCase('*')]
        public void TestBulletType(char bulletType)
        {
            ListBlock block = CreateListBlock(bulletType, "first item", "second item");
            renderer.Write(pdfBuilder, block);

            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            string expected = $" {bulletType} first item\n {bulletType} second item\n";
            Assert.That(paragraph.GetRawText(), Is.EqualTo(expected));
        }

        /// <summary>
        /// Ensure that child markdown objects (inlines) can be rendered
        /// inside of a list item.
        /// </summary>
        [Test]
        public void EnsureChildrenAreWritten()
        {
            // Create a list block with a single list item containing
            // an emphasis (italic) inline.
            BlockParser parser = new ListBlockParser();
            ListBlock block = new ListBlock(parser);
            block.IsOrdered = true;
            string item = "item";
            ListItemBlock listItem = new ListItemBlock(parser);
            ParagraphBlock paragraphBlock = new ParagraphBlock(new ParagraphBlockParser());
            EmphasisInline emphasis = new EmphasisInline();
            emphasis.DelimiterChar = '*';
            emphasis.DelimiterCount = 1;
            emphasis.AppendChild(new LiteralInline(item));
            paragraphBlock.Inline = new ContainerInline().AppendChild(emphasis);
            listItem.Add(paragraphBlock);
            block.Add(listItem);

            renderer.Write(pdfBuilder, block);

            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];

            // Paragraph should have 3 elements: bullet, content, and linefeed.
            Assert.That(paragraph.Elements.Count, Is.EqualTo(3));
            FormattedText text = paragraph.Elements[1] as FormattedText;
            Assert.That(text, Is.Not.Null);

            var textStyle = document.Styles[text.Style];
            Assert.That(textStyle.Font.Italic, Is.True);
            Assert.That(paragraph.GetRawText(), Is.EqualTo($" 1. {item}\n"));
        }

        /// <summary>
        /// Ensure that a newline character is inserted between children.
        /// </summary>
        [Test]
        public void EnsureChildrenNotOnSameLine()
        {
            ListBlock block = CreateListBlock('-', "x0", "x1");
            renderer.Write(pdfBuilder, block);

            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(6));
            Character linefeed = (Character)paragraph.Elements[2];
            Assert.That(linefeed.SymbolName, Is.EqualTo(SymbolName.LineBreak));
            linefeed = (Character)paragraph.Elements[5];
            Assert.That(linefeed.SymbolName, Is.EqualTo(SymbolName.LineBreak));
        }

        /// <summary>
        /// Create an unordered list block with the given items
        /// and bullet type.
        /// </summary>
        /// <param name="bulletType">Bullet character to be used.</param>
        /// <param name="contents">Contents - each element will be a list item.</param>
        private ListBlock CreateListBlock(char bulletType, params string[] contents)
        {
            BlockParser parser = new ListBlockParser();
            ListBlock block = new ListBlock(parser);
            block.BulletType = bulletType;
            foreach (string item in contents)
            {
                ListItemBlock listItem = new ListItemBlock(parser);
                ParagraphBlock paragraph = new ParagraphBlock(new ParagraphBlockParser());
                paragraph.Inline = new ContainerInline().AppendChild(new LiteralInline(item));
                listItem.Add(paragraph);
                block.Add(listItem);
            }

            return block;
        }

        /// <summary>
        /// Create an ordered list block with the given elements.
        /// </summary>
        /// <param name="contents">Contents - each element will be a list item.</param>
        private ListBlock CreateOrderedListBlock(params string[] contents)
        {
            BlockParser parser = new ListBlockParser();
            ListBlock block = new ListBlock(parser);
            block.IsOrdered = true;
            foreach (string item in contents)
            {
                ListItemBlock listItem = new ListItemBlock(parser);
                ParagraphBlock paragraph = new ParagraphBlock(new ParagraphBlockParser());
                paragraph.Inline = new ContainerInline().AppendChild(new LiteralInline(item));
                listItem.Add(paragraph);
                block.Add(listItem);
            }

            return block;
        }
    }
}
