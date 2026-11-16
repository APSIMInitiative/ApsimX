using Models.Core;

namespace APSIM.Documentation.Tag
{
    /// <summary>
    /// Describes an auto-doc paragraph command.
    /// </summary>
    public class Paragraph : ITag
    {
        /// <summary>Model that this paragraph was created from</summary>
        public IModel Reference { get; private set; }

        /// <summary>The paragraph text.</summary>
        public string Text  { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Paragraph"/> class.
        /// </summary>
        /// <param name="text">The paragraph text.</param>
        /// <param name="reference">Model that this paragraph is made from</param>
        public Paragraph(string text, IModel reference)
        {
            Text = text;
            Reference = reference;
        }
    }
}
