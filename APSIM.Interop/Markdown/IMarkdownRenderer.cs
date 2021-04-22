using System.Collections.Generic;
using System.Drawing;
using APSIM.Interop.Markdown.Tags;

namespace APSIM.Interop.Markdown
{
    public interface IMarkdownRenderer
    {
        void AddImage(Image image);
        void AddHeading(IEnumerable<TextTag> contents, int headingLevel);
        void AddParagraph(IEnumerable<TextTag> contents);
        void AddTable(IEnumerable<TableRow> rows);
        void AddLink(IEnumerable<TextTag> contents, string uri);
    }
}
