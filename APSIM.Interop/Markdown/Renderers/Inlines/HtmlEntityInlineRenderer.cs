using System;
using Markdig.Syntax.Inlines;

namespace APSIM.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// This class renders a <see cref="HtmlEntityInline" /> object to a PDF document.
    /// </summary>
    public class HtmlEntityInlineRenderer : PdfObjectRenderer<HtmlEntityInline>
    {
        /// <summary>
        /// Render the given HtmlEntityInline object to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="obj">The HtmlEntityInline object to be renderered.</param>
        protected override void Write(PdfBuilder renderer, HtmlEntityInline obj)
        {
            renderer.AppendText(obj.Transcoded.Text, TextStyle.Normal);
        }
    }
}
