using System;
using System.Text;

namespace APSIM.Interop.Markdown
{
    /// <summary>
    /// Text styles.
    /// </summary>
    [Flags]
    public enum TextStyle
    {
        Normal = 0,
        Italic = 1,
        Strong = 2,
        Underline = 4,
        Strikethrough = 8,
        Superscript = 16,
        Subscript = 32,
        Quote = 64,
        Code = 128,
        Bibliography = 256
    }
}
