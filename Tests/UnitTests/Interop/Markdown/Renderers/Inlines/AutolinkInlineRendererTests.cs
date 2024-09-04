using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Interop.Markdown.Renderers.Inlines;
using Markdig.Syntax.Inlines;
using APSIM.Interop.Markdown;
using MigraDocCore.DocumentObjectModel;
using Moq;

namespace UnitTests.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// Tests for <see cref="AutolinkInlineRenderer"/>.
    /// </summary>
    [TestFixture]
    public class AutolinkInlineRendererTests
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
        /// The <see cref="AutolinkInlineRenderer"/> instance being tested.
        /// </summary>
        private AutolinkInlineRenderer renderer;

        /// <summary>
        /// A sample autolink which may be used by tests.
        /// </summary>
        private AutolinkInline autolink;

        /// <summary>
        /// URI of the sample autolink.
        /// </summary>
        private string sampleUri;

        /// <summary>
        /// Initialise the testing environment.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            document = new Document();
            // Workaround for a quirk in the migradoc API.
            _ = document.AddSection().Elements;
            pdfBuilder = new PdfBuilder(document, PdfOptions.Default);
            renderer = new AutolinkInlineRenderer();
            sampleUri = "theurl";
            autolink = new AutolinkInline(sampleUri);
        }

        /// <summary>
        /// Ensure that email autolink URIs are prefixed with "mailto:".
        /// </summary>
        [Test]
        public void TestEmailUriPrefix()
        {
            autolink.IsEmail = true;
            string expected = $"mailto:{sampleUri}";
            Mock<PdfBuilder> builder = new Mock<PdfBuilder>(document, PdfOptions.Default);
            builder.Setup(b => b.SetLinkState(It.IsAny<string>()))
                   .Callback<string>(uri => Assert.That(uri, Is.EqualTo(expected)))
                   .CallBase();
            renderer.Write(builder.Object, autolink);
        }

        /// <summary>
        /// Ensure that non-email URIs are added appropriately.
        /// </summary>
        [Test]
        public void TestNonEmailUriPrefix()
        {
            autolink.IsEmail = false;
            string expected = sampleUri;
            Mock<PdfBuilder> builder = new Mock<PdfBuilder>(document, PdfOptions.Default);
            builder.Setup(b => b.SetLinkState(It.IsAny<string>()))
                   .Callback<string>(uri => Assert.That(uri, Is.EqualTo(expected)))
                   .CallBase();
            renderer.Write(builder.Object, autolink);
        }

        /// <summary>
        /// Ensure that the link uri/styling is applied to all children.
        /// </summary>
        [Test]
        public void EnsureLinkTextIsInserted()
        {
            renderer.Write(pdfBuilder, autolink);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(1));
            Hyperlink inserted = (Hyperlink)paragraph.Elements[0];
            Assert.That(inserted.Elements.Count, Is.EqualTo(1));
            FormattedText text = (FormattedText)inserted.Elements[0];
            Assert.That(text.Elements.Count, Is.EqualTo(1));
            Text rawText = (Text)text.Elements[0];
            Assert.That(rawText.Content, Is.EqualTo(sampleUri));
        }

        /// <summary>
        /// Ensure that any subsequent additions go into the same paragraph.
        /// </summary>
        [Test]
        public void EnsureSubsequentAdditionsInSameParagraph()
        {
            renderer.Write(pdfBuilder, autolink);
            pdfBuilder.AppendText("subsequent addition", TextStyle.Normal);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Ensure that subsequent additions are not included in the link.
        /// </summary>
        [Test]
        public void EnsureSubsequentAdditionsNotInLink()
        {
            renderer.Write(pdfBuilder, autolink);
            pdfBuilder.AppendText("supplemental material", TextStyle.Normal);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Ensure that the autolink contents are written to an existing
        /// paragraph (if there is one).
        /// </summary>
        [Test]
        public void EnsureContentGoesInExistingParagraph()
        {
            pdfBuilder.AppendText("supplemental material", TextStyle.Normal);
            renderer.Write(pdfBuilder, autolink);
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(1));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            Assert.That(paragraph.Elements.Count, Is.EqualTo(2));
        }
    }
}
