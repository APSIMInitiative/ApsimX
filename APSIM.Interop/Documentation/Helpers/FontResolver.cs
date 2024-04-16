
using PdfSharpCore.Fonts;



using PdfSharpCore.Drawing;

using FontHelper = APSIM.Interop.Documentation.Helpers.FontHelper;

namespace APSIM.Interop.Documentation.Helpers
{
    internal class FontResolver : IFontResolver
    {
        public string DefaultFontName => "Arial";

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Ignore case of font names.
            var name = familyName.ToLower();

            // Deal with the fonts we know.
            if (name.StartsWith("courier"))
                return new FontResolverInfo("Courier#");
            else
            {
                if (isBold)
                {
                    if (isItalic)
                        return new FontResolverInfo("Arial#bi");
                    return new FontResolverInfo("Arial#b");
                }
                if (isItalic)
                    return new FontResolverInfo("Arial#i");
                return new FontResolverInfo("Arial#");
            }
        }

        /// <summary>
        /// Return the font data for the fonts.
        /// </summary>
        public byte[] GetFont(string faceName)
        {
            switch (faceName)
            {
                case "Courier#":
                    return FontHelper.Courier;

                case "Arial#":
                    return FontHelper.Arial;

                case "Arial#b":
                    return FontHelper.ArialBold;

                case "Arial#i":
                    return FontHelper.ArialItalic;

                case "Arial#bi":
                    return FontHelper.ArialBoldItalic;
            }
            return null;
        }
    }
}
