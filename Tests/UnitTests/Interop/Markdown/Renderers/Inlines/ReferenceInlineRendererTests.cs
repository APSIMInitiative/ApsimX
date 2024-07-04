using NUnit.Framework;
using System;
using Markdig.Parsers;
using APSIM.Interop.Markdown.Parsers.Inlines;
using Moq;
using Markdig.Helpers;
using Markdig.Syntax.Inlines;
using APSIM.Interop.Markdown.Renderers.Inlines;
using Markdig.Syntax;
using Markdig;
using MigraDocCore.DocumentObjectModel;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Inlines;
using APSIM.Interop.Documentation.Helpers;
using System.Collections.Generic;
using APSIM.Interop.Markdown;

namespace UnitTests.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// Tests for <see cref="ReferenceInlineRenderer"/>.
    /// </summary>
    [TestFixture]
    public class ReferenceInlineRendererTests
    {
        /// <summary>
        /// PDF Builder API instance.
        /// </summary>
        private PdfBuilder builder;

        /// <summary>
        /// MigraDoc document to which the renderer will write.
        /// </summary>
        private Document document;

        /// <summary>
        /// The <see cref="ReferenceInlineRenderer"/> instance being tested.
        /// </summary>
        private ReferenceInlineRenderer renderer;

        /// <summary>
        /// Citations which will be read by the pdf builder.
        /// </summary>
        private Dictionary<string, ICitation> citations;

        /// <summary>
        /// Initialise the testing environment.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            document = new Document();
            // Workaround for a quirk in the migradoc API.
            _ = document.AddSection().Elements;

            citations = new Dictionary<string, ICitation>();

            Mock<ICitationHelper> citationHelper = new Mock<ICitationHelper>();
            citationHelper.Setup(c => c.Lookup(It.IsAny<string>())).Returns<string>(s => citations[s]);

            builder = new PdfBuilder(document, new PdfOptions("", citationHelper.Object));
            renderer = new ReferenceInlineRenderer();
        }

        /// <summary>
        /// Ensure that the render method appends a reference to the pdf document.
        /// </summary>
        [Test]
        public void TestRender()
        {
            string inText = "In text citation value";
            string uri = "link uri";
            string reference = "reference_name";

            Mock<ICitation> citation = new Mock<ICitation>();
            citation.Setup(c => c.InTextCite).Returns(inText);
            citation.Setup(c => c.URL).Returns(uri);
            citation.Setup(c => c.Name).Returns(reference);
            citations.Add(reference, citation.Object);

            ReferenceInline inline = new ReferenceInline(reference);
            renderer.Write(builder, inline);

            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(1));
            Hyperlink link = (Hyperlink)paragraph.Elements[0];
            Assert.That(link.Elements.Count, Is.EqualTo(1));
            FormattedText formatted = (FormattedText)link.Elements[0];
            Assert.That(formatted.Elements.Count, Is.EqualTo(1));
            Text text = (Text)formatted.Elements[0];
            Assert.That(text.Content, Is.EqualTo(inText));
        }

        /// <summary>
        /// Test the name of the reference passed into <see cref="PdfBuilder.AppendReference"/>.
        /// </summary>
        [Test]
        public void TestReferenceName()
        {
            string referenceName = "reference name";
            Mock<PdfBuilder> mockBuilder = new Mock<PdfBuilder>(document, new PdfOptions("", new Mock<ICitationHelper>().Object));
            mockBuilder.Setup(b => b.AppendReference(It.IsAny<string>(), It.IsAny<TextStyle>())).Callback<string, TextStyle>((r, style) =>
            {
                Assert.That(r, Is.EqualTo(referenceName));
            });
            renderer.Write(mockBuilder.Object, new ReferenceInline(referenceName));
            Assert.That(TestContext.CurrentContext.AssertCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Ensure that references are rendered with the correct style.
        /// </summary>
        [Test]
        public void TestStyle()
        {
            Mock<PdfBuilder> mockBuilder = new Mock<PdfBuilder>(document, new PdfOptions("", new Mock<ICitationHelper>().Object));
            mockBuilder.Setup(b => b.AppendReference(It.IsAny<string>(), It.IsAny<TextStyle>())).Callback<string, TextStyle>((s, style) =>
            {
                Assert.That(style, Is.EqualTo(TextStyle.Normal));
            });
            renderer.Write(mockBuilder.Object, new ReferenceInline(""));
            Assert.That(TestContext.CurrentContext.AssertCount, Is.EqualTo(1));
        }
    }
}
