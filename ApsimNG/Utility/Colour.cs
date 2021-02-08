using APSIM.Shared.Utilities;
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
        /// Translates a System.Drawing.Color to a Gdk.Color.
        /// </summary>
        /// <param name="colour">Colour to be translated.</param>
        public static Gdk.Color ToGdk(this Color colour)
        {
            return new Gdk.Color(colour.R, colour.G, colour.B);
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
#if NETCOREAPP
        /// <summary>
        /// Convert a System.Drawing.Color to a Gdk.RGBA.
        /// </summary>
        /// <param name="colour">The colour to be converted.</param>
        /// <returns></returns>
        internal static Gdk.RGBA ToRGBA(this Color colour)
        {
            return new Gdk.RGBA()
            {
                Red = 1.0 * colour.R / 0xff,
                Green = 1.0 * colour.G / 0xff,
                Blue = 1.0 * colour.B / 0xff,
                Alpha = 1.0 * colour.A / 0xff
            };
        }

        /// <summary>
        /// Convert a System.Drawing.Color to a Gdk.RGBA.
        /// </summary>
        /// <param name="colour">The colour to be converted.</param>
        /// <returns></returns>
        internal static Color ToColour(this Gdk.RGBA colour)
        {
            return Color.FromArgb
            (
                Convert.ToInt32(MathUtilities.Bound(colour.Alpha, 0, 1) * 0xff),
                Convert.ToInt32(MathUtilities.Bound(colour.Red, 0, 1) * 0xff),
                Convert.ToInt32(MathUtilities.Bound(colour.Green, 0, 1) * 0xff),
                Convert.ToInt32(MathUtilities.Bound(colour.Blue, 0, 1) * 0xff)
            );
        }
#endif
    }
}
