using System;
using Markdig.Syntax.Inlines;

namespace APSIM.Interop.Markdown.Inlines
{
    /// <summary>
    /// A reference to a publication in the bib file, embedded in markdown.
    /// </summary>
    public class ReferenceInline : LeafInline
    {
        /// <summary>
        /// Short name of the referenced article (e.g. holzworth2018apsim).
        /// </summary>
        public string ReferenceName { get; private set; }

        /// <summary>
        /// Initialise a new <see cref="ReferenceInline"/> instance.
        /// </summary>
        /// <param name="reference">The reference name.</param>
        public ReferenceInline(string reference)
        {
            ReferenceName = reference;
        }
    }
}
