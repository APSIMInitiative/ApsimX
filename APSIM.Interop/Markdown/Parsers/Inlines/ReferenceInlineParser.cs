using System;
using Markdig.Helpers;
using Markdig.Parsers;
using APSIM.Interop.Markdown.Inlines;

namespace APSIM.Interop.Markdown.Parsers.Inlines
{
    /// <summary>
    /// A parser for a <see cref="ReferenceInline"/>.
    /// </summary>
    public class ReferenceInlineParser : InlineParser
    {
        private const char open = '[';
        private const char close = ']';
        private const uint maxLength = 100;

        public ReferenceInlineParser()
        {
            // OpeningCharacters = new[] { open };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            int start = slice.Start;
            char current = slice.CurrentChar;
            if (slice.PeekCharExtra(-1) != open)
            {
                if (current == open)
                    current = slice.NextChar();
                else
                {
                    while (current.IsWhiteSpaceOrZero())
                    {
                        current = slice.NextChar();
                        if (slice.Start > slice.End)
                            return false;
                    }
                    if (current != open)
                        return false;
                }
            }

            int startName = slice.Start;
            int endName = slice.Start;

            // Read the reference name.
            while (current != close)
            {
                endName = slice.Start;
                if ( (endName - startName) > maxLength )
                    return false;
                current = slice.NextChar();
                if (slice.Start > slice.End)
                    // Abort if we've reached the end of the slice.
                    return false;
            }

            current = slice.NextChar(); // skip the closing ]

            // If the next char is a '(', then this is an actual link.
            if (current == '(')
                return false;

            string reference = new StringSlice(slice.Text, startName, endName).ToString();
            var inline = new ReferenceInline(reference)
            {
                Span =
                {
                    Start = processor.GetSourcePosition(slice.Start, out int line, out int column)
                },
                Line = line,
                Column = column
            };
            inline.Span.End = inline.Span.Start + (endName - startName);

            processor.Inline = inline;

            return true;
        }
    }
}
