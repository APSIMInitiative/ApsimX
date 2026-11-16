using Models.Core;

namespace APSIM.Documentation.Tag
{
    /// <summary>
    /// This class describes a header for a document - it has a title and should appear at the top of the document
    /// </summary>
    public class Header : ITag
    {
        /// <summary>Model that this header was created from</summary>
        public IModel Reference { get; private set; }

        /// <summary>The Header title.</summary>
        public string Title { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Header"/> class.
        /// </summary>
        /// <param name="title">The header title.</param>
        /// <param name="reference">Model that this header is made from</param>
        public Header(string title, IModel reference)
        {
            Title = title;
            Reference = reference;
        }
    }
}
