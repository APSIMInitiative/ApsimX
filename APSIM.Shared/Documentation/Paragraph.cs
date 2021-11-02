namespace APSIM.Shared.Documentation
{
    /// <summary>
    /// Describes an auto-doc paragraph command.
    /// </summary>
    public class Paragraph : ITag
    {
        /// <summary>The paragraph text.</summary>
        public string Text { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Paragraph"/> class.
        /// </summary>
        /// <param name="text">The paragraph contents.</param>
        public Paragraph(string text) => Text = text;
    }
}
