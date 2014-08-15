// -----------------------------------------------------------------------
// <copyright file="IGraphView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Drawing;

    /// <summary>
    /// Colour utility class
    /// </summary>
    public class Colour
    {
        private static Color[] colours = {
                                        Color.FromArgb(228,26,28),
                                        Color.FromArgb(55,126,184),
                                        Color.FromArgb(77,175,74),
                                        Color.FromArgb(152,78,163),
                                        Color.FromArgb(255,127,0),
                                        Color.FromArgb(255,255,51),
                                        Color.FromArgb(166,86,40),
                                        Color.FromArgb(247,129,191),
                                        Color.FromArgb(153,153,153),
                                        Color.FromArgb(251,180,174),
                                        Color.FromArgb(179,205,227),
                                        Color.FromArgb(204,235,197),
                                        Color.FromArgb(222,203,228),
                                        Color.FromArgb(254,217,166),
                                        Color.FromArgb(255,255,204),
                                        Color.FromArgb(229,216,189),
                                        Color.FromArgb(253,218,236),
                                        Color.FromArgb(242,242,242)                                         
                                         };


        /// <summary>
        /// Choose a colour from the color palette
        /// </summary>
        /// <param name="colourNumber">The colour number to choose</param>
        /// <returns></returns>
        public static Color ChooseColour(int colourNumber)
        {
            if (colourNumber >= colours.Length)
                System.Math.DivRem(colourNumber, colours.Length, out colourNumber);
            return colours[colourNumber];
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

            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }
    }
}
