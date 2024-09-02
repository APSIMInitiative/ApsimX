using System;
using NUnit.Framework;
using APSIM.Interop.Markdown;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using System.Collections.Generic;
using Document = MigraDocCore.DocumentObjectModel.Document;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using FormattedText = MigraDocCore.DocumentObjectModel.FormattedText;
using Text = MigraDocCore.DocumentObjectModel.Text;
using OutlineLevel = MigraDocCore.DocumentObjectModel.OutlineLevel;
using MigraDocCore.DocumentObjectModel.Fields;

namespace UnitTests.Interop.Documentation
{
    /// <summary>
    /// Tests for the headings API of the <see cref="PdfBuilder"/> class.
    /// This fixture is *NOT* intended for testing the renderers - only
    /// the PdfBuilder API.
    /// </summary>
    [TestFixture]
    public class PdfBuilderHeadingTests
    {
        /// <summary>The builder object used for testing.</summary>
        private PdfBuilder builder;

        /// <summary>
        /// This is the MigraDoc document which will be modified by
        /// <see cref="builder"/>.
        /// </summary>
        private Document doc;

        /// <summary>Initialise the PDF buidler and its document.</summary>
        [SetUp]
        public void SetUp()
        {
            doc = new Document();
            builder = new PdfBuilder(doc, PdfOptions.Default);
        }

        [Test]
        public void TestDocWithNoSections()
        {
            string msg = "Builder threw an exception on a document with no sections";
            Assert.DoesNotThrow(() => builder.AppendText("x", TextStyle.Normal), msg);
        }

        /// <summary>
        /// Test the simple case of a single heading. It should be rendered as 
        /// 1 heading text
        /// </summary>
        [Test]
        public void TestSimpleHeading()
        {
            builder.AppendHeading("hello");
            Assert.That(doc.Sections.Count, Is.EqualTo(1));
            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(1));

            Paragraph paragraph = doc.LastSection.Elements.LastObject as Paragraph;
            ValidateHeading(paragraph, "1 ", "hello");
        }

        /// <summary>
        /// Test the case of a heading with a single subheading. It should be rendered as:
        /// 1 toplevel heading text
        /// 1.1 nested heading text
        /// </summary>
        [Test]
        public void TestSingleNestedHeading()
        {
            builder.AppendHeading("top heading");

            builder.PushSubHeading();
            builder.AppendHeading("nested heading");
            builder.PopSubHeading();

            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(2), "Section should have two paragraphs");
            Paragraph top = doc.LastSection.Elements[0] as Paragraph;
            Paragraph nested = doc.LastSection.Elements[1] as Paragraph;

