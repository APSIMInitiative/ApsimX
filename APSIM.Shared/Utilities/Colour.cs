namespace APSIM.Shared.Utilities
{
    using System;
    using System.Drawing;

    /// <summary>
    /// Colour utility class
    /// </summary>
    public class ColourUtilities
    {
        /// <summary>
        ///     Colour pallette optimised for colour blindness.
        /// </summary>
        /// <remarks>
        ///     Colours come from:
        ///     Wong, B. (2011) Color blindness, Nature Methods, Vol 8, No. 6.
        /// </remarks>
        public static Color[] Colours = {
                                        Color.FromArgb(0,0,0),           // black
                                        Color.FromArgb(230, 159, 0),     // orange
                                        Color.FromArgb(86, 180, 233),    // sky blue
                                        Color.FromArgb(0, 158, 115),     // bluish green
                                        Color.FromArgb(0, 114, 178),     // blue
                                        Color.FromArgb(213, 94, 0),      // reddish purple
                                        Color.FromArgb(204, 121, 167),   // vermillion
                                        Color.FromArgb(240, 228, 66),    // yellow
                                         };


        /// <summary>
        /// Choose a colour from the color palette
        /// </summary>
        /// <param name="colourNumber">The colour number to choose</param>
        /// <returns></returns>
        public static Color ChooseColour(int colourNumber)
        {
            if (colourNumber >= Colours.Length)
                System.Math.DivRem(colourNumber, Colours.Length, out colourNumber);
            return Colours[colourNumber];
        }        
        
        /// <summary>
        /// Creates color with corrected brightness.
        /// </summary>
        /// <param name="color">Color to correct.</param>
        /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
        /// Negative values produce darker colors.</param>
        /// <returns>
        /// Corrected <see cref="Color"/> structure.
        /// </returns>
        public static Color ChangeColorBrightness(Color color, double correctionFactor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= (float)correctionFactor;
                green *= (float)correctionFactor;
                blue *= (float)correctionFactor;
            }
            else
            {
                red = (float)((255 - red) * correctionFactor + red);
                green = (float)((255 - green) * correctionFactor + green);
                blue = (float)((255 - blue) * correctionFactor + blue);
            }

            return Color.FromArgb(color.A, 
                                  (int)Math.Min(255, red), 
                                  (int)Math.Min(255, green), 
                                  (int)Math.Min(255, blue));
        }
    }
}
