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
            Assert.That(style, Is.EqualTo(doc.Styles.Normal));
        }

        /// <summary>
        /// Test italic style.
        /// </summary>
        [Test]
        public void TestItalic()
        {
            Style style = InsertText(TextStyle.Italic);
            Assert.That(style.Font.Italic, Is.True);
        }

        /// <summary>
        /// Test strong (ie bold) style.
        /// </summary>
        [Test]
        public void TestStrong()
        {
            Style style = InsertText(TextStyle.Strong);
            Assert.That(style.Font.Bold, Is.True);
        }

        /// <summary>
        /// Test underline style.
        /// </summary>
        [Test]
        public void TestUnderline()
        {
            Style style = InsertText(TextStyle.Underline);
            Assert.That(style.Font.Underline, Is.EqualTo(Underline.Single));
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
            Assert.That(style.Font.Superscript, Is.True);
        }

        /// <summary>
        /// Test subscript style.
        /// </summary>
        [Test]
        public void TestSubscript()
        {
            Style style = InsertText(TextStyle.Subscript);
            Assert.That(style.Font.Subscript, Is.True);
        }

        /// <summary>
        /// Test italic style.
        /// </summary>
        [Test]
        public void TestQuote()
        {
            Style style = InsertText(TextStyle.Quote);

            // Paragraph should be indented.
            Assert.That(style.ParagraphFormat.FirstLineIndent.Value, Is.GreaterThan(0));

            // Should not have the "normal" text colour.
            Assert.That(style.Font.Color, Is.Not.EqualTo(doc.Styles.Normal.Font.Color));

            // Should have a visible left border.
            Assert.That(style.ParagraphFormat.Borders.Left.Visible, Is.True);
            Assert.That(style.ParagraphFormat.Borders.Left.Color, Is.Not.EqualTo(Color.Empty));
            Assert.That(style.ParagraphFormat.Borders.Left.Width.Value, Is.GreaterThan(0));

            // Border should have non-zero distance from bottom.
            Assert.That(style.ParagraphFormat.Borders.DistanceFromBottom.Value, Is.GreaterThan(0));

            // Should have extra space after the paragraph.
            Assert.That(style.ParagraphFormat.SpaceAfter.Value, Is.GreaterThan(0));
        }

        /// <summary>
        /// Test code style.
        /// </summary>
        [Test]
        public void TestCode()
        {
            Style style = InsertText(TextStyle.Code);

            // This is not great. Need to fix this in PdfBuilder.
            Assert.That(style.Font.Name, Is.EqualTo("courier"));

            // Code block should have visible borders.
            Assert.That(style.ParagraphFormat.Borders.Visible, Is.True);
            Assert.That(style.ParagraphFormat.Borders.Color, Is.Not.EqualTo(Color.Empty));
            Assert.That(style.ParagraphFormat.Borders.Width.Value, Is.GreaterThan(0));

            // Code blocks should have padding on all sides.
            Assert.That(style.ParagraphFormat.Borders.DistanceFromBottom.Value, Is.GreaterThan(0));
            Assert.That(style.ParagraphFormat.Borders.DistanceFromTop.Value, Is.GreaterThan(0));
            Assert.That(style.ParagraphFormat.Borders.DistanceFromLeft.Value, Is.GreaterThan(0));
            Assert.That(style.ParagraphFormat.Borders.DistanceFromRight.Value, Is.GreaterThan(0));
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

            Assert.That(h1.Font.Size.Point, Is.GreaterThan(h2.Font.Size.Point));
            Assert.That(h2.Font.Size.Point, Is.GreaterThan(h3.Font.Size.Point));
            Assert.That(h3.Font.Size.Point, Is.GreaterThan(h4.Font.Size.Point));
            Assert.That(h4.Font.Size.Point, Is.GreaterThan(h5.Font.Size.Point));
            Assert.That(h5.Font.Size.Point, Is.GreaterThan(h6.Font.Size.Point));

            Assert.That(h1.Font.Bold, Is.True);
            Assert.That(h2.Font.Bold, Is.True);
            Assert.That(h3.Font.Bold, Is.True);
            Assert.That(h4.Font.Bold, Is.True);
            Assert.That(h5.Font.Bold, Is.True);
            Assert.That(h6.Font.Bold, Is.True);

            // These headings are all at the top level. Outline level is based on
            // nested-ness of the heading, so these headings should all have outline
            // level set to 1.
            Assert.That(h1.ParagraphFormat.OutlineLevel, Is.EqualTo(OutlineLevel.Level1));
            Assert.That(h2.ParagraphFormat.OutlineLevel, Is.EqualTo(OutlineLevel.Level1));
            Assert.That(h3.ParagraphFormat.OutlineLevel, Is.EqualTo(OutlineLevel.Level1));
            Assert.That(h4.ParagraphFormat.OutlineLevel, Is.EqualTo(OutlineLevel.Level1));
            Assert.That(h5.ParagraphFormat.OutlineLevel, Is.EqualTo(OutlineLevel.Level1));
            Assert.That(h6.ParagraphFormat.OutlineLevel, Is.EqualTo(OutlineLevel.Level1));
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

            Assert.That(h1.Font.Size.Point, Is.GreaterThan(h2.Font.Size.Point));
            Assert.That(h2.Font.Size.Point, Is.GreaterThan(h3.Font.Size.Point));
            Assert.That(h3.Font.Size.Point, Is.GreaterThan(h4.Font.Size.Point));
            Assert.That(h4.Font.Size.Point, Is.GreaterThan(h5.Font.Size.Point));
            Assert.That(h5.Font.Size.Point, Is.GreaterThan(h6.Font.Size.Point));

            Assert.That(h1.Font.Bold, Is.True);
            Assert.That(h2.Font.Bold, Is.True);
            Assert.That(h3.Font.Bold, Is.True);
            Assert.That(h4.Font.Bold, Is.True);
            Assert.That(h5.Font.Bold, Is.True);
            Assert.That(h6.Font.Bold, Is.True);

            Assert.That(h1.ParagraphFormat.OutlineLevel, Is.EqualTo(OutlineLevel.Level1));
            Assert.That(h2.ParagraphFormat.OutlineLevel, Is.EqualTo(OutlineLevel.Level2));
            Assert.That(h3.ParagraphFormat.OutlineLevel, Is.EqualTo(OutlineLevel.Level3));
            Assert.That(h4.ParagraphFormat.OutlineLevel, Is.EqualTo(OutlineLevel.Level4));
            Assert.That(h5.ParagraphFormat.OutlineLevel, Is.EqualTo(OutlineLevel.Level5));
            Assert.That(h6.ParagraphFormat.OutlineLevel, Is.EqualTo(OutlineLevel.Level6));
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
            Assert.That(style.Font.Color, Is.Not.EqualTo(doc.Styles.Normal.Font.Color));
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
            Assert.That(style.ParagraphFormat.LeftIndent.Point, Is.GreaterThan(0));

            // Should have 1st line indent = -1 * left indent.
            // This has the effect of indenting all lines except for the first.
            Assert.That(-1 * style.ParagraphFormat.FirstLineIndent.Point, Is.EqualTo(style.ParagraphFormat.LeftIndent.Point));
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
            Assert.That(style.Font.Bold, Is.True);
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
            Assert.That(style.Font.Bold, Is.True);
            Assert.That(style.Font.Italic, Is.True);
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
            Assert.That(style.Font.Superscript, Is.True);
            Assert.That(style.Font.Italic, Is.True);
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
            Assert.That(style.Font.Subscript, Is.False);
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