            ValidateHeading(top, "1 ", "top heading");
            ValidateHeading(nested, "1.1 ", "nested heading");
        }

        /// <summary>
        /// Test the case of multiple levels of nested headings.
        /// 1 toplevel heading
        /// 1.1 middle heading
        /// 1.1.1 lowest level heading
        /// </summary>
        [Test]
        public void TestMultipleNestedHeadings()
        {
            builder.AppendHeading("toplevel heading");

            builder.PushSubHeading();
            builder.AppendHeading("middle heading");
            builder.PushSubHeading();
            builder.AppendHeading("lowest level heading");
            builder.PopSubHeading();
            builder.PopSubHeading();

            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(3), "Section should have three paragraphs");
            Paragraph top = doc.LastSection.Elements[0] as Paragraph;
            Paragraph middle = doc.LastSection.Elements[1] as Paragraph;
            Paragraph lowest = doc.LastSection.Elements[2] as Paragraph;

            ValidateHeading(top, "1 ", "toplevel heading");
            ValidateHeading(middle, "1.1 ", "middle heading");
            ValidateHeading(lowest, "1.1.1 ", "lowest level heading");
        }

        /// <summary>
        /// Test the complicated case of multilevel nested headings with siblings:
        /// 1 Top-level heading
        /// 1.1 nested 1
        /// 2 Another toplevel heading
        /// 3 A third toplevel heading
        /// 3.1 Nested beneath third toplevel heading
        /// 3.1.1 3 levels of nesting
        /// 3.2 Sibling to 3.1
        /// 3.2.1 1st cousin of 3.1.1
        /// 3.2.1.1 3.1.1 is a 1st cousin once removed
        /// 3.2.1.2 Again, 3.1.1 is a 1st cousin once removed
        /// 3.2.2 another 1st cousin of 3.1.1
        /// 3.3 Directly beneath 3
        /// 4 Final toplevel heading
        /// </summary>
        [Test]
        public void TestMultipleNestedSiblingHeadings()
        {
            // 1
            builder.AppendHeading("Top-level heading");

            // Subheadings of 1
            builder.PushSubHeading();

            // 1.1
            builder.AppendHeading("nested 1");

            // Back up to toplevel headings
            builder.PopSubHeading();

            // 2
            builder.AppendHeading("Another toplevel heading");

            // 3
            builder.AppendHeading("A third toplevel heading");

            // Subheadings of 3 (so 3.x)
            builder.PushSubHeading();

            // 3.1
            builder.AppendHeading("Nested beneath third toplevel heading");

            // Subheadings of 3.1 (so 3.1.x)
            builder.PushSubHeading();

            // 3.1.1
            builder.AppendHeading("3 levels of nesting");

            // Back up to 3.x level
            builder.PopSubHeading();

            // 3.2
            builder.AppendHeading("Sibling to 3.1");

            // Subheadings of 3.2 (so 3.2.x)
            builder.PushSubHeading();

            // 3.2.1
            builder.AppendHeading("1st cousin of 3.1.1");

            // Subheadings of 3.2.1 (so 3.2.1.x)
            builder.PushSubHeading();

            // 3.2.1.1
            builder.AppendHeading("3.1.1 is a 1st cousin once removed");

            // 3.2.1.2
            builder.AppendHeading("Again, 3.1.1 is a 1st cousin once removed");

            // Back up to 3.2.x level
            builder.PopSubHeading();

            // 3.2.2
            builder.AppendHeading("another 1st cousin of 3.1.1");

            // Back up to 3.x level
            builder.PopSubHeading();

            // 3.3
            builder.AppendHeading("Directly beneath 3");

            // Back up to toplevel headings.
            builder.PopSubHeading();

            // 4
            builder.AppendHeading("Final toplevel heading");

            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(13), "Section should have thirteen paragraphs");
            var elements = doc.LastSection.Elements;

            ValidateHeading(elements[0] as Paragraph,  "1 ", "Top-level heading");
            ValidateHeading(elements[1] as Paragraph,  "1.1 ", "nested 1");
            ValidateHeading(elements[2] as Paragraph,  "2 ", "Another toplevel heading");
            ValidateHeading(elements[3] as Paragraph,  "3 ", "A third toplevel heading");
            ValidateHeading(elements[4] as Paragraph,  "3.1 ", "Nested beneath third toplevel heading");
            ValidateHeading(elements[5] as Paragraph,  "3.1.1 ", "3 levels of nesting");
            ValidateHeading(elements[6] as Paragraph,  "3.2 ", "Sibling to 3.1");
            ValidateHeading(elements[7] as Paragraph,  "3.2.1 ", "1st cousin of 3.1.1");
            ValidateHeading(elements[8] as Paragraph,  "3.2.1.1 ", "3.1.1 is a 1st cousin once removed");
            ValidateHeading(elements[9] as Paragraph,  "3.2.1.2 ", "Again, 3.1.1 is a 1st cousin once removed");
            ValidateHeading(elements[10] as Paragraph, "3.2.2 ", "another 1st cousin of 3.1.1");
            ValidateHeading(elements[11] as Paragraph, "3.3 ", "Directly beneath 3");
            ValidateHeading(elements[12] as Paragraph, "4 ", "Final toplevel heading");
        }

        /// <summary>
        /// In this test we examine the case of skipped heading levels:
        /// 1 toplevel heading
        /// 1.1.1 what happend to 1.1??
        /// 1.1.2 section 1.1.2
        /// 1.1.2.1.1 section 1.1.2.1.1
        /// 1.1.3 section 1.1.3
        /// 1.2 section 1.2
        /// 2 another toplevel heading
        /// </summary>
        [Test]
        public void TestSkippedHeadingLevels()
        {
            // 1 toplevel heading
            builder.AppendHeading("toplevel heading");

            // Subheadings of 1 (so 1.x)
            builder.PushSubHeading();

            // Subheadings of 1.1 (so 1.1.x)
            builder.PushSubHeading();

            // 1.1.1
            builder.AppendHeading("what happend to 1.1??");

            // 1.1.2
            builder.AppendHeading("section 1.1.2");

            // Subheadings of 1.1.2 (so 1.1.2.x)
            builder.PushSubHeading();

            // Subheadings of 1.1.2.1 (so 1.1.2.1.x)
            builder.PushSubHeading();

            // 1.1.2.1.1
            builder.AppendHeading("section 1.1.2.1.1");

            // Back up to 1.1.2.x
            builder.PopSubHeading();

            // Back up to 1.1.x
            builder.PopSubHeading();

            // 1.1.3 
            builder.AppendHeading("section 1.1.3");

            // Back up to 1.x
            builder.PopSubHeading();

            // 1.2
            builder.AppendHeading("section 1.2");

            // Back up to the toplevel headings
            builder.PopSubHeading();

            // 2
            builder.AppendHeading("another toplevel heading");

            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(7), "Section should have seven paragraphs");
            var elements = doc.LastSection.Elements;

            ValidateHeading(elements[0] as Paragraph,  "1 ", "toplevel heading");
            ValidateHeading(elements[1] as Paragraph,  "1.1.1 ", "what happend to 1.1??");
            ValidateHeading(elements[2] as Paragraph,  "1.1.2 ", "section 1.1.2");
            ValidateHeading(elements[3] as Paragraph,  "1.1.2.1.1 ", "section 1.1.2.1.1");
            ValidateHeading(elements[4] as Paragraph,  "1.1.3 ", "section 1.1.3");
            ValidateHeading(elements[5] as Paragraph,  "1.2 ", "section 1.2");
            ValidateHeading(elements[6] as Paragraph,  "2 ", "another toplevel heading");
        }

        /// <summary>
        /// Negative heading depths are not allowed. Calling PopHeadingLevel()
        /// without first calling PushHeadingLevel() should result in an error.
        /// </summary>
        [Test]
        public void TestNegativeHeadingDepth()
        {
            Assert.Throws<InvalidOperationException>(() => builder.PopSubHeading());
            builder.PushSubHeading();
            builder.PopSubHeading();
            Assert.Throws<InvalidOperationException>(() => builder.PopSubHeading());
        }

        /// <summary>
        /// In this test, we create a subsection with no headings,
        /// and ensure that the next section is rendered as expected:
        /// (increment heading depth)
        /// (decrement heading depth)
        /// 2 section 2
        /// (increment heading depth)
        /// (decrement heading depth)
        /// 3 section 3
        /// </summary>
        /// <remarks>
        /// It's kind of debatable what should actually happen here.
        /// For simplicity, I'm calling this the expected behaviour for now.
        /// </remarks>
        [Test]
        public void TestEmptySubsection()
        {
            builder.PushSubHeading();
            builder.PopSubHeading();
            builder.AppendHeading("section 2");
            builder.PushSubHeading();
            builder.PopSubHeading();
            builder.AppendHeading("section 3");

            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(2), "Section has incorrect number of paragraphs");
            var elements = doc.LastSection.Elements;

            ValidateHeading(elements[0] as Paragraph,  "2 ", "section 2");
            ValidateHeading(elements[1] as Paragraph,  "3 ", "section 3");
        }

        /// <summary>
        /// Ensure that outline level matches heading level.
        /// </summary>
        [Test]
        public void TestOutlineLevelNormalHeading()
        {
            builder.AppendHeading("Heading level 1");
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements.LastObject;
            Assert.That(paragraph.Format.OutlineLevel, Is.EqualTo(OutlineLevel.Level1));

            builder.PushSubHeading();

            builder.AppendHeading("Heading level 2");
            paragraph = (Paragraph)doc.LastSection.Elements.LastObject;
            Assert.That(paragraph.Format.OutlineLevel, Is.EqualTo(OutlineLevel.Level2));

            builder.PushSubHeading();

            builder.AppendHeading("Heading level 3");
            paragraph = (Paragraph)doc.LastSection.Elements.LastObject;
            Assert.That(paragraph.Format.OutlineLevel, Is.EqualTo(OutlineLevel.Level3));

            builder.PushSubHeading();

            builder.AppendHeading("Heading level 4");
            paragraph = (Paragraph)doc.LastSection.Elements.LastObject;
            Assert.That(paragraph.Format.OutlineLevel, Is.EqualTo(OutlineLevel.Level4));

            builder.PushSubHeading();

            builder.AppendHeading("Heading level 5");
            paragraph = (Paragraph)doc.LastSection.Elements.LastObject;
            Assert.That(paragraph.Format.OutlineLevel, Is.EqualTo(OutlineLevel.Level5));

            builder.PushSubHeading();

            builder.AppendHeading("Heading level 6");
            paragraph = (Paragraph)doc.LastSection.Elements.LastObject;
            Assert.That(paragraph.Format.OutlineLevel, Is.EqualTo(OutlineLevel.Level6));
        }

        /// <summary>
        /// Ensure that the outline level is set correctly when mixing calls to
        /// <see cref="PdfBuilder.PushSubHeading()"/> with calls to
        /// <see cref="PdfBuilder.SetHeadingLevel(uint)"/>.
        /// </summary>
        [Test]
        public void TestOutlineLevelMixedHeadings()
        {
            builder.AppendHeading("Toplevel heading");

            builder.PushSubHeading();
            builder.SetHeadingLevel(2);
            builder.AppendText("Nested heading", TextStyle.Normal);
            builder.ClearHeadingLevel();
            builder.PopSubHeading();

            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(2), "Section has incorrect # paragraphs");
            Paragraph paragraph0 = (Paragraph)doc.LastSection.Elements[0];
            Paragraph paragraph1 = (Paragraph)doc.LastSection.Elements[1];
            Assert.That(paragraph0.Format.OutlineLevel, Is.EqualTo(OutlineLevel.Level1));
            Assert.That(paragraph1.Format.OutlineLevel, Is.EqualTo(OutlineLevel.Level2));
        }

        /// <summary>
        /// Ensure that the outline level is set correctly when mixing calls to
        /// <see cref="PdfBuilder.PushSubHeading()"/> with calls to
        /// <see cref="PdfBuilder.SetHeadingLevel(uint)"/>.
        /// </summary>
        [Test]
        public void TestOutlineLevelRelativeHeadings()
        {
            builder.AppendHeading("Toplevel heading");

            builder.PushSubHeading();
            builder.SetHeadingLevel(1);
            builder.AppendText("Nested heading", TextStyle.Normal);
            builder.ClearHeadingLevel();
            builder.PopSubHeading();

            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(2), "Section has incorrect # paragraphs");
            Paragraph paragraph0 = (Paragraph)doc.LastSection.Elements[0];
            Paragraph paragraph1 = (Paragraph)doc.LastSection.Elements[1];
            Assert.That(paragraph0.Format.OutlineLevel, Is.EqualTo(OutlineLevel.Level1));
            Assert.That(paragraph1.Format.OutlineLevel, Is.EqualTo(OutlineLevel.Level2));
        }

        /// <summary>
        /// Ensure that the given paragraph contains only a heading, with
        /// the specified heading indices and text. This does not verify
        /// the heading text's style (bold/font size/heading level/etc).
        /// </summary>
        /// <param name="paragraph">The paragraph. An appropriate error will be given if this is null.</param>
        /// <param name="expectedIndices">The expected heading indices. E.g. "1.3.2 "</param>
        /// <param name="expectedHeadingText">The actual heading text not including the indices.</param>
        private void ValidateHeading(Paragraph paragraph, string expectedIndices, string expectedHeadingText)
        {
            Assert.That(paragraph, Is.Not.Null, "Heading was not written to a paragraph object");

            Assert.That(paragraph.Elements.Count, Is.EqualTo(3));
            FormattedText indices = paragraph.Elements[0] as FormattedText;
            FormattedText heading = paragraph.Elements[1] as FormattedText;
            BookmarkField bookmark = paragraph.Elements[2] as BookmarkField;

            Assert.That(indices, Is.Not.Null, "Heading indices were not written to document");
            Assert.That(heading, Is.Not.Null, "Heading text was not written to document");
            Assert.That(bookmark, Is.Not.Null, "Heading text was not written as a bookmark");

            Assert.That(indices.Elements.Count, Is.EqualTo(1), "Heading indices should be a single text element");
            Assert.That(heading.Elements.Count, Is.EqualTo(1), "Heading text should be a single text element");

            Text indicesText = indices.Elements.LastObject as Text;
            Text headingText = heading.Elements.LastObject as Text;

            Assert.That(indices, Is.Not.Null, "Heading indices were not written");
            Assert.That(heading, Is.Not.Null, "Heading text was not written");

            Assert.That(indicesText.Content, Is.EqualTo(expectedIndices), "Heading index is incorrect");
            Assert.That(headingText.Content, Is.EqualTo(expectedHeadingText), "Heading text is incorrect");
            Assert.That(bookmark.Name, Is.EqualTo($"#{expectedHeadingText}"));
        }
    }
}
