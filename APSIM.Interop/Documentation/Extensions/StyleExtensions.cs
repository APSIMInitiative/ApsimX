using System;
#if NETCOREAPP
using MigraDocCore.DocumentObjectModel;
#else
using MigraDoc.DocumentObjectModel;
#endif

namespace APSIM.Interop.Documentation.Extensions
{
    internal static class StyleExtensions
    {
        /// <summary>
        /// Creates a copy of the specified style with font size
        /// appropriately set for a heading.
        /// </summary>
        /// <param name="style">Style to be copied.</param>
        /// <param name="level">Heading level (>0).</param>
        public static Style MakeHeading(this Style style, int level)
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException("Heading level must be greater than 0");

            style = style.Clone();
            if (level == 1)
            {
                style.Font.Size = 14;
                style.Font.Bold = true;
            }
            else if (level == 2)
                style.Font.Size = 12;
            else if (level == 3)
                style.Font.Size = 11;
            else if (level == 4)
                style.Font.Size = 10;
            else if (level == 5)
                style.Font.Size = 9;
            else if (level == 6)
                style.Font.Size = 8;
            return style;
        }

        /// <summary>
        /// Creates an indented copy of the specified style.
        /// </summary>
        /// <param name="style">The style to be copied.</param>
        /// <param name="level">Indentation level (>0).</param>
        public static Style Indent(this Style style, int level)
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException("Heading level must be greater than 0");

            style = style.Clone();
            style.ParagraphFormat.LeftIndent = Unit.FromCentimeter(level);
            return style;
        }

        /// <summary>
        /// Creates a quote style which is a based on a copy of the given style.
        /// </summary>
        /// <param name="style">The style to be copied.</param>
        public static Style MakeQuote(this Style style)
        {
            // fixme
            style = style.Clone();
            style.ParagraphFormat.LeftIndent = Unit.FromCentimeter(1);
            //style.ParagraphFormat.Shading.Color = ...
            return style;
        }

        /// <summary>
        /// Create an italic style based on the given style.
        /// </summary>
        /// <param name="style">The style to be copied.</param>
        public static Style MakeItalic(this Style style)
        {
            style = style.Clone();
            style.Font.Italic = true;
            return style;
        }

        /// <summary>
        /// Create a bold style based on the given style.
        /// </summary>
        /// <param name="style">The style to be copied.</param>
        public static Style MakeBold(this Style style)
        {
            style = style.Clone();
            style.Font.Bold = true;
            return style;
        }

        /// <summary>
        /// Create a monospaced style (appropriate for code segments) based on the given style.
        /// </summary>
        /// <param name="style">The style to be copied.</param>
        public static Style MakeCode(this Style style)
        {
            style = style.Clone();
            style.ParagraphFormat.LeftIndent = Unit.FromCentimeter(1);
            style.Font.Name = "monospace";
            return style;
        }

        /// <summary>
        /// Create a style appropriate for an inline code segments based on the given style.
        /// </summary>
        /// <param name="style">The style to be copied.</param>
        public static Style MakeInlineCode(this Style style)
        {
            style = style.Clone();
            style.Font.Name = "monospace";
            return style;
        }

        /// <summary>
        /// Create a subscript style based on the given style.
        /// </summary>
        /// <param name="style">The style to be copied.</param>
        public static Style MakeSubscript(this Style style)
        {
            style = style.Clone();
            style.Font.Subscript = true;
            return style;
        }

        /// <summary>
        /// Create a superscript style based on the given style.
        /// </summary>
        /// <param name="style">The style to be copied.</param>
        public static Style MakeSuperscript(this Style style)
        {
            style = style.Clone();
            style.Font.Superscript = true;
            return style;
        }

        /// <summary>
        /// Create a strikethrough style based on the given style.
        /// </summary>
        /// <param name="style">The style to be copied.</param>
        public static Style MakeStrikethrough(this Style style)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create an emphasis style based on the given style and the
        /// character used to define the emphasis inline in the markdown.
        /// E.g. if delimiter is '*' and count is 1, then the result will
        /// be an italic style.
        /// </summary>
        /// <param name="style">The style to be copied.</param>
        /// <param name="delimiter">The markdown delimiter character used to create the emphasis inline.</param>
        /// <param name="count">The number of delimiter characters used to create the emphasis inline.</param>
        public static Style MakeEmphasis(this Style style, char delimiter, int count)
        {
            switch (delimiter)
            {
                case '^':
                    style = style.MakeSuperscript();
                    break;
                case '~':
                    style = count == 1 ? style.MakeSubscript() : style.MakeStrikethrough();
                    break;
                default:
                    style = count == 1 ? style.MakeItalic() : style.MakeBold();
                    break;
            }
            return style;
        }

        /// <summary>
        /// Create a hyperlink style based on the given style.
        /// </summary>
        /// <param name="style">The style to be copied.</param>
        public static Style MakeLink(this Style style)
        {
            style = style.Clone();
            style.Font.Underline = Underline.Single;
            style.Font.Color = new Color(0, 0, 0xff);
            return style;
        }

        public static TextFormat GetTextFormat(this Style style)
        {
            TextFormat format = style.Font.Bold ? TextFormat.Bold : TextFormat.NotBold;
            format |= style.Font.Italic ? TextFormat.Italic : TextFormat.NotItalic;
            format |= style.Font.Underline != Underline.None ? TextFormat.Underline : TextFormat.NoUnderline;
            return format;
        }
    }
}