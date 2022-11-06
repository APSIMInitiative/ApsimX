using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Interop.Markdown;
using MigraDocCore.DocumentObjectModel;
using System;

namespace UnitTests.Interop
{
    /// <summary>
    /// Unit tests for text styles supported by <see cref="PdfBuilder"/>.
    /// </summary>
    [TestFixture]
    public class PdfBuilderStyleTests
    {
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
        /// Test normal (ie no) style.
        /// </summary>
        [Test]
        public void TestNormal()
        {
            Style style = InsertText(TextStyle.Normal);
            Assert.AreEqual(doc.Styles.Normal, style);
        }

        /// <summary>
        /// Test italic style.
        /// </summary>
        [Test]
        public void TestItalic()
        {
            Style style = InsertText(TextStyle.Italic);
            Assert.True(style.Font.Italic);
        }

        /// <summary>
        /// Test strong (ie bold) style.
        /// </summary>
        [Test]
        public void TestStrong()
        {
            Style style = InsertText(TextStyle.Strong);
            Assert.True(style.Font.Bold);
        }

        /// <summary>
        /// Test underline style.
        /// </summary>
        [Test]
        public void TestUnderline()
        {
            Style style = InsertText(TextStyle.Underline);
            Assert.AreEqual(Underline.Single, style.Font.Underline);
        }

        /// <summary>
        /// Test strikethrough style.
        /// </summary>
        /// <remarks>
        /// Strikethrough is a TBI feature.
        /// </remarks>
        [Test]
        public void TestStrikethrough()
        {
            
        }

        /// <summary>
        /// Test superscript style.
        /// </summary>
        [Test]
        public void TestSuperscript()
        {
            Style style = InsertText(TextStyle.Superscript);
            Assert.True(style.Font.Superscript);
        }

        /// <summary>
        /// Test subscript style.
        /// </summary>
        [Test]
        public void TestSubscript()
        {
            Style style = InsertText(TextStyle.Subscript);
            Assert.True(style.Font.Subscript);
        }

        /// <summary>
        /// Test italic style.
        /// </summary>
        [Test]
        public void TestQuote()
        {
            Style style = InsertText(TextStyle.Quote);

            // Paragraph should be indented.
            Assert.Greater(style.ParagraphFormat.FirstLineIndent, 0);

            // Should not have the "normal" text colour.
            Assert.AreNotEqual(doc.Styles.Normal.Font.Color, style.Font.Color);

            // Should have a visible left border.
            Assert.True(style.ParagraphFormat.Borders.Left.Visible);
            Assert.AreNotEqual(Color.Empty, style.ParagraphFormat.Borders.Left.Color);
            Assert.Greater(style.ParagraphFormat.Borders.Left.Width, 0);

            // Border should have non-zero distance from bottom.
            Assert.Greater(style.ParagraphFormat.Borders.DistanceFromBottom, 0);

            // Should have extra space after the paragraph.
            Assert.Greater(style.ParagraphFormat.SpaceAfter, 0);
        }

        /// <summary>
        /// Test code style.
        /// </summary>
        [Test]
        public void TestCode()
        {
            Style style = InsertText(TextStyle.Code);

            // This is not great. Need to fix this in PdfBuilder.
            Assert.AreEqual("courier", style.Font.Name);

            // Code block should have visible borders.
            Assert.True(style.ParagraphFormat.Borders.Visible);
            Assert.AreNotEqual(Color.Empty, style.ParagraphFormat.Borders.Color);
            Assert.Greater(style.ParagraphFormat.Borders.Width, 0);

            // Code blocks should have padding on all sides.
            Assert.Greater(style.ParagraphFormat.Borders.DistanceFromBottom, 0);
            Assert.Greater(style.ParagraphFormat.Borders.DistanceFromTop, 0);
            Assert.Greater(style.ParagraphFormat.Borders.DistanceFromLeft, 0);
            Assert.Greater(style.ParagraphFormat.Borders.DistanceFromRight, 0);
        }

