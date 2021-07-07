using APSIM.Interop.Markdown.Renderers;
using APSIM.Services.Documentation;

namespace APSIM.Interop.Documentation.Renderers
{
    /// <summary>
    /// A class which can use a <see cref="PdfRenderer" /> to render an
    /// <see cref="ITag" /> to a PDF document.
    /// </summary>
    /// <typeparam name="T">The type of tag which this class can render.</typeparam>
    internal class ImageTagRenderer : TagRendererBase<Image>
    {
        /// <summary>
        /// Render the given image tag to the PDF document.
        /// </summary>
        /// <param name="image">Image tag to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        protected override void Render(Image image, PdfRenderer renderer)
        {
            // Add the image to a new paragraph.
            renderer.StartNewParagraph();
            renderer.AppendImage(image.Raster);
        }
    }
}
