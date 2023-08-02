using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;

namespace APSIM.Interop.Documentation
{
    /// <summary>
    /// This interface describes a class which can use a <see cref="PdfBuilder" />
    /// to render an <see cref="ITag" /> to a PDF document.
    /// </summary>
    internal abstract class TagRendererBase<T> : ITagRenderer where T : ITag
    {
        /// <summary>
        /// Can this renderer render the given tag?
        /// </summary>
        /// <param name="tag">The tag to be rendered.</param>
        public bool CanRender(ITag tag)
        {
            return tag is T;
        }

        /// <summary>
        /// Render the tag to the PDF document.
        /// </summary>
        /// <param name="tag">Tag to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        public void Render(ITag tag, PdfBuilder renderer)
        {
            Render((T)tag, renderer);
        }

        /// <summary>
        /// Render the tag to the PDF document.
        /// </summary>
        /// <param name="tag">Tag to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        protected abstract void Render(T tag, PdfBuilder renderer);
    }
}
