using System;

namespace APSIM.Services.Documentation
{
    /// <summary>
    /// Describes an auto-doc heading command.
    /// </summary>
    public class Heading : Tag
    {
        /// <summary>Heading level.</summary>
        private int headingLevel;

        /// <summary>The heading text</summary>
        public string Text { get; private set; }

        /// <summary>The heading level.</summary>
        public int HeadingLevel
        {
            get => headingLevel;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Heading level must be greater than zero");
                headingLevel = value;
            }
        }

        /// <summary>
        /// Indent the heading and increase the heading level.
        /// </summary>
        /// <param name="n">The relative change in heading level/indentation.</param>
        public override void Indent(int n)
        {
            base.Indent(n);
            HeadingLevel += n;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Heading"/> class.
        /// Heading level is set to indent + 1.
        /// </summary>
        /// <param name="text">The heading text.</param>
        /// <param name="indent">Indentation level.</param>
        public Heading(string text, int indent = 0) : base(indent)
        {
            Text = text;
            HeadingLevel = indent + 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Heading"/> class.
        /// </summary>
        /// <param name="text">The heading text.</param>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">The heading level.</param>
        public Heading(string text, int indent, int headingLevel) : base(indent)
        {
            Text = text;
            HeadingLevel = headingLevel;
        }
    }
}
