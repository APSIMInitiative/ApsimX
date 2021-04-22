using System;
using System.Text;

namespace APSIM.Interop.Markdown
{
    internal class TextStyle : ITextStyle
    {
        public bool Italic { get; private set; }

        public bool Strong { get; private set; }

        public bool Underline { get; private set; }

        public bool Strikethrough { get; private set; }

        public bool Superscript { get; private set; }

        public bool Subscript { get; private set; }

        public bool Quote { get; private set; }

        public bool Code { get; private set; }

        public TextStyle() { }

        private TextStyle(TextStyle baseStyle)
        {
            Italic = baseStyle.Italic;
            Strong = baseStyle.Strong;
            Underline = baseStyle.Underline;
            Strikethrough = baseStyle.Strikethrough;
            Superscript = baseStyle.Superscript;
            Subscript = baseStyle.Subscript;
            Quote = baseStyle.Quote;
            Code = baseStyle.Code;
        }

        public ITextStyle MakeCode() => new TextStyle(this) { Code = true };

        public ITextStyle MakeInlineCode() => throw new NotImplementedException();

        public ITextStyle MakeItalic() => new TextStyle(this) { Italic = true };

        public ITextStyle MakeQuote() => new TextStyle(this) { Quote = true };

        public ITextStyle MakeStrikethrough() => new TextStyle(this) { Strikethrough = true };

        public ITextStyle MakeStrong() => new TextStyle(this) { Strong = true };

        public ITextStyle MakeSubscript() => new TextStyle(this) { Subscript = true };

        public ITextStyle MakeSuperscript() => new TextStyle(this) { Superscript = true };

        public ITextStyle MakeUnderline() => new TextStyle(this) { Underline = true };

        public ITextStyle MakeEmphasis(char delimiter, int count)
        {
            switch (delimiter)
            {
                case '^':
                    return MakeSuperscript();
                case '~':
                    return count == 1 ? MakeSubscript() : MakeStrikethrough();
                default:
                    return count == 1 ? MakeItalic() : MakeStrong();
            }
        }
    }
}
