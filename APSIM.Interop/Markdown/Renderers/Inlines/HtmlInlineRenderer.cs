using System;
using Markdig.Syntax.Inlines;

namespace APSIM.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// This class renders a <see cref="HtmlInline" /> object to a PDF document.
    /// </summary>
    public class HtmlInlineRenderer : PdfObjectRenderer<HtmlInline>
    {
        /// <summary>
        /// Render the given HtmlInline object to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="obj">The HtmlInline object to be renderered.</param>
        protected override void Write(PdfBuilder renderer, HtmlInline obj)
        {
            // todo - this should probably render the HTML tags to the PDF somehow.
            renderer.AppendText(obj.Tag, TextStyle.Normal);
        }
    }
}
