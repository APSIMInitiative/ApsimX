using System.Drawing;

namespace APSIM.Interop.Markdown.Tags
{
    /// <summary>
    /// This class encapsulates a piece of text in a markdown document.
    /// It's not a tag in and of itself, but it's a building block used
    /// by other tags (such as paragraphs, headings, etc).
    /// </summary>
    public class ImageTag : IMarkdownTag
    {
        private Image image;

        public ImageTag(Image image)
        {
            this.image = image;
        }

        public virtual void Render(IMarkdownRenderer renderer)
        {
            renderer.AddImage(image);
        }
    }
}
