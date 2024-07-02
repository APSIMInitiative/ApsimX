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

    /// <summary>Constrains the rectangle to the given width and height.</summary>
    /// <param name="width">Width of a window in pixels.</param>
    /// <param name="height">Height of a window in pixels.</param>
    public static Rectangle Clip(this Rectangle r, int width, int height)
    {
        if (r.Right > width || r.Bottom > height)
            return new Rectangle(r.Left, r.Top, width - r.Left, height - r.Top);
        else
            return r;
    }      
}