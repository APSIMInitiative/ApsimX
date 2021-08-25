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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test italic style.
        /// </summary>
        [Test]
        public void TestItalic()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test strong (ie bold) style.
        /// </summary>
        [Test]
        public void TestStrong()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test underline style.
        /// </summary>
        [Test]
        public void TestUnderline()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test strikethrough style.
        /// </summary>
        [Test]
        public void TestStrikethrough()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test superscript style.
        /// </summary>
        [Test]
        public void TestSuperscript()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test subscript style.
        /// </summary>
        [Test]
        public void TestSubscript()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test italic style.
        /// </summary>
        [Test]
        public void TestQuote()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Test code style.
        /// </summary>
        [Test]
        public void TestCode()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that style applied via <see cref="PdfBuilder.PushStyle(TextStyle)"/>
        /// is used when inserting text.
        /// </summary>
        [Test]
        public void TestPushStyle()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that multiple calls to <see cref="PdfBuilder.PushStyle(TextStyle)"/>
        /// combine, rather than overwrite, their style effects.
        /// </summary>
        [Test]
        public void TestNestedStyle()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that style applied via calls to
        /// <see cref="PdfBuilder.AppendText(string, TextStyle)"/> is combined with
        /// style applied via calls to <see cref="PdfBuilder.PushStyle(TextStyle)"/>.
        /// </summary>
        [Test]
        public void TestCombinedStyle()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that after calling <see cref="PdfBuilder.PopStyle()"/>,
        /// style is no longer applied when inserting new text.
        /// </summary>
        [Test]
        public void TestPopStyle()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that calling <see cref="PdfBuilder.PopStyle()"/> without
        /// a matching call to <see cref="PdfBuilder.PushStyle(TextStyle)"/>
        /// will trigger an exception.
        /// </summary>
        [Test]
        public void EnsurePopStyleCantThrow()
        {
            Assert.Throws<InvalidOperationException>(() => builder.PopStyle());
        }
    }
}
