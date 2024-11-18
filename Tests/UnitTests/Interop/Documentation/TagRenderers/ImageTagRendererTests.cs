using NUnit.Framework;
using System.Linq;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;
using System.Collections.Generic;
using APSIM.Interop.Documentation.Renderers;
using Document = MigraDocCore.DocumentObjectModel.Document;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using Image = System.Drawing.Image;
using ImageTag = APSIM.Shared.Documentation.Image;
using MigraDocImage = MigraDocCore.DocumentObjectModel.Shapes.Image;
using System.Drawing;

namespace UnitTests.Interop.Documentation.TagRenderers
{
    /// <summary>
    /// Tests for <see cref="ImageTagRenderer"/>.
    /// </summary>
    /// <remarks>
    /// todo: mock out PdfBuilder API.
    /// </remarks>
    [TestFixture]
    public class ImageTagRendererTests
    {
        /// <summary>
        /// A pdf builder instance.
        /// </summary>
        /// <remarks>
        /// As with all of these tag renderer test classes, this API really
        /// ought to be mocked out.
        /// </remarks>
        private PdfBuilder pdfBuilder;

        /// <summary>
        /// Pdf document which will be modified by the pdfBuilder.
        /// </summary>
        private Document document;

        /// <summary>
        /// The image tag renderer being tested.
        /// </summary>
        private ImageTagRenderer renderer;

        /// <summary>
        /// An image tag which may be used by the renderer.
        /// </summary>
        private ImageTag imageTag;

        /// <summary>
        /// Our mock image tag will return this image.
        /// </summary>
        private static SkiaSharp.SKImage image;

        [SetUp]
        public void SetUp()
        {
            document = new MigraDocCore.DocumentObjectModel.Document();
            // Workaround for a quirk in the migradoc API.
            _ = document.AddSection().Elements;
            pdfBuilder = new PdfBuilder(document, PdfOptions.Default);
            pdfBuilder.UseTagRenderer(new MockTagRenderer());
            image = SkiaSharp.SKImage.Create(new SkiaSharp.SKImageInfo(4, 4));

            imageTag = new ImageTag(image);
            renderer = new ImageTagRenderer();
        }

        /// <summary>
        /// Dispose of the created image.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            image.Dispose();
        }

        /// <summary>
        /// Ensure that the renderer writes the image to the PDF document.
        /// </summary>
        [Test]
        public void EnsureImageIsAdded()
        {
            renderer.Render(imageTag, pdfBuilder);

            List<Paragraph> paragraphs = document.LastSection.Elements.OfType<Paragraph>().ToList();
            Assert.That(paragraphs.Count, Is.EqualTo(1));
            MigraDocImage actual = paragraphs[0].Elements.OfType<MigraDocImage>().First();
            AssertEqual(image, actual);
        }

        /// <summary>
        /// Ensure that the image is not written to a previous paragraph
        /// (if one exists).
        /// </summary>
        [Test]
        public void EnsureImageIsAddedToNewParagraph()
        {
            // Write a non-empty paragraph to the document.
            document.LastSection.AddParagraph("paragraph text");

            // Write the image.
            renderer.Render(imageTag, pdfBuilder);

            // Ensure that the image was not written to the paragraph.
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[1];
            MigraDocImage actual = paragraph.Elements[0] as MigraDocImage;
            AssertEqual(image, actual);
        }

        /// <summary>
        /// Ensure that content written after the image is not added to the
        /// same paragraph as the image.
        /// </summary>
        [Test]
        public void EnsureLaterContentIsInNewParagraph()
        {
            // Write the image.
            renderer.Render(imageTag, pdfBuilder);

            // Write a non-empty paragraph to the document.
            document.LastSection.AddParagraph("paragraph text");

            // Ensure that the image was not written to the paragraph.
            Assert.That(document.LastSection.Elements.Count, Is.EqualTo(2));    
            Paragraph paragraph = (Paragraph)document.LastSection.Elements[0];
            MigraDocImage actual = paragraph.Elements[0] as MigraDocImage;
            AssertEqual(image, actual);
        }

        /// <summary>
        /// Ensure that a System.Drawing.Image and a MigraDoc image are equivalent.
        /// </summary>
        /// <param name="expected">Expected image.</param>
        /// <param name="actual">Actual image.</param>
        private void AssertEqual(SkiaSharp.SKImage expected, MigraDocImage actual)
        {
            if (expected == null)
                Assert.That(actual, Is.Null);
            else
                Assert.That(actual, Is.Not.Null);

            // Note: actual.Width is not the actual width (that would be too easy);
            // instead, it represents a custom user-settable width. We're more
            // interested in the width of the underlying image.
            Assert.That(actual.Source.Width, Is.EqualTo(expected.Width));
            Assert.That(actual.Source.Height, Is.EqualTo(expected.Height));
        }
    }
}
