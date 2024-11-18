using APSIM.Interop.Documentation;
using APSIM.Interop.Documentation.Helpers;
using APSIM.Interop.Markdown;
using APSIM.Interop.Markdown.Renderers;
using MigraDocCore.DocumentObjectModel;
using Moq;
using NUnit.Framework;
using System;

namespace UnitTests.Interop.Documentation
{
    /// <summary>
    /// Unit tests for <see cref="PdfBuilder.AppendReference(string, TextStyle)"/>.
    /// </summary>
    [TestFixture]
    public class PdfBuilderReferenceTests
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
        Mock<ICitationHelper> citationResolver;

        /// <summary>
        /// Initialise the PDF buidler and its document.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            doc = new Document();
            citationResolver = new Mock<ICitationHelper>();
            builder = new PdfBuilder(doc, new PdfOptions(null, citationResolver.Object));
        }

        /// <summary>
        /// Ensure that an exception will be thrown immediately if
        /// <see cref="PdfBuilder"/> receives a null citation resolver.
        /// </summary>
        [Test]
        public void EnsureNullCitationHelperIsAllowed()
        {
            Assert.DoesNotThrow(() => new PdfBuilder(doc, new PdfOptions(null, null)));
        }

        /// <summary>
        /// Ensure that the citation resolver passed in via the
        /// <see cref="PdfOptions"/> instance is used to resolve references.
        /// </summary>
        [Test]
        public void TestUseCustomBibFile()
        {
            string reference = "custom_reference";
            AddCitation(reference);
            citationResolver.Setup(r => r.Lookup(It.IsNotIn<string>(reference))).Throws<NotImplementedException>();
            Assert.DoesNotThrow(() => builder.AppendReference(reference, TextStyle.Normal));
        }

        /// <summary>
        /// Ensure that references to citations without a URL are linked to
        /// the bibliography.
        /// </summary>
        [Test]
        public void TestAppendReferenceWithoutUrl()
        {
            string reference = "reference *name*";
            string inTextCite = "in-text citation words";
            Mock<ICitation> citation = AddCitation(reference, inTextCite);

            Assert.DoesNotThrow(() => builder.AppendReference(reference, TextStyle.Normal));
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Hyperlink link = (Hyperlink)paragraph.Elements[0];
            Assert.That(link.Name, Is.EqualTo(reference));
        }

        /// <summary>
        /// Ensure taht references to citations with a URL are linked to the
        /// full citation in the bibliography, which is linked to the URL.
        /// </summary>
        [Test]
        public void TestAppendReferenceWithUrl()
        {
            string reference = "ref";
            string url = "link uri";
            Mock<ICitation> citation = AddCitation(reference);
            citation.Setup(c => c.URL).Returns(url);

            builder.AppendReference(reference, TextStyle.Normal);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Hyperlink link = (Hyperlink)paragraph.Elements[0];
            Assert.That(link.Name, Is.EqualTo(reference));
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendReference(string, TextStyle)"/>
        /// will insert invalid references as plaintext between square brackets
        /// if no matching reference is found in the citation helper.
        /// </summary>
        [Test]
        public void TestAppendInvalidReference()
        {
            string reference = "some invalid reference";
            builder.AppendReference(reference, TextStyle.Normal);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            FormattedText formatted = (FormattedText)paragraph.Elements[0];
            Text text = (Text)formatted.Elements[0];
            Assert.That(text.Content, Is.EqualTo($"[{reference}]"));
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendReference(string, TextStyle)"/>
        /// creates a bookmark link, rather than a web link.
        /// </summary>
        [Test]
        public void TestLinkType()
        {
            string name = "reference";
            AddCitation(name);
            builder.AppendReference(name, TextStyle.Normal);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Hyperlink link = (Hyperlink)paragraph.Elements[0];
            Assert.That(link.Type, Is.EqualTo(HyperlinkType.Bookmark));
        }

        /// <summary>
        /// Ensure that we use the citation's in-text property as the text
        /// for the created hyperlink.
        /// </summary>
        [Test]
        public void TestAppendReferenceText()
        {
            string reference = "a reference";
            string inText = "in-text citation";
            AddCitation(reference, inText);

            builder.AppendReference(reference, TextStyle.Normal);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Hyperlink link = (Hyperlink)paragraph.Elements[0];
            FormattedText formatted = (FormattedText)link.Elements[0];
            Text text = (Text)formatted.Elements[0];
            Assert.That(text.Content, Is.EqualTo(inText));
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendReference(string, TextStyle)"/>
        /// respects the given style for valid references.
        /// </summary>
        [Test]
        public void TestAppendValidReferenceStyle()
        {
            string reference = "a reference";
            string inText = "in-text citation";
            AddCitation(reference, inText);

            builder.AppendReference(reference, TextStyle.Italic);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Hyperlink link = (Hyperlink)paragraph.Elements[0];
            FormattedText formatted = (FormattedText)link.Elements[0];
            Style style = doc.Styles[formatted.Style];
            Assert.That(style.Font.Italic, Is.True);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendReference(string, TextStyle)"/>
        /// respects the given style for invalid references.
        /// </summary>
        [Test]
        public void TestAppendInalidReferenceStyle()
        {
            builder.AppendReference("a reference", TextStyle.Strong);
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            FormattedText formatted = (FormattedText)paragraph.Elements[0];
            Style style = doc.Styles[formatted.Style];
            Assert.That(style.Font.Bold, Is.True);
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendReference(string, TextStyle)"/>
        /// will insert the text in the final paragraph of the document.
        /// </summary>
        [Test]
        public void TestAppendValidReferenceCorrectParagraph()
        {
            builder.AppendText("some text", TextStyle.Normal);
            builder.StartNewParagraph();

            string reference = "a reference";
            AddCitation(reference, "in-text citation");

            builder.AppendReference(reference, TextStyle.Normal);
            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(2));
            Assert.That(doc.LastSection.Elements[0].GetType(), Is.EqualTo(typeof(Paragraph)));
            Assert.That(doc.LastSection.Elements[1].GetType(), Is.EqualTo(typeof(Paragraph)));
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendReference(string, TextStyle)"/>
        /// will insert the text in the final paragraph of the document for invalid
        /// references.
        /// </summary>
        [Test]
        public void TestAppendInvalidReferenceCorrectParagraph()
        {
            builder.AppendText("some text", TextStyle.Normal);
            builder.StartNewParagraph();

            builder.AppendReference("a reference", TextStyle.Normal);
            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(2));
            Assert.That(doc.LastSection.Elements[0].GetType(), Is.EqualTo(typeof(Paragraph)));
            Assert.That(doc.LastSection.Elements[1].GetType(), Is.EqualTo(typeof(Paragraph)));
        }

        /// <summary>
        /// Ensure that we can have multiple references to the same paper
        /// in a document.
        /// </summary>
        [Test]
        public void TestMultipleReferencesToSamePaper()
        {
            string reference = "refname";
            string inText = "in-txt";
            AddCitation(reference, inText);
            builder.AppendReference(reference, TextStyle.Normal);
            builder.AppendReference(reference, TextStyle.Normal);

            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(2));
            Assert.That(paragraph.Elements[0].GetType(), Is.EqualTo(typeof(Hyperlink)));
            Assert.That(paragraph.Elements[1].GetType(), Is.EqualTo(typeof(Hyperlink)));

            for (int i = 0; i < 2; i++)
            {
                Hyperlink link = (Hyperlink)paragraph.Elements[i];
                Assert.That(link.Name, Is.EqualTo(reference));
                Assert.That(link.Elements.Count, Is.EqualTo(1));
                FormattedText formatted = (FormattedText)link.Elements[0];
                Text text = (Text)formatted.Elements[0];
                Assert.That(text.Content, Is.EqualTo(inText));
            }
        }

        /// <summary>
        /// Ensure that we can have multiple references to different papers
        /// in a document.
        /// </summary>
        [Test]
        public void TestMultipleReferencesToDifferentPapers()
        {
            string[] references = new string[2]
            {
                "reference0",
                "reference1"
            };
            string[] inTexts = new string[2]
            {
                "in-txt0",
                "in-txt1"
            };
            for (int i = 0; i < 2; i++)
                AddCitation(references[i], inTexts[i]);

            builder.AppendReference(references[0], TextStyle.Normal);
            builder.AppendReference(references[1], TextStyle.Normal);

            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(2));
            Assert.That(paragraph.Elements[0].GetType(), Is.EqualTo(typeof(Hyperlink)));
            Assert.That(paragraph.Elements[1].GetType(), Is.EqualTo(typeof(Hyperlink)));

            for (int i = 0; i < 2; i++)
            {
                Hyperlink link = (Hyperlink)paragraph.Elements[i];
                Assert.That(link.Name, Is.EqualTo(references[i]));
                Assert.That(link.Elements.Count, Is.EqualTo(1));
                FormattedText formatted = (FormattedText)link.Elements[0];
                Text text = (Text)formatted.Elements[0];
                Assert.That(text.Content, Is.EqualTo(inTexts[i]));
            }
        }

        /// <summary>
        /// Ensure that any content added after a reference goes in the same
        /// paragraph as the reference, but is not part of the reference.
        /// </summary>
        [Test]
        public void TestContentAfterReference()
        {
            string reference = "reference name";
            AddCitation(reference);
            builder.AppendReference(reference, TextStyle.Normal);
            builder.AppendText("extra content", TextStyle.Normal);
            Assert.That(doc.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)doc.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(2));
            Assert.That(paragraph.Elements[0].GetType(), Is.EqualTo(typeof(Hyperlink)));
            Assert.That(paragraph.Elements[1].GetType(), Is.EqualTo(typeof(FormattedText)));
        }

        /// <summary>
        /// Add a citation to the list of citations accessed by the PdfBuilder.
        /// </summary>
        /// <param name="name">Name of the citation.</param>
        private Mock<ICitation> AddCitation(string name, string inTextCite = "")
        {
            Mock<ICitation> citation = new Mock<ICitation>();
            citation.Setup(c => c.InTextCite).Returns(inTextCite);
            citationResolver.Setup(r => r.Lookup(name)).Returns(citation.Object);
            return citation;
        }
    }
}
