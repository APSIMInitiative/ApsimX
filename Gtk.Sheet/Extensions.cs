using System.Drawing;

namespace Gtk.Sheet;

internal static class Extensions
{
    /// <summary>
    /// Translates a System.Drawing.Color to a Cairo.Color.
    /// </summary>
    /// <param name="colour">Colour to be translated.</param>
    /// <returns>The same colour as a Cairo.Color.</returns>
    public static Cairo.Color ToCairo(this Color colour)
    {
        return new Cairo.Color(colour.R / 255.0, colour.G / 255.0, colour.B / 255.0);
    }   
}