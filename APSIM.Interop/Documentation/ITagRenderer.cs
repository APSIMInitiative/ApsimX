using APSIM.Shared.Documentation;

namespace APSIM.Interop.Documentation
{
    /// <summary>
    /// This interface describes a class which can use a <see cref="PdfBuilder" />
    /// to render an <see cref="ITag" /> to a PDF document.
    /// </summary>
    internal interface ITagRenderer
    {
        /// <summary>
        /// Can this renderer render the given tag?
        /// </summary>
        /// <param name="tag">The tag to be rendered.</param>
        bool CanRender(ITag tag);

        /// <summary>
        /// Render the tag to the PDF document.
        /// </summary>
        /// <param name="tag">Tag to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        void Render(ITag tag);
    }
}
