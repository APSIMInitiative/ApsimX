using System;

namespace APSIM.Services.Documentation
{
    public abstract class Tag : ITag
    {
        /// <summary>
        /// Indentation level of the tag.
        /// </summary>
        public int Indentation { get; private set; }

        /// <summary>
        /// Indent the tag by N levels (relative to current indentation).
        /// </summary>
        /// <param name="n">Number of levels by which the tag will be further indented.</param>
        public virtual void Indent(int n)
        {
            int indent = Indentation + n;
            if (indent < 0)
                throw new ArgumentException("Indentation level must be positive");
            Indentation = indent;
        }

        /// <summary>
        /// Constructs a Tag instance.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        public Tag(int indent)
        {
            if (indent < 0)
                throw new ArgumentException("Indentation level must be positive");
            Indentation = indent;
        }
    }
}
