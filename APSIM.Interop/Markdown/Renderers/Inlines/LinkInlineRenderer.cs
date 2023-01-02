using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using Markdig.Syntax.Inlines;

namespace APSIM.Interop.Markdown.Renderers.Inlines
{
    /// <summary>
    /// This class renders a <see cref="HtmlInline" /> object to a PDF document.
    /// </summary>
    public class LinkInlineRenderer : PdfObjectRenderer<LinkInline>
    {
        /// <summary>
        /// Relative path to images.
        /// </summary>
        private string imageRelativePath;

        /// <summary>
        /// Construct a <see cref="LinkInlineRenderer"/> instance.
        /// </summary>
        /// <param name="imagePath">Relative path used to search for images. E.g. if an image URI is images/image.png, then the path will be assumed to be relative to this argument.</param>
        public LinkInlineRenderer(string imagePath)
        {
            imageRelativePath = imagePath;
        }

        /// <summary>
        /// Render the given LinkInline object to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="link">The link object to be renderered.</param>
        protected override void Write(PdfBuilder renderer, LinkInline link)
        {
            string uri = link.GetDynamicUrl != null ? link.GetDynamicUrl() ?? link.Url : link.Url;
            if (link.IsImage)
            {
                SkiaSharp.SKImage image = GetImage(uri);
                // Technically, the image should be written to the same paragraph as any existing content.
                // However, if the image is too large, I'm going to add it to its own paragraph.
                // I'm defining "too large" as "taller than page height * 0.9".
                renderer.GetPageSize(out _, out double height);
                if (image.Height > 0.9 * height)
                    renderer.StartNewParagraph();

                renderer.AppendImage(image);

                // The assumption here is that any children of the image are the image's caption.
                renderer.StartNewParagraph();

                // Increment heading count, iff the link has a caption.
                if (link.Any())
                {
                    renderer.IncrementFigureNumber();
                    renderer.AppendText($"Figure {renderer.FigureNumber}: ", TextStyle.Strong);
                    renderer.WriteChildren(link);
                    renderer.StartNewParagraph();
                }
            }
            else
            {
                // Clunky bookmark detection. This could be improved.
                if (uri.StartsWith("#"))
                    renderer.StartBookmark(uri);
                else
                    renderer.SetLinkState(uri);
                renderer.WriteChildren(link);
                renderer.ClearLinkState();
            }
        }

        /// <summary>
        /// Get the image specified by the given url.
        /// </summary>
        /// <param name="uri">Image URI.</param>
        public virtual SkiaSharp.SKImage GetImage(string uri)
        {
            return APSIM.Shared.Documentation.Image.LoadImage(uri, imageRelativePath);
        }
    }
}
