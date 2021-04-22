using System;

namespace APSIM.Interop.Markdown.Tags
{
    /// <summary>
    /// This class encapsulates a piece of text in a markdown document.
    /// It's not a tag in and of itself, but it's a building block used
    /// by other tags (such as paragraphs, headings, etc).
    /// </summary>
    public class TextTag : IMarkdownTag
    {
        public string Content { get; private set; }

        public ITextStyle Style { get; private set; }

        public TextTag(string content, ITextStyle style)
        {
            Content = content;
            Style = style;
        }

        public virtual void Render(IMarkdownRenderer renderer)
        {
            throw new NotImplementedException();
        }
    }
}
