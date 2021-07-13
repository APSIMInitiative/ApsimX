using System;
using System.Collections;
using System.Collections.Generic;
using APSIM.Services.Documentation.Extensions;

namespace APSIM.Services.Documentation
{
    /// <summary>
    /// This class describes a section in a document - it has a title and contains
    /// multiple child tags.
    /// </summary>
    public class Section : ITag
    {
        /// <summary>Child tags.</summary>
        public IEnumerable<ITag> Children { get; private set; }

        /// <summary>The section title.</summary>
        public string Title { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Section"/> class.
        /// </summary>
        /// <param name="title">The section title. Can be null.</param>
        /// <param name="children">The child tags.</param>
        public Section(string title, IEnumerable<ITag> children)
        {
            Title = title;
            this.Children = children;
        }

        /// <summary>
        /// Initializes a new <see cref="Section"/> with no title.
        /// </summary>
        /// <param name="children">The child tags.</param>
        public Section(IEnumerable<ITag> children)
        {
            Title = null;
            this.Children = children;
        }

        /// <summary>
        /// Create a <see cref="Section"/> instance with a single child.
        /// </summary>
        /// <param name="title">The section title. Can be null.</param>
        /// <param name="children">The child tags.</param>
        public Section(string title, ITag child)
        {
            Title = title;
            Children = child.ToEnumerable();
        }
    }
}
