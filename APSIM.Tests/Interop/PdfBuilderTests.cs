using NUnit.Framework;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Interop.Markdown.Renderers.Blocks;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Markdown;
using MigraDocCore.DocumentObjectModel;
using APSIM.Interop.Markdown.Renderers.Inlines;
using Moq;
using Markdig.Parsers.Inlines;
using System;
using System.Drawing;
using System.IO;
using APSIM.Services.Documentation;

namespace APSIM.Tests.Interop
{
    /// <summary>
    /// Unit tests for <see cref="PdfBuilder"/>.
    /// </summary>
    /// <remarks>
    /// I've added skeleton tests for most of the public API.
    /// Now need to test some of the implied features. E.g.
    /// - GetLastParagraph()
    ///   - Section with no paragraphs
    ///   - No sections
    ///   - Multiple sections
    ///   - Existing paragraph with/without content
    /// - GetLastSection()
    /// - Heading font size
    /// - Link style
    /// - Style naming
    /// - HR style (this may be done already in the skeleton tests)
    /// - ReadResizeImage doesn't belong in this class
    /// - GetPageSize edge cases
    ///   - no sections
    ///   - section but no content
    /// - Markdown object rendering
    ///   - this should ultimately be extracted to another class
    ///   - Known tag with default renderer
    ///   - Unknown tag - exception
    ///   - Custom tag with custom renderer added
    /// </remarks>
    [TestFixture]
    public class PdfBuilderTests
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
        /// Ensure that PdfBuilder can render all known tag types.
        /// </summary>
        [Test]
        public void EnsureCanRenderKnownTagTypes()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that PdfBuilder throws an exception when trying
        /// to render an unknown tag type (ie a tag which is missing
        /// a corresponding renderer type).
        /// </summary>
        [Test]
        public void EnsureUnknownTagTypesCauseError()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that calling <see cref="PdfBuilder.SetLinkState(string)"/> will cause
        /// subsequent textual additions to be inserted as a link.
        /// </summary>
        [Test]
        public void TestSetLinkState()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that serial calls to <see cref="PdfBuilder.SetLinkState(string)"/>
        /// trigger an exception. (Nested links are not implemented.)
        /// </summary>
        [Test]
        public void EnsureSetLinkStateThrows()
        {
            builder.SetLinkState("");
            Assert.Throws<NotImplementedException>(() => builder.SetLinkState(""));
        }

        /// <summary>
        /// Ensure that after calling <see cref="PdfBuilder.ClearLinkState()"/>,
        /// newly-inserted text is no longer inserted a hyperlink.
        /// </summary>
        [Test]
        public void TestClearLinkState()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that calling <see cref="PdfBuilder.ClearLinkState()"/> without
        /// first calling <see cref="PdfBuilder.SetLinkState(string)"/> will trigger
        /// an exception.
        /// </summary>
        [Test]
        public void EnsureClearLinkStateThrows()
        {
            Assert.Throws<InvalidOperationException>(() => builder.ClearLinkState());
        }

        /// <summary>
        /// Ensure that calls to <see cref="PdfBuilder.StartListItem(string)"/> cause
        /// a bullet point to be inserted.
        /// </summary>
        [Test]
        public void TestStartListItem()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.StartListItem(string)"/> will trigger
        /// an exception if called serially without calling
        /// <see cref="PdfBuilder.FinishListItem()"/> in between.
        /// </summary>
        [Test]
        public void EnsureStartListItemCanThrow()
        {
            builder.StartListItem("");
            Assert.Throws<NotImplementedException>(() => builder.StartListItem(""));
        }

        /// <summary>
        /// Ensure that after finishing a list item (via <see cref="PdfBuilder.FinishListItem()"/>),
        /// any newly-inserted text is not added to that list item.
        /// </summary>
        [Test]
        public void TestFinishListItem()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that calls to <see cref="PdfBuilder.FinishListItem()"/> trigger an
        /// exception if <see cref="PdfBuilder.StartListItem(string)"/> has not been
        /// previously called.
        /// </summary>
        [Test]
        public void EnsureFinishListItemCanThrow()
        {
            Assert.Throws<NotImplementedException>(() => builder.FinishListItem());
        }

        /// <summary>
        /// Ensure that multiple list items can be added to the document.
        /// </summary>
        [Test]
        public void TestMultipleListItems()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// tbi
        /// </summary>
        [Test]
        public void TestAppendBookmarkedText()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendText(string, TextStyle)"/>
        /// will insert the text into the document.
        /// </summary>
        [Test]
        public void TestAppendText()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendText(string, TextStyle)"/>
        /// will apply the specified style to the inserted text.
        /// </summary>
        [Test]
        public void TestAppendTextStyle()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendText(string, TextStyle)"/>
        /// will insert the text at the end of the document, in the same paragraph
        /// as existing content.
        /// </summary>
        [Test]
        public void TestAppendTextLocation()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendText(string, TextStyle)"/> will
        /// insert a new paragraph if none exists.
        /// </summary>
        [Test]
        public void TestAppendTextNewParagraph()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendImage(Image)"/> adds the
        /// image into the document.
        /// </summary>
        [Test]
        public void TestAppendImage()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendImage(System.Drawing.Image)"/>
        /// adds the image at the end of the document, in an existing paragraph
        /// (if one exists).
        /// </summary>
        [Test]
        public void TestAppendImageLocation()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendHorizontalRule()"/> adds
        /// a horizontal rule at the current end of document.
        /// </summary>
        [Test]
        public void TestAppendHR()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that any content added after a HR is indeed added
        /// below the HR.
        /// </summary>
        [Test]
        public void TestAppendAfterHR()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that adding text after calling
        /// <see cref="PdfBuilder.StartNewParagraph()"/> adds the text into
        /// a new paragraph.
        /// </summary>
        [Test]
        public void TestStartNewParagraph()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that calling <see cref="PdfBuilder.StartNewParagraph()"/> while inserting
        /// text into an inline hyperlink will trigger an exception.
        /// </summary>
        [Test]
        public void EnsureStartParagraphCanThrow()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.GetPageSize(out double, out double)"/>
        /// returns the correct width and height in pixels.
        /// </summary>
        [Test]
        public void TestGetPageSize()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that when the document contains multiple sections of different
        /// sizes, <see cref="PdfBuilder.GetPageSize(out double, out double)"/>
        /// returns the correct width and height in pixels of the last section.
        /// </summary>
        [Test]
        public void TestGetPageSizeMultipleSections()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.Write(ITag)"/> causes the tag to be
        /// written to the document via an appropriate tag renderer.
        /// </summary>
        [Test]
        public void TestWriteTag()
        {
            throw new NotImplementedException();
        }
    }
}
