using System.Collections.Generic;
using System.Linq;
using Models.Core;

namespace APSIM.Documentation.Tag
{
    /// <summary>
    /// This class describes a section in a document - it has a title and contains
    /// multiple child tags.
    /// </summary>
    public class Section : ITag
    {
        /// <summary>Model that this section was created from</summary>
        public IModel Reference { get; private set; }

        /// <summary>Child tags.</summary>
        public List<ITag> Children { get; set; }

        /// <summary>The section title.</summary>
        public string Title { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Section"/> class.
        /// </summary>
        /// <param name="title">The section title. Can be null.</param>
        /// <param name="children">The child tags.</param>
        /// <param name="reference">Model that this section is made from</param>
        public Section(string title, IEnumerable<ITag> children, IModel reference)
        {
            Title = title;
            Children = children.ToList();
            Reference = reference;
        }

        /// <summary>
        /// Initializes a new <see cref="Section"/> with no title.
        /// </summary>
        /// <param name="children">The child tags.</param>
        /// <param name="reference">Model that this section is made from</param>
        public Section(IEnumerable<ITag> children, IModel reference)
        {
            Title = null;
            Children = children.ToList();
            Reference = reference;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Section"/> class.
        /// </summary>
        /// <param name="title">The section title. Can be null.</param>
        /// <param name="reference">Model that this section is made from</param>
        public Section(string title, IModel reference)
        {
            Title = title;
            Children = new List<ITag>();
            Reference = reference;
        }

        /// <summary>
        /// Create a <see cref="Section"/> instance with a single child.
        /// </summary>
        /// <param name="title">The section title. Can be null.</param>
        /// <param name="child">The child tag.</param>
        /// <param name="reference">Model that this section is made from</param>
        public Section(string title, ITag child, IModel reference)
        {
            Title = title;
            Children = new List<ITag>() {child};
            Reference = reference;
        }

        /// <summary>
        /// Check if a section is empty. A section is defined as non-empty
        /// iff it contains any content (ie any non-section children, or
        /// any non-section children of section children etc).
        /// </summary>
        public bool IsEmpty()
        {
            foreach (ITag tag in Children)
            {
                if (tag is Section section)
                {
                    // If the child section is non-empty, then this
                    // section is non-empty.
                    if (!section.IsEmpty())
                        return false;
                }
                else
                    // If the section contains any children which aren't
                    // a section, then this section is non-empty.
                    return false;
            }
            return true;
        }

        /// <summary>Adds an ITag as a child of this ITag</summary>
        public void Add(ITag tag) {
            Children.Add(tag);
        }

        /// <summary>Adds a list of ITags as a children of this ITag</summary>
        public void Add(IEnumerable<ITag> tags) {
            Children.AddRange(tags);
        }
    }
}
