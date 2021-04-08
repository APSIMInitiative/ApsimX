namespace APSIM.Services.Documentation
{
    /// <summary>
    /// Describes an auto-doc paragraph command.
    /// </summary>
    public class Paragraph : Tag
    {
        /// <summary>The paragraph text.</summary>
        public string Text { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Paragraph"/> class.
        /// </summary>
        /// <param name="text">The paragraph text.</param>
        /// <param name="indent">Indentation level.</param>
        public Paragraph(string text, int indent = 0) : base(indent) => Text = text;
    }
}
