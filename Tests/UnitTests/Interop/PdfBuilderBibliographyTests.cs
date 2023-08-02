using APSIM.Interop.Documentation;
using APSIM.Interop.Documentation.Helpers;
using APSIM.Interop.Markdown;
using APSIM.Interop.Markdown.Renderers;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Fields;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests.Interop.Documentation
{
    /// <summary>
    /// Unit tests for <see cref="PdfBuilder.AppendReference(string, TextStyle)"/>.
    /// </summary>
    [TestFixture]
    public class PdfBuilderBibliographyTests
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
        /// Mock <see cref="ICitationHelper"/> instance used for tests.
        /// </summary>
        private Mock<ICitationHelper> citationResolver;

        /// <summary>
        /// Lookup table of citations used by the pdf builder.
        /// </summary>
        private Dictionary<string, ICitation> citations;

        /// <summary>
        /// Initialise the PDF buidler and its document.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            doc = new Document();
            citations = new Dictionary<string, ICitation>();
            citationResolver = new Mock<ICitationHelper>();
            citationResolver.Setup(c => c.Lookup(It.IsAny<string>())).Returns<string>(n => { citations.TryGetValue(n, out ICitation result); return result; });
            builder = new PdfBuilder(doc, new PdfOptions(null, citationResolver.Object));
        }

        /// <summary>
        /// Ensure that each reference in the bibliography gets its own paragraph,
        /// and that bibliography contents are not appended to any existing paragraphs.
        /// </summary>
        [Test]
        public void EnsureOneParagraphPerCitation()
        {
            AddCitation("cite0", "fullText0");
            AddCitation("cite1", "fullText1");
            foreach ((string name, ICitation _) in citations)
                builder.AppendReference(name, TextStyle.Normal);
            builder.WriteBibliography();
            // Should be 4 elements in the document - 1 paragraph containing the two
            // citations above, second paragraph for bibliography heading, then two
            // more paragraphs, one for each reference in the bibliography.
            Assert.AreEqual(4, doc.LastSection.Elements.Count);
        }

        /// <summary>
        /// Ensure that bibliography contents are sorted alphabetically.
        /// </summary>
        [Test]
        public void EnsureBibliographyIsSorted()
        {
            AddCitation("citation #0", "full text 1 <- should appear second");
            AddCitation("citation #1", "full text 0 <- should appear first");

            foreach ((string name, ICitation _) in citations)
                builder.AppendReference(name, TextStyle.Normal);
            builder.WriteBibliography();

            Assert.AreEqual(4, doc.LastSection.Elements.Count);
            Paragraph paragraph0 = (Paragraph)doc.LastSection.Elements[2];
            Paragraph paragraph1 = (Paragraph)doc.LastSection.Elements[3];

            FormattedText formatted0 = (FormattedText)paragraph0.Elements[0];
            FormattedText formatted1 = (FormattedText)paragraph1.Elements[0];

            Text text0 = (Text)formatted0.Elements[0];
            Text text1 = (Text)formatted1.Elements[0];

            Assert.AreEqual(citations.Last().Value.BibliographyText, text0.Content);
            Assert.AreEqual(citations.First().Value.BibliographyText, text1.Content);
        }

        /// <summary>
        /// Ensure that references with a URL are written to bibliography
        /// as a hyperlink.
        /// </summary>
        [Test]
        public void EnsureLinkIsWritten()
        {
            string name = "citationname";
            string url = "citation url";
            Mock<ICitation> citation = AddCitation(name, "bibliography text");
            citation.Setup(c => c.URL).Returns(url);

            builder.AppendReference(name, TextStyle.Normal);
            builder.WriteBibliography();

            Paragraph bibliography = (Paragraph)doc.LastSection.Elements[2];
            Assert.AreEqual(typeof(Hyperlink), bibliography.Elements[0].GetType());
            Hyperlink link = (Hyperlink)bibliography.Elements[0];
            Assert.AreEqual(url, link.Name);
        }

        /// <summary>
        /// Ensure that references without a URL are not written to the
        /// bibliography as a hyperlink.
        /// </summary>
        [Test]
        public void EnsureNoLink()
        {
            string name = "citationname";
            AddCitation(name, "bibliography text");

            builder.AppendReference(name, TextStyle.Normal);
            builder.WriteBibliography();

            Paragraph bibliography = (Paragraph)doc.LastSection.Elements[1];
            Assert.AreEqual(typeof(FormattedText), bibliography.Elements[0].GetType());
        }

        /// <summary>
        /// Ensure that bibliography contents are written with normal
        /// text style.
        /// </summary>
        [Test]
        public void TestStyle()
        {
            string name = "citationname";
            AddCitation(name, "bibliography text");

            builder.AppendReference(name, TextStyle.Normal);
            builder.WriteBibliography();

            Paragraph bibliography = (Paragraph)doc.LastSection.Elements[2];
            Assert.AreEqual(typeof(FormattedText), bibliography.Elements[0].GetType());
            FormattedText formatted = (FormattedText)bibliography.Elements[0];
            Assert.AreEqual("Bibliography", formatted.Style);
        }

        /// <summary>
        /// Ensure that each bibliography entry is given a bookmark
        /// which comes from the short name of the reference.
        /// </summary>
        [Test]
        public void EnsureBookmarkIsWritten()
        {
            string name = "citation_name";
            AddCitation(name, "full citation text");
            builder.AppendReference(name, TextStyle.Normal);
            builder.WriteBibliography();
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[2];
            Assert.AreEqual(typeof(BookmarkField), paragraph.Elements[1].GetType());
            BookmarkField bookmark = (BookmarkField)paragraph.Elements[1];
            Assert.AreEqual(name, bookmark.Name);
        }

        /// <summary>
        /// Ensure that each citation
        /// </summary>
        [Test]
        public void EnsureFullCitationTextIsWritten()
        {
            string name = "citation name";
            string citation = "full citation text";
            AddCitation(name, citation);

            builder.AppendReference(name, TextStyle.Normal);
            builder.WriteBibliography();

            Assert.AreEqual(3, doc.LastSection.Elements.Count);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[2];
            Assert.GreaterOrEqual(paragraph.Elements.Count, 1);
            FormattedText formatted = (FormattedText)paragraph.Elements[0];
            Assert.AreEqual(1, formatted.Elements.Count);
            Text text = (Text)formatted.Elements[0];
            Assert.AreEqual(citation, text.Content);
        }

        /// <summary>
        /// Ensure that a heading is written before the bibliography.
        /// </summary>
        [Test]
        public void EnsureHeadingIsWritten()
        {
            string name = "a citation";
            AddCitation(name, "this is the full citation text");
            builder.AppendReference(name, TextStyle.Normal);
            builder.WriteBibliography();
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[1];
            Assert.AreEqual(typeof(FormattedText), paragraph.Elements[0].GetType());
            FormattedText formatted = (FormattedText)paragraph.Elements[1];
            Style style = doc.Styles[formatted.Style];
            Assert.Greater(style.Font.Size, doc.Styles.Normal.Font.Size);
            Assert.AreEqual(typeof(Text), formatted.Elements[0].GetType());
            Text text = (Text)formatted.Elements[0];
            Assert.AreEqual("References", text.Content);
        }

        /// <summary>
        /// Ensure that no heading or bibliography is written if the document
        /// contains no references.
        /// </summary>
        [Test]
        public void DontWriteEmptyBibliography()
        {
            builder.AppendReference("a citation", TextStyle.Normal);
            builder.WriteBibliography();
            Assert.AreEqual(1, doc.LastSection.Elements.Count);
        }

        /// <summary>
        /// Ensure that citations are inserted with bibliography style.
        /// </summary>
        [Test]
        public void TestBibliographyStyle()
        {
            string name = "a citation";
            string citation = "this is the full citation text";
            AddCitation(name, citation);

            Mock<PdfBuilder> mockBuilder = new Mock<PdfBuilder>(doc, new PdfOptions("", citationResolver.Object));
            mockBuilder.Setup(b => b.AppendText(citation, It.IsAny<TextStyle>()))
                       .Callback<string, TextStyle>((_, style) => Assert.AreEqual(TextStyle.Bibliography, style))
                       .CallBase();
            mockBuilder.Setup(b => b.AppendReference(It.IsAny<string>(), It.IsAny<TextStyle>())).CallBase();

            mockBuilder.Object.AppendReference(name, TextStyle.Normal);
            mockBuilder.Object.WriteBibliography();

            // Sanity check for the above plumbing code.
            Assert.AreEqual(1, TestContext.CurrentContext.AssertCount);
        }

        /// <summary>
        /// Add a citation with no details.
        /// </summary>
        /// <param name="name">Citation/reference name.</param>
        /// <param name="fullText">Full text of the citation as it should appear in the bibliography.</param>
        private Mock<ICitation> AddCitation(string name, string fullText)
        {
            Mock<ICitation> citation = new Mock<ICitation>();
            citation.Setup(c => c.Name).Returns(name);
            citation.Setup(c => c.InTextCite).Returns("in-text citation");
            citation.Setup(c => c.BibliographyText).Returns(fullText);
            citations.Add(name, citation.Object);
            return citation;
        }
    }
}
