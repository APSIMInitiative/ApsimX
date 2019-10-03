using OxyPlot;
using System;
using System.Drawing;

namespace Utility
{
    /// <summary>
    /// Colour Utility methods which do not belong in APSIM.Shared.
    /// </summary>
    public static class Colour
    {
        /// <summary>
        /// Translates a Gdk.Color into a System.Drawing.Color.
        /// </summary>
        /// <param name="colour">Colour to be translated.</param>
        /// <returns>The same colour as a System.Drawing.Color object.</returns>
        public static Color FromGtk(Gdk.Color colour)
        {
            return Color.FromArgb((int)(colour.Red / 65535.0 * 255), (int)(colour.Green / 65535.0 * 255), (int)(colour.Blue / 65535.0 * 255));
        }

        /// <summary>
        /// Translates a Gdk.Color into an OxyPlot.OxyColor.
        /// </summary>
        /// <param name="colour">Colour to be translated.</param>
        /// <returns>The same colour as an OxyColor object.</returns>
        public static OxyColor GtkToOxyColor(Gdk.Color colour)
        {
            return OxyColor.FromRgb((byte)(colour.Red / 65535.0 * 255), (byte)(colour.Green / 65535.0 * 255), (byte)(colour.Blue / 65535.0 * 255));
        }

        /// <summary>
        /// Gets a hex string representation of a colour, with a hash in front.
        /// e.g. #FF0000
        /// </summary>
        /// <param name="colour">Colour to be translated.</param>
        /// <returns>Hex string with a hash in front.</returns>
        public static string ToHex(Color colour)
        {
            return ColorTranslator.ToHtml(Color.FromArgb(colour.ToArgb()));
        }

        /// <summary>
        /// Translates a System.Drawing.Color to a Cairo.Color.
        /// </summary>
        /// <param name="colour">Colour to be translated.</param>
        /// <returns>The same colour as a Cairo.Color.</returns>
        public static Cairo.Color ToCairo(Color colour)
        {
            return new Cairo.Color(colour.A, colour.R, colour.G, colour.B);
        }

        /// <summary>
        /// Translates a System.Drawing.Color to an OxyColor.
        /// </summary>
        /// <param name="colour">Colour to be translated.</param>
        /// <returns>The same colour as a Cairo.Color.</returns>
        public static OxyColor ToOxy(Color colour)
        {
            return OxyColor.FromArgb(colour.A, colour.R, colour.G, colour.B);
        }

        internal static Cairo.Color ToCairo(Gdk.Color colour)
        {
            return ToCairo(FromGtk(colour));
        }

        internal static OxyColor ToOxy(Gdk.Color colour)
        {
            return ToOxy(FromGtk(colour));
        }
    }
}
