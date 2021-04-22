using System.Collections.Generic;

namespace APSIM.Interop.Markdown.Tags
{
    /// <summary>
    /// This class encapsulates a piece of text in a markdown document.
    /// It's not a tag in and of itself, but it's a building block used
    /// by other tags (such as paragraphs, headings, etc).
    /// </summary>
    public class ParagraphTag : IMarkdownTag
    {
        public IEnumerable<TextTag> Contents { get; private set; }
        public ParagraphTag(IEnumerable<TextTag> contents) => Contents = contents;
        public ParagraphTag(TextTag text) => Contents = new[] { text };

        public virtual void Render(IMarkdownRenderer renderer)
        {
            renderer.AddParagraph(Contents);
        }
    }
}
