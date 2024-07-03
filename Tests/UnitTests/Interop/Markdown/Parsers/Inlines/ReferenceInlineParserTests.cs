using NUnit.Framework;
using System;
using Markdig.Parsers;
using APSIM.Interop.Markdown.Parsers.Inlines;
using Moq;
using Markdig.Helpers;
using Markdig.Syntax.Inlines;
using APSIM.Interop.Markdown.Inlines;
using Markdig.Syntax;
using Markdig;

namespace UnitTests.Interop.Markdown.Parsers.Inlines
{
    [TestFixture]
    public class ReferenceInlineParserTests
    {
        private ReferenceInlineParser parser;
        private InlineProcessor processor;

        /// <summary>
        /// Initialise the testing environment before each test is run.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            parser = new ReferenceInlineParser();
            Mock<InlineProcessor> mockProcessor = new Mock<InlineProcessor>(new MarkdownDocument(), new InlineParserList(new InlineParser[0]), false, new MarkdownParserContext(), false);
            processor = mockProcessor.Object;
        }

        /// <summary>
        /// Ensure that the given text can be parsed as a <see cref="ReferenceInline"/>.
        /// </summary>
        /// <param name="reference">Reference name (without the enclosing square brackets).</param>
        [TestCase("simpleref")]
        [TestCase("simple_ref")]
        [TestCase("simple ref")]
        [TestCase(" simple _ref_ * ")]
        public void EnsureSucessfulParse(string reference)
        {
            StringSlice slice = new StringSlice($"[{reference}]");
            bool result = parser.Match(processor, ref slice);
            Assert.That(result, Is.True);
            Assert.That(processor.Inline, Is.Not.Null);
            Assert.That(processor.Inline.GetType(), Is.EqualTo(typeof(ReferenceInline)));
            ReferenceInline inline = (ReferenceInline)processor.Inline;
            Assert.That(inline.ReferenceName, Is.EqualTo(reference));
        }

        /// <summary>
        /// Ensure that the given contents don't parse.
        /// </summary>
        /// <param name="contents">Text which shouldn't be parsed as a <see cref="ReferenceInline"/>.</param>
        [TestCase("[link text](link dest)")]
        [TestCase("asdf")]
        [TestCase("asdf [valid link text]")]
        public void EnsureFailedParse(string contents)
        {
            StringSlice slice = new StringSlice(contents);
            bool result = parser.Match(processor, ref slice);
            Assert.That(result, Is.False);
            Assert.That(processor.Inline, Is.Null);
        }

        [Test]
        public void DontParseLongStrings()
        {
            EnsureFailedParse(new string('x', 150));
        }
    }
}
