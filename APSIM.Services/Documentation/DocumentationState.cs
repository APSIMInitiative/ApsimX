using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Services.Documentation.Extensions;

namespace APSIM.Services.Documentation
{
    /// <summary>
    /// An instance of this class is passed around during the recursive calls to
    /// the autodocumentation methods, to track state such as indentation and
    /// heading levels.
    /// </summary>
    public class DocumentationState
    {
        /// <summary>
        /// Create a new <see cref="DocumentationState"/> instance with the given state.
        /// This is intended for internal use only.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingIndices">Heading indices.</param>
        private DocumentationState(uint indent, IEnumerable<uint> headingIndices)
        {
            Indentation = indent;
            HeadingIndices = headingIndices;
        }

        /// <summary>
        /// Create a new <see cref="DocumentationState"/> instance with default settings
        /// suitable for a toplevel model.
        /// </summary>
        public DocumentationState()
        {
            Indentation = 0;
            HeadingIndices = 1u.ToEnumerable();
        }

        /// <summary>
        /// Indentation level of the tag.
        /// </summary>
        public uint Indentation { get; private set; }

        /// <summary>
        /// Heading indices. These are used to construct heading tags.
        /// </summary>
        public IEnumerable<uint> HeadingIndices { get; private set; }

        /// <summary>
        /// Create a new <see cref="DocumentationState"/> instance, which is indented by 1 level.
        /// </summary>
        public DocumentationState Indent()
        {
            return new DocumentationState(Indentation + 1, HeadingIndices);
        }

        /// <summary>
        /// Create a new <see cref="DocumentationState"/> instance, which contains an extra subheading level.
        /// </summary>
        public DocumentationState CreateSubHeading()
        {
            return new DocumentationState(Indentation, HeadingIndices.Append(1u));
        }

        /// <summary>
        /// Increment the final heading index.
        /// This should only really be called from the constructor of
        /// <see cref="Heading"/>.
        /// </summary>
        internal void IncrementHeadingIndex()
        {
            if (HeadingIndices.Any())
            {
                List<uint> indices = HeadingIndices.ToList();
                indices[indices.Count - 1] += 1;
                HeadingIndices = indices;
            }
        }
    }
}