        /// <summary>
        /// Ensure that heading styles work correctly, and that
        /// font size decreases as heading level increases (up to
        /// h6, anyway).
        /// </summary>
        [Test]
        public void TestHeadingStyle()
        {
            // Add 6 headings - one of each level.
            Style h1 = CreateHeading(1);
            Style h2 = CreateHeading(2);
            Style h3 = CreateHeading(3);
            Style h4 = CreateHeading(4);
            Style h5 = CreateHeading(5);
            Style h6 = CreateHeading(6);

            Assert.Greater(h1.Font.Size.Point, h2.Font.Size.Point);
            Assert.Greater(h2.Font.Size.Point, h3.Font.Size.Point);
            Assert.Greater(h3.Font.Size.Point, h4.Font.Size.Point);
            Assert.Greater(h4.Font.Size.Point, h5.Font.Size.Point);
            Assert.Greater(h5.Font.Size.Point, h6.Font.Size.Point);

            Assert.True(h1.Font.Bold);
            Assert.True(h2.Font.Bold);
            Assert.True(h3.Font.Bold);
            Assert.True(h4.Font.Bold);
            Assert.True(h5.Font.Bold);
            Assert.True(h6.Font.Bold);

            // These headings are all at the top level. Outline level is based on
            // nested-ness of the heading, so these headings should all have outline
            // level set to 1.
            Assert.AreEqual(OutlineLevel.Level1, h1.ParagraphFormat.OutlineLevel);
            Assert.AreEqual(OutlineLevel.Level1, h2.ParagraphFormat.OutlineLevel);
            Assert.AreEqual(OutlineLevel.Level1, h3.ParagraphFormat.OutlineLevel);
            Assert.AreEqual(OutlineLevel.Level1, h4.ParagraphFormat.OutlineLevel);
            Assert.AreEqual(OutlineLevel.Level1, h5.ParagraphFormat.OutlineLevel);
            Assert.AreEqual(OutlineLevel.Level1, h6.ParagraphFormat.OutlineLevel);
        }

        /// <summary>
        /// Ensure that heading styles work correctly for nested headings.
        /// </summary>
        [Test]
        public void TestNestedHeadingOutlineLevel()
        {
            Style h1 = CreateHeading(1);
            builder.PushSubHeading();
            Style h2 = CreateHeading(2);
            builder.PushSubHeading();
            Style h3 = CreateHeading(3);
            builder.PushSubHeading();
            Style h4 = CreateHeading(4);
            builder.PushSubHeading();
            Style h5 = CreateHeading(5);
            builder.PushSubHeading();
            Style h6 = CreateHeading(6);
            builder.PushSubHeading();

            Assert.Greater(h1.Font.Size.Point, h2.Font.Size.Point);
            Assert.Greater(h2.Font.Size.Point, h3.Font.Size.Point);
            Assert.Greater(h3.Font.Size.Point, h4.Font.Size.Point);
            Assert.Greater(h4.Font.Size.Point, h5.Font.Size.Point);
            Assert.Greater(h5.Font.Size.Point, h6.Font.Size.Point);

            Assert.True(h1.Font.Bold);
            Assert.True(h2.Font.Bold);
            Assert.True(h3.Font.Bold);
            Assert.True(h4.Font.Bold);
            Assert.True(h5.Font.Bold);
            Assert.True(h6.Font.Bold);

            Assert.AreEqual(OutlineLevel.Level1, h1.ParagraphFormat.OutlineLevel);
            Assert.AreEqual(OutlineLevel.Level2, h2.ParagraphFormat.OutlineLevel);
            Assert.AreEqual(OutlineLevel.Level3, h3.ParagraphFormat.OutlineLevel);
            Assert.AreEqual(OutlineLevel.Level4, h4.ParagraphFormat.OutlineLevel);
            Assert.AreEqual(OutlineLevel.Level5, h5.ParagraphFormat.OutlineLevel);
            Assert.AreEqual(OutlineLevel.Level6, h6.ParagraphFormat.OutlineLevel);
        }

        /// <summary>
        /// Ensure that hyperlinks have the appropriate style applied.
        /// </summary>
        [Test]
        public void TestLinkStyle()
        {
            builder.SetLinkState("link uri");
            builder.AppendText("link text", TextStyle.Normal);
            builder.ClearLinkState();

            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Hyperlink link = (Hyperlink)paragraph.Elements[0];
            FormattedText formatted = (FormattedText)link.Elements[0];
            Style style = doc.Styles[formatted.Style];

            // Links should not have the "Normal" font colour - they should be blue.
            // I'm not going to test the exact shade. As long as it's not the
            // default font colour then we're good.
            Assert.AreNotEqual(doc.Styles.Normal.Font.Color, style.Font.Color);
        }

