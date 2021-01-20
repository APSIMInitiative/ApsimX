using System;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax.Inlines;

namespace UserInterface.Classes
{
    /// <summary>
    /// Parser for subscript/superscript HTML tags.
    /// </summary>
    public class SubSuperScriptParser : InlineParser
    {
        /// <summary>
        /// Try to find a matching sub or superscript element in the specified slice.
        /// </summary>
        /// <param name="processor">The markdown processor.</param>
        /// <param name="slice">The slice of text in which to search.</param>
        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var startPosition = processor.GetSourcePosition(slice.Start, out int line, out int column);

            // Slightly faster to perform our own search for opening characters
            var nextStart = processor.Parsers.IndexOfOpeningCharacter(slice.Text, slice.Start + 1, slice.End);
            //var nextStart = str.IndexOfAny(processor.SpecialCharacters, slice.Start + 1, slice.Length - 1);
            string text = slice.Text.Substring(slice.Start, slice.End - slice.Start + 1);

            if (slice.PeekCharExtra(-1) != '>')
                return false;

            string openSuperscript = "<sup>";
            string closeSuperscript = "</sup>";
            string openSubscript = "<sub>";
            string closeSubscript = "</sub>";
            
            if (TryMatch(slice, openSuperscript))
            {
                int indexCloseSuper = text.IndexOf(closeSuperscript);
                if (indexCloseSuper > 0)
                {
                    slice = new StringSlice(text.Substring(0, indexCloseSuper));
                    processor.Inline = new EmphasisInline()
                    {
                        DelimiterChar = '^',
                        DelimiterCount = 1,
                    };
                    return true;
                }
            }
            else if (TryMatch(slice, openSubscript))
            {
                int indexCloseSub = text.IndexOf(closeSubscript);
                if (indexCloseSub > 0)
                {
                    slice = new StringSlice(text.Substring(0, indexCloseSub));
                    processor.Inline = new EmphasisInline()
                    {
                        DelimiterChar = '~',
                        DelimiterCount = 1,
                    };
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if the given slice of text contains a specific string.
        /// </summary>
        /// <param name="slice">Slice of text in which to search.</param>
        /// <param name="text">Text to search for.</param>
        private bool TryMatch(StringSlice slice, string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            for (int i = 0; i < text.Length; i++)
                if (slice.PeekCharExtra(-1 * text.Length + i) != text[i])
                    return false;
            return true;
        }
    }
}
