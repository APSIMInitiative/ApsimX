using System;
using NUnit.Framework;
using System.Linq;
using System.Text;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;
using System.Collections.Generic;
using APSIM.Interop.Documentation.Renderers;
using TextStyle = APSIM.Interop.Markdown.TextStyle;
using Document = MigraDocCore.DocumentObjectModel.Document;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using Section = APSIM.Shared.Documentation.Section;
using APSIM.Interop.Documentation.Extensions;

namespace UnitTests.Interop.Documentation.TagRenderers
{
    /// <summary>
    /// Tests for the <see cref="Section"/> class.
    /// </summary>
    /// <remarks>
    /// todo: mock out PdfBuilder API.
    /// </remarks>
    [TestFixture]
    public class SectionTagRendererTests
    {
        private PdfBuilder pdfBuilder;
        private Document document;
        private SectionTagRenderer renderer;

        [SetUp]
        public void SetUp()
        {
            document = new MigraDocCore.DocumentObjectModel.Document();
            pdfBuilder = new PdfBuilder(document, PdfOptions.Default);
            pdfBuilder.UseTagRenderer(new MockTagRenderer());
            renderer = new SectionTagRenderer();
        }

        /// <summary>
        /// A section with an empty title should be rendered to the document
        /// without a heading.
        /// </summary>
        [TestCase("")]
        [TestCase(null)]
        public void TestEmptyTitle(string title)
        {
            string text = "Test message";
            Section section = new Section(title, new MockTag(p => p.AppendText(text, TextStyle.Normal)));
            renderer.Render(section, pdfBuilder);
            Assert.That(document.Sections.Count, Is.EqualTo(1));
            Assert.That(document.LastSection.Elements.OfType<Paragraph>().Count(), Is.EqualTo(1), $"Incorrect number of paragraphs when title is set to {(title == null ? "null" : $"'{title}'")}");
            Assert.That(document.LastSection.LastParagraph.GetRawText(), Is.EqualTo(text));
        }

        /// <summary>
        /// Ensure that all children of the section are
        /// rendered (in addition to the title).
        /// </summary>
        /// <param name="numChildren">A section with this many children will be tested.</param>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EnsureChildrenAreWritten(int numChildren)
        {
            string title = "Section title";
            StringBuilder paragraphText = new StringBuilder();
            List<ITag> tags = new List<ITag>();
            for (int i = 0; i < numChildren; i++)
            {
                string text = $"paragraph text {i}";
                paragraphText.Append(text);
                tags.Add(new MockTag(p => p.AppendText(text, TextStyle.Normal)));
            }

            Section section = new Section(title, tags);
            renderer.Render(section, pdfBuilder);

            if (numChildren < 1)
                Assert.That(document.LastSection, Is.Null);
            else
            {
                List<Paragraph> paragraphs = document.LastSection.Elements.OfType<Paragraph>().ToList();

                // There should be 1 paragraph for title, plus 1 more paragraph if there are
                // any children of this section.
                Assert.That(paragraphs.Count, Is.EqualTo(2));
                Assert.That(paragraphs[0].GetRawText(), Is.EqualTo($"1 {title}"));

                if (numChildren > 0)
                    Assert.That(paragraphs[1].GetRawText(), Is.EqualTo(paragraphText.ToString()));
            }
        }

        /// <summary>
        /// Ensure that child tags' headings are subheadings (ie 1.X instead of 2).
        /// </summary>
        [Test]
        public void EnsureChildrenUseSubheadings()
        {
            string title = "Section title";
            string childTitle = "subsection";
            Section section = new Section(title, new MockTag(p => p.AppendHeading(childTitle)));
            renderer.Render(section, pdfBuilder);

            List<Paragraph> paragraphs = document.LastSection.Elements.OfType<Paragraph>().ToList();
            Assert.That(paragraphs.Count, Is.EqualTo(2));
            Assert.That(paragraphs[0].GetRawText(), Is.EqualTo($"1 {title}"));
            Assert.That(paragraphs[1].GetRawText(), Is.EqualTo($"1.1 {childTitle}"));
        }

        /// <summary>
        /// Ensure that serial sections write to the same heading level.
        /// </summary>
        [Test]
        public void HeadingLevelSerialSections()
        {
            string title1 = "section 1";
            string title2 = "section 2";

            Section section1 = new Section(title1, new MockTag(p => { }));
            Section section2 = new Section(title2, new MockTag(p => { }));

            renderer.Render(section1, pdfBuilder);
            renderer.Render(section2, pdfBuilder);

            List<Paragraph> paragraphs = document.LastSection.Elements.OfType<Paragraph>().ToList();
            Assert.That(paragraphs.Count, Is.EqualTo(2));
            Assert.That(paragraphs[0].GetRawText(), Is.EqualTo($"1 {title1}"));
            Assert.That(paragraphs[1].GetRawText(), Is.EqualTo($"2 {title2}"));
        }
    }
}
