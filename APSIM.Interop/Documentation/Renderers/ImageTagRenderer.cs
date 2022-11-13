using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;

namespace APSIM.Interop.Documentation.Renderers
{
    /// <summary>
    /// A class which can use a <see cref="PdfBuilder" /> to render an
    /// <see cref="ITag" /> to a PDF document.
    /// </summary>
    /// <typeparam name="T">The type of tag which this class can render.</typeparam>
    internal class ImageTagRenderer : TagRendererBase<Image>
    {
        /// <summary>
        /// Path on which to search for images.
        /// </summary>
        private string searchPath = null;

        /// <summary>
        /// Create a new <see cref="ImageTagRenderer"/> instance for the given
        /// search path.
        /// </summary>
        /// <param name="searchPath">Image search path. If image names are provided as filename only, will search on this path.</param>
        public ImageTagRenderer(string searchPath)
        {
            this.searchPath = searchPath;
        }

        /// <summary>
        /// Create a new <see cref="ImageTagRenderer"/> instance which does not search for images on disk.
        /// </summary>
        public ImageTagRenderer()
        {
        }

        /// <summary>
        /// Render the given image tag to the PDF document.
        /// </summary>
        /// <param name="image">Image tag to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        protected override void Render(Image image, PdfBuilder renderer)
        {
            // Add the image to a new paragraph.
            renderer.StartNewParagraph();
            renderer.AppendImage(image.GetRaster(searchPath));
            renderer.StartNewParagraph();
        }
    }
}
