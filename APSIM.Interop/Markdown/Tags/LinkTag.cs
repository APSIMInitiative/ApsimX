using System;
using System.Collections.Generic;
using System.Linq;

namespace APSIM.Interop.Markdown.Tags
{
    /// <summary>
    /// This class encapsulates a hyperlink in a markdown document.
    /// </summary>
    public class LinkTag : TextTag
    {
        public IEnumerable<TextTag> Contents { get; private set; }
        public string Uri { get; private set; }
        public LinkTag(IEnumerable<TextTag> contents, string uri) : base(string.Join("", contents.SelectMany(c => c.Content)), new TextStyle())
        {
            Contents =  contents;
            Uri = uri;
        }

        public override void Render(IMarkdownRenderer renderer)
        {
            renderer.AddLink(Contents, Uri);
        }
    }
}
