using NUnit.Framework;
using System.Linq;
using System.Collections.Generic;
using Document = MigraDocCore.DocumentObjectModel.Document;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using APSIM.Interop.Documentation.Extensions;
using MigraDocCore.DocumentObjectModel;
using System;

namespace UnitTests.Interop.PdfRendering
{
    /// <summary>
    /// Tests for <see cref="ParagraphExtensions" /> class.
    /// </summary>
    [TestFixture]
    public class ParagraphExtensionsTests
    {
        /// <summary>
        /// The document used for running tests. Each test can assume
        /// that the document is initialised with one empty section.
        /// </summary>
        private Document document;

        [SetUp]
        public void SetUp()
        {
            document = new Document();
            document.AddSection();
        }

        /// <summary>
        /// Ensure that calls to GetTextElements() fail with an appropriate
        /// exception if we pass in a null paragraph.
        /// </summary>
        [Test]
        public void TestGetTextElementsNullParagraph()
        {
            Assert.Throws<ArgumentNullException>(() => ParagraphExtensions.GetTextElements(null).ToArray());
        }

        /// <summary>
        /// Ensure that calls to IsEmpty() fail with an appropriate
        /// exception if we pass in a null section.
        /// </summary>
        [Test]
        public void TestIsEmptyNullParagraph()
        {
            Assert.Throws<ArgumentNullException>(() => ParagraphExtensions.IsEmpty(null));
        }

        /// <summary>
        /// Ensure that calls to GetRawText() fail with an appropriate
        /// exception if we pass in a null section.
        /// </summary>
        [Test]
        public void TestGetRawTextNullParagraph()
        {
            Assert.Throws<ArgumentNullException>(() => ParagraphExtensions.GetRawText(null));
        }

        /// <summary>
        /// Test the GetRawText() method for all different TextFormats.
        /// </summary>
        /// <param name="text">Text to add to the document.</param>
        /// <param name="format"></param>
        [TestCase("A short message 0.", TextFormat.Bold)]
        [TestCase("A short message 1.", TextFormat.Italic)]
        [TestCase("A short message 2.", TextFormat.NotBold)]
        [TestCase("A short message 3.", TextFormat.NotItalic)]
        [TestCase("A short message 4.", TextFormat.NoUnderline)]
        [TestCase("A short message 5.", TextFormat.Underline)]
        public void GetRawTextFormats(string text, TextFormat format)
        {
            document.LastSection.AddParagraph().AddFormattedText(text, format);
            Assert.That(document.LastSection.LastParagraph.GetRawText(), Is.EqualTo(text));
        }

        /// <summary>
        /// Test GetRawText() on a paragraph with linebreaks.
        /// </summary>
        [Test]
        public void TestParagraphWithLinebreaks()
        {
            Paragraph paragraph = document.LastSection.AddParagraph();
            string text0 = "before line break";
            string text1 = "\n";
            string text2 = "afterlinebreak";
            paragraph.AddText(text0);
            paragraph.AddText(text1);
            paragraph.AddText(text2);

            string expected = $"{text0}{text1}{text2}";
            Assert.That(paragraph.GetRawText(), Is.EqualTo(expected));
        }

        /// <summary>
        /// Ensure that the GetRawTextElements() function will return
        /// the text elements in the correct order.
        /// </summary>
        [Test]
        public void TestGetTextElementsOrder()
        {
            Paragraph paragraph = document.LastSection.AddParagraph();
            string text0 = "formatted text 0";
            string text1 = "plaintext 1";
            string text2 = "formatted text 2";
            paragraph.AddFormattedText(text0);
            paragraph.AddText(text1);
            paragraph.AddFormattedText(text2);

            string[] elements = paragraph.Elements.GetTextElements().ToArray();
            Assert.That(elements[0], Is.EqualTo(text0));
            Assert.That(elements[1], Is.EqualTo(text1));
            Assert.That(elements[2], Is.EqualTo(text2));
        }

        /// <summary>
        /// Ensure that GetRawText() returns the correct value for paragraphs which
        /// contain a mixture of formatted and raw text.
        /// </summary>
        /// <param name="formattedText"></param>
        /// <param name="plainText"></param>
        [TestCase("", "plaintext")]
        [TestCase("formattedtext", "")]
        [TestCase("formatted text", "plain text")]
        public void TestMixedPlainAndFormattedText(string formattedText, string plainText)
        {
            Paragraph paragraph = document.LastSection.AddParagraph();
            paragraph.AddFormattedText(formattedText);
            paragraph.AddText(plainText);
            Assert.That(paragraph.GetRawText(), Is.EqualTo($"{formattedText}{plainText}"));
        }

        /// <summary>
        /// Test the IsEmpty() method.
        /// </summary>
        /// <param name="empty">Expected return value of IsEmpty().</param>
        /// <param name="formattedText">Should the text be inserted as formatted text (T) or plain text (F)?</param>
        /// <param name="elements">Text elements to be inserted.</param>
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(true, true, "")]
        [TestCase(true, false, "")]
        [TestCase(true, true, "", "")]
        [TestCase(true, false, "", "")]
        [TestCase(false, true, "0")]
        [TestCase(false, false, "1")]
        [TestCase(false, true, "", "2")]
        [TestCase(false, false, "", "3")]
        public void TestIsEmpty(bool empty, bool formattedText, params string[] elements)
        {
            Paragraph paragraph = document.LastSection.AddParagraph();
            foreach (string text in elements)
            {
                if (formattedText)
                    paragraph.AddFormattedText(text);
                else
                    paragraph.AddText(text);
            }
            Assert.That(paragraph.IsEmpty(), Is.EqualTo(empty));
        }

        /// <summary>
        /// Test the GetLastTable() function.
        /// </summary>
        /// <param name="formatted">Should the text be inserted as formatted text (T) or plain text (F)?</param>
        /// <param name="elements">Text elements to be inserted.</param>
        [TestCase(true)]
        [TestCase(false, "plain text")]
        [TestCase(true, "some formatted text")]
        [TestCase(true, "formatted ", "text", " with ", "multiple", " elements.")]
        [TestCase(false, "multiple", " ", "plain", " text elements")]
        public void TestGetTextElements(bool formatted, params string[] elements)
        {
            Paragraph paragraph = document.LastSection.AddParagraph();
            foreach (string text in elements)
            {
                if (formatted)
                    paragraph.AddFormattedText(text);
                else
                    paragraph.AddText(text);
            }
            List<string> textElements = paragraph.Elements.GetTextElements().ToList();
            Assert.That(textElements.Count, Is.EqualTo(elements.Length));
            for (int i = 0; i < elements.Length; i++)
                Assert.That(textElements[i], Is.EqualTo(elements[i]));
        }

        /// <summary>
        /// Ensure that <see cref="ParagraphExtensions.GetTextElements(ParagraphElements)"/>
        /// returns text elements inside hyperlinks.
        /// </summary>
        [Test]
        public void TestGetHyperlinkTextElements()
        {
            string linkName = "hyperlinkName";
            string linkText = "link text";
            string linkFormatted = "link formatted text";
            Paragraph paragraph = document.LastSection.AddParagraph();
            Hyperlink link = paragraph.AddHyperlink(linkName);
            link.AddText(linkText);
            link.AddFormattedText(linkFormatted);
            List<string> textElements = paragraph.Elements.GetTextElements().ToList();
            Assert.That(textElements.Count, Is.EqualTo(2));
            Assert.That(textElements[0], Is.EqualTo(linkText));
            Assert.That(textElements[1], Is.EqualTo(linkFormatted));
        }
    }
}
