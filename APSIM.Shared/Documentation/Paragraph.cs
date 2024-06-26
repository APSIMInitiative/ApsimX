namespace APSIM.Shared.Documentation
{
    /// <summary>
    /// Describes an auto-doc paragraph command.
    /// </summary>
    public class Paragraph : ITag
    {
        /// <summary>The paragraph text.</summary>
        public string text  { get; private set; }

        /// <summary>The indent level.</summary>
        public int indent;

        /// <summary>The bookmark name (optional)</summary>
        public string bookmarkName;

        /// <summary>Should the paragraph indent all lines except the first?</summary>
        public bool handingIndent;

        /// <summary>
        /// Initializes a new instance of the <see cref="Paragraph"/> class.
        /// </summary>
        /// <param name="text">The paragraph text.</param>
        /// <param name="indent">The paragraph indent.</param>
        public Paragraph(string text, int indent = 0)
        {
            this.text = text;
            this.indent = indent;
        }
    }
}
