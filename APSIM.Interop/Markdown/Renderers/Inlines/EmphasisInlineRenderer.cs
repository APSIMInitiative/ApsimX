using System;
using Markdig.Syntax.Inlines;

namespace APSIM.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// This class renders an <see cref="EmphasisInline" /> object to a PDF document.
    /// </summary>
    public class EmphasisInlineRenderer : PdfObjectRenderer<EmphasisInline>
    {
        /// <summary>
        /// Render the given emphasis inline object to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="obj">The emphasis inline object to be renderered.</param>
        protected override void Write(PdfBuilder renderer, EmphasisInline obj)
        {
            renderer.PushStyle(CreateStyle(obj.DelimiterChar, obj.DelimiterCount));
            renderer.WriteChildren(obj);
            renderer.PopStyle();
        }

        private static TextStyle CreateStyle(char delimiter, int count)
        {
            switch (delimiter)
            {
                case '^':
                    return TextStyle.Superscript;
                case '~':
                    return count == 1 ? TextStyle.Subscript : TextStyle.Strikethrough;
                default:
                    return count == 1 ? TextStyle.Italic : TextStyle.Strong;
            }
        }
    }
}
