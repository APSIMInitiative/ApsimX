using System.Collections.Generic;
using System.Linq;
using System;

using MigraDocCore.DocumentObjectModel;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using Image = MigraDocCore.DocumentObjectModel.Shapes.Image;


namespace APSIM.Interop.Documentation.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="Paragraph"/> class.
    /// </summary>
    internal static class ParagraphExtensions
    {
        /// <summary>
        /// Return true iff the paragraph is empty.
        /// </summary>
        /// <param name="paragraph">The paragraph.</param>
        internal static bool IsEmpty(this Paragraph paragraph)
        {
            if (paragraph == null)
                throw new ArgumentNullException(nameof(paragraph));

            if (paragraph.Elements.OfType<Image>().Any())
                return false;
            IEnumerable<string> textElements = paragraph.Elements.GetTextElements();
            return textElements.All(string.IsNullOrEmpty);
        }

        /// <summary>
        /// Return all text elements in the paragraph - including the text elements
        /// inside the FormattedText elements.
        /// </summary>
        /// <param name="paragraph">The paragraph.</param>
        internal static IEnumerable<string> GetTextElements(this ParagraphElements paragraph)
        {
            if (paragraph == null)
                throw new ArgumentNullException(nameof(paragraph));

            foreach (DocumentObject obj in paragraph)
            {
                if (obj is Text text)
                    yield return text.Content;
                else if (obj is Character character)
                {
                    if (character.SymbolName == SymbolName.LineBreak)
                        // avoids issue with Windows vs. Linux.
                        yield return "\n";
                    else
                        // This probably doesn't work.
                        yield return new string(character.Char, character.Count);
                }
                else if (obj is FormattedText formattedText)
                    foreach (string subtext in formattedText.Elements.GetTextElements())
                        yield return subtext;
                else if (obj is Hyperlink link)
                    foreach (string subtext in link.Elements.GetTextElements())
                        yield return subtext;
            }
        }

        /// <summary>
        /// Get the plain text contents of the paragraph.
        /// </summary>
        /// <remarks>
        /// This will unwrap the FormattedText elements as needed.
        /// </remarks>
        /// <param name="paragraph">The paragraph.</param>
        internal static string GetRawText(this Paragraph paragraph)
        {
            if (paragraph == null)
                throw new ArgumentNullException(nameof(paragraph));

            return string.Join("", paragraph.Elements.GetTextElements());
        }
    }
}
