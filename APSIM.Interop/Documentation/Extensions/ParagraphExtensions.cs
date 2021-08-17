using System.Collections.Generic;
using System.Linq;
using System;
#if NETCOREAPP
using MigraDocCore.DocumentObjectModel;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using Image = MigraDocCore.DocumentObjectModel.Shapes.Image;
#else
using MigraDoc.DocumentObjectModel;
using Paragraph = MigraDoc.DocumentObjectModel.Paragraph;
using Image = MigraDoc.DocumentObjectModel.Shapes.Image;
#endif

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
            IEnumerable<Text> textElements = paragraph.GetTextElements();
            return textElements.All(t => string.IsNullOrEmpty(t.Content));
        }

        /// <summary>
        /// Return all text elements in the paragraph - including the text elements
        /// inside the FormattedText elements.
        /// </summary>
        /// <param name="paragraph">The paragraph.</param>
        internal static IEnumerable<Text> GetTextElements(this Paragraph paragraph)
        {
            if (paragraph == null)
                throw new ArgumentNullException(nameof(paragraph));

            IEnumerable<Text> raw = paragraph.Elements.OfType<Text>();
            IEnumerable<Text> formatted = paragraph.Elements.OfType<FormattedText>().SelectMany(f => f.Elements.OfType<Text>());
            return formatted.Union(raw);
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

            return string.Join("", paragraph.GetTextElements().Select(t => t.Content));
        }
    }
}
