using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using Markdig.Syntax;
using APSIM.Interop.Markdown;
using MigraDocCore.DocumentObjectModel;
using Moq;
using System;
using System.Drawing;
using ITag = APSIM.Shared.Documentation.ITag;
using Color = System.Drawing.Color;
using SectionTag = APSIM.Shared.Documentation.Section;
using ParagraphTag = APSIM.Shared.Documentation.Paragraph;
using ImageTag = APSIM.Shared.Documentation.Image;
using TableTag = APSIM.Shared.Documentation.Table;
using GraphTag = APSIM.Shared.Documentation.Graph;
using GraphPageTag = APSIM.Shared.Documentation.GraphPage;
using MigraDocImage = MigraDocCore.DocumentObjectModel.Shapes.Image;
using System.Data;
using System.Collections.Generic;
using APSIM.Shared.Graphing;

namespace UnitTests.Interop
{
    /// <summary>
    /// Unit tests for <see cref="PdfBuilder"/>.
    /// </summary>
    /// <remarks>
    /// I haven't explicitly tested GetLastParagraph() and GetLastSection(),
    /// both of which are pretty important internal functions of the
    /// PdfBuilder implementation. I'm not sure of the best way to test them.
    /// They are also tested pretty extensively due to the large number of
    /// tests for this class so I'm going to leave this as-is for now.
    /// 
    /// I haven't tested bookmarks yet, as this feature is TBI.
    /// 
    /// The only other major part which I haven't tested is the algorithm
    /// for finding a tag renderer for the given tag. Again, this should be
    /// implicitly tested fairly extensively, but it might be nice to revisit
    /// this in the future.
    /// </remarks>
    [TestFixture]
    public class PdfBuilderTests
    {
        /// <summary>
        /// Default page width.
        /// </summary>
        private const double defaultWidth = 604.72440944881873;

        /// <summary>
        /// Default page height.
        /// </summary>
        private const double defaultHeight = 952.44102177657487;

        /// <summary>
        /// The pdf builder instance used for testing.
        /// </summary>
        private PdfBuilder builder;

        /// <summary>
        /// This is the MigraDoc document which will be modified by
        /// <see cref="builder"/>.
        /// </summary>
        private Document doc;

        /// <summary>
        /// Initialise the PDF buidler and its document.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            doc = new Document();
            builder = new PdfBuilder(doc, PdfOptions.Default);
        }

        /// <summary>
        /// Ensure that PdfBuilder can render all known tag types.
        /// </summary>
        [Test]
        public void EnsureCanRenderKnownTagTypes()
        {
            EnsureCanRenderTag(new SectionTag("title", new ITag[1] { new ParagraphTag("") }));
            EnsureCanRenderTag(new ParagraphTag("paragraph text"));
            using (SkiaSharp.SKImage image = SkiaSharp.SKImage.Create(new SkiaSharp.SKImageInfo(2, 2)))
                EnsureCanRenderTag(new ImageTag(image));
            EnsureCanRenderTag(new TableTag(CreateTable()));
            EnsureCanRenderTag(CreateGraphTag());
            EnsureCanRenderTag(new GraphPageTag(new[] { CreateGraphTag() }));
        }

        /// <summary>
        /// Ensure that PdfBuilder throws an exception when trying
        /// to render an unknown tag type (ie a tag which is missing
        /// a corresponding renderer type).
        /// </summary>
        [Test]
        public void EnsureUnknownTagTypesCauseError()
        {
            Mock<ITag> mockTag = new Mock<ITag>();
            Assert.Throws<NotImplementedException>(() => builder.Write(mockTag.Object));
        }

