using Markdig.Renderers;
using Markdig.Syntax;

namespace APSIM.Interop.Markdown.Renderers
{
    /// <summary>
    /// Base class for renderers of markdown objects to a PDF document.
    /// </summary>
    /// <typeparam name="T">The type of markdown object to be rendererd.</typeparam>
    public abstract class PdfObjectRenderer<T> : MarkdownObjectRenderer<PdfBuilder, T> where T : MarkdownObject
    {
    }
}