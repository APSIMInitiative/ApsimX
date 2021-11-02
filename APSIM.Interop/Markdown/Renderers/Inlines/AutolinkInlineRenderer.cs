using System;
using Markdig.Syntax.Inlines;

namespace APSIM.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// This class renders an <see cref="AutolinkInline" /> object to a PDF document.
    /// </summary>
    public class AutolinkInlineRenderer : PdfObjectRenderer<AutolinkInline>
    {
        /// <summary>
        /// Render the given autolink inline object to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="obj">The autolink inline object to be renderered.</param>
        protected override void Write(PdfBuilder renderer, AutolinkInline obj)
        {
            string prefix = obj.IsEmail ? "mailto:" : "";
            string uri = $"{prefix}{obj.Url}";
            renderer.SetLinkState(uri);
            renderer.AppendText(uri, TextStyle.Normal);
            renderer.ClearLinkState();
        }
    }
}