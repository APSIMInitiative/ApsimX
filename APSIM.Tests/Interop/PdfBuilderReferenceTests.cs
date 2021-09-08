using APSIM.Interop.Documentation;
using APSIM.Interop.Documentation.Helpers;
using APSIM.Interop.Markdown;
using APSIM.Interop.Markdown.Renderers;
using MigraDocCore.DocumentObjectModel;
using Moq;
using NUnit.Framework;
using System;

namespace APSIM.Tests.Interop.Documentation
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
        public void EnsureNullCitationHelperNotAllowed()
        {
            Assert.Throws<ArgumentNullException>(() => new PdfBuilder(doc, new PdfOptions(null, null)));
        }

        /// <summary>
        /// Ensure that the citation resolver passed in via the
        /// <see cref="PdfOptions"/> instance is used to resolve references.
        /// </summary>
        [Test]
        public void TestUseCustomBibFile()
        {
            string reference = "custom_reference";
            Mock<ICitation> citation = new Mock<ICitation>();
            citationResolver.Setup(r => r.Lookup(reference)).Returns(citation.Object);
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure taht references to citations with a URL are linked to the
        /// citation's URL.
        /// </summary>
        [Test]
        public void TestAppendReferenceWithUrl()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendReference(string, TextStyle)"/>
        /// will insert invalid references as plaintext between square brackets
        /// if no matching reference is found in the citation helper.
        /// </summary>
        [Test]
        public void TestAppendInvalidReference()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that we use the citation's in-text property as the text
        /// for the created hyperlink.
        /// </summary>
        [Test]
        public void TestAppendReferenceText()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendReference(string, TextStyle)"/>
        /// respects the given style for valid references.
        /// </summary>
        [Test]
        public void TestAppendValidReferenceStyle()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that <see cref="PdfBuilder.AppendReference(string, TextStyle)"/>
        /// respects the given style for invalid references.
        /// </summary>
        [Test]
        public void TestAppendInalidReferenceStyle()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure taht <see cref="PdfBuilder.AppendReference(string, TextStyle)"/>
        /// will insert the text in the final paragraph of the document.
        /// </summary>
        [Test]
        public void TestAppendReferenceCorrectParagraph()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that we can have multiple references to the same paper
        /// in a document.
        /// </summary>
        [Test]
        public void TestMultipleReferencesToSamePaper()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that we can have multiple references to different papers
        /// in a document.
        /// </summary>
        [Test]
        public void TestMultipleReferencesToDifferentPapers()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ensure that any content added after a reference goes in the same
        /// paragraph as the reference, but is not part of the reference.
        /// </summary>
        [Test]
        public void TestContentAfterReference()
        {
            throw new NotImplementedException();
        }
    }
}
