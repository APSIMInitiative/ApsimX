using System;
using System.IO;
using System.Reflection;

namespace APSIM.Interop.Documentation.Helpers
{
    /// <summary>
    /// Helper class that reads font data from embedded resources.
    /// </summary>
    internal static class FontHelper
    {
        public static byte[] Courier
        {
            get { return LoadFontData("APSIM.Interop.Resources.Fonts.cour.ttf"); }
        }

        // Make sure the fonts have compile type "Embedded Resource". Names are case-sensitive.
        public static byte[] Arial
        {
            get { return LoadFontData("APSIM.Interop.Resources.Fonts.arial.ttf"); }
        }

        public static byte[] ArialBold
        {
            get { return LoadFontData("APSIM.Interop.Resources.Fonts.arialbd.ttf"); }
        }

        public static byte[] ArialItalic
        {
            get { return LoadFontData("APSIM.Interop.Resources.Fonts.ariali.ttf"); }
        }

        public static byte[] ArialBoldItalic
        {
            get { return LoadFontData("APSIM.Interop.Resources.Fonts.arialbi.ttf"); }
        }

        /// <summary>
        /// Returns the specified font from an embedded resource.
        /// </summary>
        private static byte[] LoadFontData(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                    throw new ArgumentException("No resource with name " + name);

                int count = (int)stream.Length;
                byte[] data = new byte[count];
                stream.Read(data, 0, count);
                return data;
            }
        }
    }
}
