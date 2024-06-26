using System.Data;

namespace APSIM.Shared.Documentation
{
    /// <summary>
    /// Describes an auto-doc heading command.
    /// </summary>
    public class Heading : ITag
    {
        /// <summary>The heading text</summary>
        public string text;

        /// <summary>The heading level</summary>
        public int headingLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="Heading"/> class.
        /// </summary>
        /// <param name="text">The heading text.</param>
        /// <param name="headingLevel">The heading level.</param>
        public Heading(string text, int headingLevel)
        {
            this.text = text;
            this.headingLevel = headingLevel;
        }
    }
}