        /// <summary>
        /// Ensure that calling <see cref="PdfBuilder.SetLinkState(string)"/> will cause
        /// subsequent textual additions to be inserted as a link.
        /// </summary>
        [Test]
        public void TestSetLinkState()
        {
            string linkUri = "linkuri";
            string linkText = "this is a hyperlink";
            builder.SetLinkState(linkUri);
            builder.AppendText(linkText, TextStyle.Normal);
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.AreEqual(1, paragraph.Elements.Count);
            Hyperlink link = (Hyperlink)paragraph.Elements[0];
            Assert.AreEqual(linkUri, link.Name);
            Assert.AreEqual(1, link.Elements.Count);
            FormattedText text = (FormattedText)link.Elements[0];
            Assert.AreEqual(1, text.Elements.Count);
            Text rawText = (Text)text.Elements[0];
            Assert.AreEqual(linkText, rawText.Content);
        }

        /// <summary>
        /// Ensure that serial calls to <see cref="PdfBuilder.SetLinkState(string)"/>
        /// trigger an exception. (Nested links are not implemented.)
        /// </summary>
        [Test]
        public void EnsureSetLinkStateThrows()
        {
            builder.SetLinkState("");
            Assert.Throws<NotImplementedException>(() => builder.SetLinkState(""));
        }

        /// <summary>
        /// Ensure that after calling <see cref="PdfBuilder.ClearLinkState()"/>,
        /// newly-inserted text is no longer inserted a hyperlink.
        /// </summary>
        [Test]
        public void TestClearLinkState()
        {
            builder.SetLinkState("this is a link");
            builder.ClearLinkState();
            builder.AppendText("some text", TextStyle.Normal);
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];