        /// <summary>
        /// Ensure that bibliography elements have the correct style applied.
        /// </summary>
        [Test]
        public void TestBibliographyStyle()
        {
            builder.AppendText("bibliography text", TextStyle.Bibliography);

            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            FormattedText formatted = (FormattedText)paragraph.Elements[0];
            Style style = doc.Styles[formatted.Style];

            // Should have left indent > 0.
            Assert.Greater(style.ParagraphFormat.LeftIndent.Point, 0);

            // Should have 1st line indent = -1 * left indent.
            // This has the effect of indenting all lines except for the first.
            Assert.AreEqual(style.ParagraphFormat.LeftIndent.Point, -1 * style.ParagraphFormat.FirstLineIndent.Point);
        }

        /// <summary>
        /// Ensure that style applied via <see cref="PdfBuilder.PushStyle(TextStyle)"/>
        /// is used when inserting text.
        /// </summary>
        [Test]
        public void TestPushStyle()
        {
            builder.PushStyle(TextStyle.Strong);
            Style style = InsertText(TextStyle.Normal);
            Assert.True(style.Font.Bold);
        }

        /// <summary>
        /// Ensure that multiple calls to <see cref="PdfBuilder.PushStyle(TextStyle)"/>
        /// combine, rather than overwrite, their style effects.
        /// </summary>
        [Test]
        public void TestNestedStyle()
        {
            builder.PushStyle(TextStyle.Strong);
            builder.PushStyle(TextStyle.Italic);
            Style style = InsertText(TextStyle.Normal);
            Assert.True(style.Font.Bold);
            Assert.True(style.Font.Italic);
        }

        /// <summary>
        /// Ensure that style applied via calls to
        /// <see cref="PdfBuilder.AppendText(string, TextStyle)"/> is combined with
        /// style applied via calls to <see cref="PdfBuilder.PushStyle(TextStyle)"/>.
        /// </summary>
        [Test]
        public void TestCombinedStyle()
        {
            builder.PushStyle(TextStyle.Superscript);
            Style style = InsertText(TextStyle.Italic);
            Assert.True(style.Font.Superscript);
            Assert.True(style.Font.Italic);
        }

        /// <summary>
        /// Ensure that after calling <see cref="PdfBuilder.PopStyle()"/>,
        /// style is no longer applied when inserting new text.
        /// </summary>
        [Test]
        public void TestPopStyle()
        {
            builder.PushStyle(TextStyle.Subscript);
            builder.PopStyle();
            Style style = InsertText(TextStyle.Normal);
            Assert.False(style.Font.Subscript);
        }

        /// <summary>
        /// Ensure that calling <see cref="PdfBuilder.PopStyle()"/> without
        /// a matching call to <see cref="PdfBuilder.PushStyle(TextStyle)"/>
        /// will trigger an exception.
        /// </summary>
        [Test]
        public void EnsurePopStyleCanThrow()
        {
            Assert.Throws<InvalidOperationException>(() => builder.PopStyle());
        }

        /// <summary>
        /// Create a heading with the given heading level.
        /// </summary>
        /// <param name="headingLevel">Heading level.</param>
        private Style CreateHeading(uint headingLevel)
        {
            builder.StartNewParagraph();
            builder.SetHeadingLevel(headingLevel);
            builder.AppendText($"h{headingLevel}", TextStyle.Normal);
            builder.ClearHeadingLevel();
            builder.StartNewParagraph();

            // We could have some assertions here, but we have explicit tests
            // for these casts elsewhere.
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[doc.LastSection.Elements.Count - 1];
            FormattedText formatted = (FormattedText)paragraph.Elements[0];
            return doc.Styles[formatted.Style];
        }

        /// <summary>
        /// Insert some text with the given style, and return the MigraDoc
        /// style of the inserted text.
        /// </summary>
        /// <param name="style">Style to be applied to the inserted text.</param>
        private Style InsertText(TextStyle style)
        {
            builder.AppendText("normal text", style);
            // We could have some assertions here, but we have explicit tests
            // for these casts elsewhere.
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            FormattedText formatted = (FormattedText)paragraph.Elements[0];
            return doc.Styles[formatted.Style];
        }
    }
}
