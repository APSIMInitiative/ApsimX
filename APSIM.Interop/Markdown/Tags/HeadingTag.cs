using System.Collections.Generic;

namespace APSIM.Interop.Markdown.Tags
{
    /// <summary>
    /// This class encapsulates a piece of text in a markdown document.
    /// It's not a tag in and of itself, but it's a building block used
    /// by other tags (such as paragraphs, headings, etc).
    /// </summary>
    public class HeadingTag : ParagraphTag
    {
        public int HeadingLevel { get; private set; }
        public HeadingTag(IEnumerable<TextTag> contents, int headingLevel) : base(contents)
        {
            HeadingLevel = headingLevel;
        }

        public override void Render(IMarkdownRenderer renderer)
        {
            renderer.AddHeading(Contents, HeadingLevel);
        }
    }
}