            // This hyperlink is empty - arguably it shouldn't be inserted at all.
            Assert.AreEqual(2, paragraph.Elements.Count);
            Assert.True(paragraph.Elements[0] is Hyperlink);
            Assert.True(paragraph.Elements[1] is FormattedText);
        }

        /// <summary>
        /// Ensure that calling <see cref="PdfBuilder.ClearLinkState()"/> without
        /// first calling <see cref="PdfBuilder.SetLinkState(string)"/> will trigger
        /// an exception.
        /// </summary>
        [Test]
        public void EnsureClearLinkStateThrows()
        {
            Assert.Throws<InvalidOperationException>(() => builder.ClearLinkState());
        }

        /// <summary>
        /// Ensure that calls to <see cref="PdfBuilder.StartListItem(string)"/> cause
        /// a bullet point to be inserted.
        /// </summary>
        [TestCase('-')]
        [TestCase('*')]
        public void TestStartListItem(char bulletChar)
        {
            string bullet = bulletChar.ToString();
            builder.StartListItem(bullet);
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.AreEqual(1, paragraph.Elements.Count);
            Text text = (Text)paragraph.Elements[0];
            Assert.AreEqual(bullet, text.Content);
        }

        /// <summary>
        /// Ensure that text appended after <see cref="PdfBuilder.StartListItem(string)"/>
        /// is added to the list item (ie not after a line break or in a new paragraph).
        /// </summary>
        [TestCase('-')]
        public void TestWriteToListItem(char bulletChar)
        {
            string bullet = bulletChar.ToString();
            builder.StartListItem(bullet);
            string text = "list item contents";
            builder.AppendText(text, TextStyle.Normal);
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.AreEqual(2, paragraph.Elements.Count);
            Text bulletElement = (Text)paragraph.Elements[0];
            Assert.AreEqual(bullet, bulletElement.Content);
            FormattedText insertedText = (FormattedText)paragraph.Elements[1];
            Assert.AreEqual(1, insertedText.Elements.Count);
            Text rawText = (Text)insertedText.Elements[0];
            Assert.AreEqual(text, rawText.Content);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartListItem(string)"/> will trigger
        /// an exception if called serially without calling
        /// <see cref="PdfBuilder.FinishListItem()"/> in between.
        /// </summary>
        [Test]
        public void EnsureStartListItemCanThrow()
        {
            builder.StartListItem("");
            Assert.Throws<NotImplementedException>(() => builder.StartListItem(""));
        }

        /// <summary>
        /// Ensure that after finishing a list item (via <see cref="PdfBuilder.FinishListItem()"/>),
        /// any newly-inserted text is not added to that list item, but is still inserted
        /// into the same paragraph.
        /// </summary>
        [Test]
        public void TestFinishListItem()
        {
            string bullet = "-";
            string bulletText = "bullet point text";
            string serialText = "this goes after the list item.";

            builder.StartListItem(bullet);
            builder.AppendText(bulletText, TextStyle.Normal);
            builder.FinishListItem();
            builder.AppendText(serialText, TextStyle.Normal);

            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.AreEqual(4, paragraph.Elements.Count);

            // First element is the bullet.
            Text insertedBullet = (Text)paragraph.Elements[0];
            Assert.AreEqual(bullet, insertedBullet.Content);

            // Second element is the list item text.
            FormattedText listItem = (FormattedText)paragraph.Elements[1];
            Assert.AreEqual(1, listItem.Elements.Count);
            Text listItemText = (Text)listItem.Elements[0];
            Assert.AreEqual(bulletText, listItemText.Content);

            // Third element is a newline character.
            Character newline = (Character)paragraph.Elements[2];
            Assert.AreEqual(SymbolName.LineBreak, newline.SymbolName);

            // Fourth element is the text inserted after the list item.
            FormattedText serialContent = (FormattedText)paragraph.Elements[3];
            Assert.AreEqual(1, serialContent.Elements.Count);
            Text rawText = (Text)serialContent.Elements[0];
            Assert.AreEqual(serialText, rawText.Content);
        }

        /// <summary>
        /// Ensure that calls to <see cref="PdfBuilder.FinishListItem()"/> trigger an
        /// exception if <see cref="PdfBuilder.StartListItem(string)"/> has not been
        /// previously called.
        /// </summary>
        [Test]
        public void EnsureFinishListItemCanThrow()
        {
            Assert.Throws<NotImplementedException>(() => builder.FinishListItem());
        }

        /// <summary>
        /// Ensure that multiple list items can be added to the document.
        /// </summary>
        [Test]
        public void TestMultipleListItems()
        {
            string bullet = "+";
            string listItem0 = "list item 0";
            string listItem1 = "the next list item";

            builder.StartListItem(bullet);
            builder.AppendText(listItem0, TextStyle.Normal);
            builder.FinishListItem();

            builder.StartListItem(bullet);
            builder.AppendText(listItem1, TextStyle.Normal);
            builder.FinishListItem();

            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.AreEqual(6, paragraph.Elements.Count);

            // 1. Bullet.
            Text insertedBullet = (Text)paragraph.Elements[0];
            Assert.AreEqual(bullet, insertedBullet.Content);

            // 2. List item 0.
            FormattedText listItem = (FormattedText)paragraph.Elements[1];
            Assert.AreEqual(1, listItem.Elements.Count);
            Text listItemText = (Text)listItem.Elements[0];
            Assert.AreEqual(listItem0, listItemText.Content);

            // 3. Newline.
            Character newline = (Character)paragraph.Elements[2];
            Assert.AreEqual(SymbolName.LineBreak, newline.SymbolName);

            // 4. Another bullet.
            insertedBullet = (Text)paragraph.Elements[3];
            Assert.AreEqual(bullet, insertedBullet.Content);

            // 5. List item 1.
            listItem = (FormattedText)paragraph.Elements[4];
            Assert.AreEqual(1, listItem.Elements.Count);
            listItemText = (Text)listItem.Elements[0];
            Assert.AreEqual(listItem1, listItemText.Content);

            // 6. Another newline.
            newline = (Character)paragraph.Elements[5];
            Assert.AreEqual(SymbolName.LineBreak, newline.SymbolName);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendText(string, TextStyle)"/>
        /// will insert the text into the document.
        /// </summary>
        [Test]
        public void TestAppendText()
        {
            string message = "This is a plaintext message.";
            builder.AppendText(message, TextStyle.Normal);
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.AreEqual(1, paragraph.Elements.Count);
            FormattedText formatted = (FormattedText)paragraph.Elements[0];
            Assert.AreEqual(1, formatted.Elements.Count);
            Text plain = (Text)formatted.Elements[0];
            Assert.AreEqual(message, plain.Content);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendText(string, TextStyle)"/>
        /// will apply the specified style to the inserted text.
        /// </summary>
        [TestCase(TextStyle.Normal)]
        [TestCase(TextStyle.Italic)]
        [TestCase(TextStyle.Strong)]
        [TestCase(TextStyle.Underline)]
        // [TestCase(TextStyle.Strikethrough)] // tbi
        [TestCase(TextStyle.Superscript)]
        [TestCase(TextStyle.Subscript)]
        [TestCase(TextStyle.Quote)]
        [TestCase(TextStyle.Code)]
        public void TestAppendTextStyle(TextStyle style)
        {
            builder.AppendText("some text", style);
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.AreEqual(1, paragraph.Elements.Count);
            FormattedText formatted = (FormattedText)paragraph.Elements[0];
            if (style == TextStyle.Normal)
                Assert.AreEqual(doc.Styles.Normal.Name, formatted.Style);
            else
                Assert.AreNotEqual(doc.Styles.Normal.Name, formatted.Style);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendText(string, TextStyle)"/>
        /// will insert the text at the end of the document, in the same paragraph
        /// as existing content.
        /// </summary>
        [Test]
        public void TestAppendTextLocation()
        {
            string existingText = "Some existing content.";
            string addedText = "This should be inserted into the same paragraph";

            doc.AddSection().AddParagraph(existingText);
            builder.AppendText(addedText, TextStyle.Normal);

            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.AreEqual(2, paragraph.Elements.Count);

            Text text = (Text)paragraph.Elements[0];
            Assert.AreEqual(existingText, text.Content);

            FormattedText formatted = (FormattedText)paragraph.Elements[1];
            Assert.AreEqual(1, formatted.Elements.Count);
            Text plain = (Text)formatted.Elements[0];
            Assert.AreEqual(addedText, plain.Content);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendText(string, TextStyle)"/>
        /// will insert the text in to the final paragraph in the document.
        /// </summary>
        [Test]
        public void TestAppendTextCorrectParagraph()
        {
            doc.AddSection().AddParagraph("paragraph 1");
            doc.LastSection.AddParagraph("paragraph 2");
            builder.AppendText("new text", TextStyle.Normal);
            Assert.AreEqual(2, doc.LastSection.Elements.Count);
            Assert.True(doc.LastSection.Elements[0] is Paragraph);
            Assert.True(doc.LastSection.Elements[1] is Paragraph);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendText(string, TextStyle)"/> will
        /// insert a new paragraph if none exists.
        /// </summary>
        [Test]
        public void TestAppendTextNewParagraph()
        {
            builder.AppendText("new paragraph", TextStyle.Normal);
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Assert.True(doc.LastSection.Elements[0] is Paragraph);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendImage(SkiaSharp.SKImage)"/> adds the
        /// image into the document.
        /// </summary>
        [Test]
        public void TestAppendImage()
        {
            using (SkiaSharp.SKImage image = SkiaSharp.SKImage.Create(new SkiaSharp.SKImageInfo(2, 2)))
            {
                builder.AppendImage(image);
                Assert.AreEqual(1, doc.LastSection.Elements.Count);
                Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
                Assert.AreEqual(1, paragraph.Elements.Count);
                MigraDocImage inserted = (MigraDocImage)paragraph.Elements[0];
                Assert.AreEqual(image.Width, inserted.Source.Width);
                Assert.AreEqual(image.Height, inserted.Source.Height);
            }
        }

        /// <summary>
        /// Ensure that any images are resized so that they fit the document
        /// before they are added, and that the aspect ratio of the image
        /// is preserved when it is resized.
        /// </summary>
        /// <param name="pageWidth">Page width (in px).</param>
        /// <param name="pageHeight">Page height (in px).</param>
        /// <param name="imageWidth">Image width (in px).</param>
        /// <param name="imageHeight">Image height (in px).</param>
        /// <param name="expectedWidth">Expected width of the image after it's resized (in px).</param>
        /// <param name="expectedHeight">Expected height of the image after it's resized (in px).</param>
        [TestCase(10, 10, 20, 20, 10, 10)]
        [TestCase(10, 10, 21, 9, 10, 4)]
        [TestCase(10, 10, 8, 14, 5, 10)]
        public void TestAppendImageSizing(double pageWidth, double pageHeight, int imageWidth, int imageHeight, int expectedWidth, int expectedHeight)
        {
            Section section = doc.AddSection();
            _ = section.Elements;
            const double pointsToPixels = 96.0 / 72;
            // When setting the page width and height, convert from px to points.
            section.PageSetup.PageWidth =  Unit.FromPoint(pageWidth / pointsToPixels);
            section.PageSetup.PageHeight = Unit.FromPoint(pageHeight / pointsToPixels);
            using (SkiaSharp.SKImage image = SkiaSharp.SKImage.Create(new SkiaSharp.SKImageInfo(imageWidth, imageHeight)))
            {
                builder.AppendImage(image);
                Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
                MigraDocImage inserted = (MigraDocImage)paragraph.Elements[0];

                // The image we inserted is 20x20. However, the page size is
                // 10x10. The image would have been resized to fit this.
                Assert.AreEqual(expectedWidth, inserted.Source.Width);
                Assert.AreEqual(expectedHeight, inserted.Source.Height);
            }
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendImage(SkiaSharp.SKImage)"/>
        /// adds the image at the end of the document, in an existing paragraph
        /// (if one exists).
        /// </summary>
        [Test]
        public void TestAppendImageLocation()
        {
            doc.AddSection().AddParagraph("some text is already in the paragraph");
            using (SkiaSharp.SKImage image = SkiaSharp.SKImage.Create(new SkiaSharp.SKImageInfo(2, 2)))
                builder.AppendImage(image);
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.AreEqual(2, paragraph.Elements.Count);
            Assert.True(paragraph.Elements[0] is Text);
            Assert.True(paragraph.Elements[1] is MigraDocImage);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendHorizontalRule()"/> adds
        /// a horizontal rule at the current end of document.
        /// </summary>
        [Test]
        public void TestAppendHR()
        {
            builder.AppendHorizontalRule();
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.AreEqual(0, paragraph.Elements.Count);
            Assert.AreEqual(1, paragraph.Format.Borders.Bottom.Width.Point);
        }

        /// <summary>
        /// Ensure that any content added after a HR is indeed added
        /// below the HR.
        /// </summary>
        [Test]
        public void TestAppendAfterHR()
        {
            builder.AppendText("existing paragraph", TextStyle.Normal);
            builder.AppendHorizontalRule();
            builder.AppendText("additional text.", TextStyle.Normal);
            Assert.AreEqual(2, doc.LastSection.Elements.Count);
        }

        /// <summary>
        /// Ensure that adding text after calling
        /// <see cref="PdfBuilder.StartNewParagraph()"/> adds the text into
        /// a new paragraph.
        /// </summary>
        [Test]
        public void TestStartNewParagraph()
        {
            doc.AddSection().AddParagraph("existing paragraph");
            builder.StartNewParagraph();
            string text = "this should be in a new paragraph";
            builder.AppendText(text, TextStyle.Normal);

            Assert.AreEqual(2, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[1];
            Assert.AreEqual(1, paragraph.Elements.Count);
            FormattedText formatted = (FormattedText)paragraph.Elements[0];
            Assert.AreEqual(1, formatted.Elements.Count);
            Text plain = (Text)formatted.Elements[0];
            Assert.AreEqual(text, plain.Content);
        }

        /// <summary>
        /// Ensure that calling <see cref="PdfBuilder.StartNewParagraph()"/> while inserting
        /// text into an inline hyperlink will trigger an exception.
        /// </summary>
        [Test]
        public void EnsureStartParagraphCanThrow()
        {
            builder.SetLinkState("link uri");
            Assert.Throws<InvalidOperationException>(() => builder.StartNewParagraph());
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.IncrementFigureNumber()"/>
        /// works as expected.
        /// </summary>
        [Test]
        public void TestIncrementFigureCount()
        {
            Assert.AreEqual(0, builder.FigureNumber);
            builder.IncrementFigureNumber();
            Assert.AreEqual(1, builder.FigureNumber);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.GetPageSize(out double, out double)"/>
        /// returns the correct width and height in pixels.
        /// </summary>
        [Test]
        public void TestGetPageSize()
        {
            Section section = doc.AddSection();
            section.PageSetup.PageWidth = 216;
            section.PageSetup.PageHeight = 288;

            builder.GetPageSize(out double width, out double height);

            // fixme: should probably find a better way to test this...
            Assert.AreEqual(288, width);
            Assert.AreEqual(384, height);
        }

        /// <summary>
        /// Ensure that when the document contains multiple sections of different
        /// sizes, <see cref="PdfBuilder.GetPageSize(out double, out double)"/>
        /// returns the correct width and height in pixels of the last section.
        /// </summary>
        [Test]
        public void TestGetPageSizeMultipleSections()
        {
            Section section0 = doc.AddSection();
            Section section1 = doc.AddSection();
            section0.PageSetup.PageWidth = 300;
            section0.PageSetup.PageHeight = 450;
            section1.PageSetup.PageWidth = 324;
            section1.PageSetup.PageHeight = 360;

            builder.GetPageSize(out double width, out double height);

            Assert.AreEqual(432, width);
            Assert.AreEqual(480, height);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.GetPageSize(out double, out double)"/>
        /// returns the default page size if the document contains no sections.
        /// </summary>
        [Test]
        public void TestGetPageSizeNoSections()
        {
            builder.GetPageSize(out double width, out double height);
            Assert.AreEqual(defaultWidth, width);
            Assert.AreEqual(defaultHeight, height);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.GetPageSize(out double, out double)"/>
        /// returns the document's default page width/height if the last section's
        /// width or height are 0.
        /// </summary>
        [TestCase(123, 0)]
        [TestCase(0, 666)]
        [TestCase(0, 0)]
        public void TestGetDefaultPageSize(double sectionWidth, double sectionHeight)
        {
            // Unfortunately, changes to the default document page width/height
            // don't seem to persist. Should probably revisit this later, but
            // for now I'm just going to hardcode the default page width/height
            // as chosen by MigraDoc.

            Section section = doc.AddSection();
            section.PageSetup.PageWidth = sectionWidth;
            section.PageSetup.PageHeight = sectionHeight;

            builder.GetPageSize(out double width, out double height);

            Assert.AreEqual(defaultWidth, width);
            Assert.AreEqual(defaultHeight, height);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.GetPageSize(out double, out double)"/>
        /// takes the (vertical) space after paragraphs into account.
        /// </summary>
        [Test]
        public void TestGetPageSizeSpaceAfterParagraph()
        {
            Section section = doc.AddSection();
            section.PageSetup.PageWidth = 576;
            section.PageSetup.PageHeight = 648;
            section.PageSetup.LeftMargin = 72;
            doc.Styles.Normal.ParagraphFormat.SpaceAfter = 9;

            builder.GetPageSize(out double width, out double height);

            Assert.AreEqual(672, width);
            Assert.AreEqual(852, height);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.GetPageSize(out double, out double)"/>
        /// takes the left margin width into account.
        /// </summary>
        [Test]
        public void TestGetPageSizeLeftMargin()
        {
            Section section = doc.AddSection();
            section.PageSetup.PageWidth = 576;
            section.PageSetup.PageHeight = 648;
            section.PageSetup.LeftMargin = 72;

            builder.GetPageSize(out double width, out double height);

            Assert.AreEqual(672, width);
            Assert.AreEqual(864, height);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.GetPageSize(out double, out double)"/>
        /// takes the right margin width into account.
        /// </summary>
        [Test]
        public void TestGetPageSizeRightMargin()
        {
            Section section = doc.AddSection();
            section.PageSetup.PageWidth = 576;
            section.PageSetup.PageHeight = 648;
            section.PageSetup.RightMargin = 144;

            builder.GetPageSize(out double width, out double height);

            Assert.AreEqual(576, width);
            Assert.AreEqual(864, height);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.GetPageSize(out double, out double)"/>
        /// takes the top margin height into account.
        /// </summary>
        [Test]
        public void TestGetPageSizeTopMargin()
        {
            Section section = doc.AddSection();
            section.PageSetup.PageWidth = 720;
            section.PageSetup.PageHeight = 792;
            section.PageSetup.TopMargin = 216;

            builder.GetPageSize(out double width, out double height);

            Assert.AreEqual(960, width);
            Assert.AreEqual(768, height);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.GetPageSize(out double, out double)"/>
        /// takes the bottom margin height into account.
        /// </summary>
        [Test]
        public void TestGetPageSizeBottomMargin()
        {
            Section section = doc.AddSection();
            section.PageSetup.PageWidth = 792;
            section.PageSetup.PageHeight = 864;
            section.PageSetup.BottomMargin = 72;

            builder.GetPageSize(out double width, out double height);

            Assert.AreEqual(1056, width);
            Assert.AreEqual(1056, height);
        }

        /// <summary>
        /// Ensure that writing an unknown markdown block type does not cause an exception.
        /// </summary>
        [Test]
        public void TestWriteUnknownMarkdownBlock()
        {
            Mock<MarkdownObject> mockBlock = new Mock<MarkdownObject>();
            Assert.DoesNotThrow(() => builder.Write(mockBlock.Object));
        }

        /// <summary>
        /// Create a simple graph tag.
        /// </summary>
        private GraphTag CreateGraphTag()
        {
            Marker marker = new Marker(MarkerType.None, MarkerSize.Normal, 1);
            Line line = new Line(LineType.Solid, LineThickness.Normal);
            IEnumerable<object> x = new object[] { 0, 1 };
            IEnumerable<object> y = new object[] { 1, 2 };
            IEnumerable<Series> series = new Series[1] { new LineSeries("s0", Color.Red, true, x, y, line, marker, "", "") };
            Axis xAxis = new Axis("x", AxisPosition.Bottom, false, false);
            Axis yAxis = new Axis("Y", AxisPosition.Left, false, false);
            LegendConfiguration legend = new LegendConfiguration(LegendOrientation.Horizontal, LegendPosition.BottomCenter, true);
            GraphTag graph = new GraphTag("title", string.Empty, series, xAxis, yAxis, legend);
            return graph;
        }

        /// <summary>
        /// Create a simple DataTable.
        /// </summary>
        private DataTable CreateTable()
        {
            DataTable table = new DataTable("sample table");
            table.Columns.Add("x", typeof(string));
            table.Columns.Add("y", typeof(string));
            table.Rows.Add("x0", "y0");
            table.Rows.Add("x1", "y1");
            return table;
        }

        /// <summary>
        /// Ensure that the given tag is rendered without error and
        /// adds something to the document.
        /// </summary>
        /// <param name="tag">The tag to be rendered.</param>
        private void EnsureCanRenderTag(ITag tag)
        {
            Assert.DoesNotThrow(() => builder.Write(tag));
            Assert.Greater(doc.LastSection.Elements.Count, 0);
        }
    }
}
