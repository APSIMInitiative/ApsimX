using System;

namespace APSIM.Services.Documentation
{
    public abstract class Tag : ITag
    {
        /// <summary>
        /// Indentation level of the tag.
        /// </summary>
        public uint Indentation { get; private set; }

        /// <summary>
        /// Indent the tag by N levels (relative to current indentation).
        /// </summary>
        /// <param name="n">Number of levels by which the tag will be further indented.</param>
        public virtual void Indent(uint n)
        {
            uint indent = Indentation + n;
            Indentation = indent;
        }

        /// <summary>
        /// Constructs a Tag instance.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        public Tag(uint indent)
        {
            Indentation = indent;
        }
    }
}
