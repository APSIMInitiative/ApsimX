namespace APSIM.Interop.Markdown
{
    public interface ITextStyle
    {
        bool Italic { get; }

        bool Strong { get; }

        bool Underline { get; }

        bool Strikethrough { get; }

        bool Superscript { get; }

        bool Subscript { get; }

        bool Quote { get; }

        bool Code { get; }

        ITextStyle MakeItalic();
        ITextStyle MakeStrong();
        ITextStyle MakeUnderline();
        ITextStyle MakeStrikethrough();
        ITextStyle MakeSuperscript();
        ITextStyle MakeSubscript();
        ITextStyle MakeQuote();
        ITextStyle MakeCode();
        ITextStyle MakeInlineCode();
        ITextStyle MakeEmphasis(char delimiterChar, int delimiterCount);
    }
}
