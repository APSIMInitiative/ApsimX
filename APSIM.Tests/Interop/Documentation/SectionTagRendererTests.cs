using System;
using NUnit.Framework;
using APSIM.Server.IO;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Services.Documentation;
using System.Collections.Generic;
using APSIM.Interop.Documentation.Renderers;
using TextStyle = APSIM.Interop.Markdown.TextStyle;
using Document = MigraDocCore.DocumentObjectModel.Document;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using Section = APSIM.Services.Documentation.Section;
using APSIM.Interop.Documentation.Extensions;
using MigraDocCore.DocumentObjectModel;

namespace APSIM.Tests.Interop.Documentation
{
    /// <summary>
    /// Tests for the <see cref="PdfBuilder"/> class.
    /// </summary>
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
            Assert.AreEqual(1, document.Sections.Count);
            Assert.AreEqual(1, document.LastSection.Elements.OfType<Paragraph>().Count(), $"Incorrect number of paragraphs when title is set to {(title == null ? "null" : $"'{title}'")}");
            Assert.AreEqual(text, document.LastSection.LastParagraph.GetRawText());
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
                Assert.Null(document.LastSection);
            else
            {
                List<Paragraph> paragraphs = document.LastSection.Elements?.OfType<Paragraph>()?.ToList();

                // There should be 1 paragraph for title, plus 1 more paragraph if there are
                // any children of this section.
                Assert.AreEqual(2, paragraphs.Count);
                Assert.AreEqual($"1 {title}", paragraphs[0].GetRawText());

                if (numChildren > 0)
                    Assert.AreEqual(paragraphText.ToString(), paragraphs[1].GetRawText());
            }
        }
    }
}
