// -----------------------------------------------------------------------
// <copyright file="ImageUtilities.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace APSIM.Shared.Utilities
{
    using System;
    using System.Drawing;

    /// <summary>
    /// Colour utility class
    /// </summary>
    public class ImageUtilities
    {
        /// <summary>
        /// Resize the specified image, returning a new one. Will keep the
        /// aspect ratio.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="targetWidth">The width to aim for when rescaling.</param>
        /// <param name="targetHeight">The height to aim for when rescaling.</param>
        public static Image ResizeImage(Image image, double targetWidth, double targetHeight)
        {
            // Determine scaling.
            double scale = Math.Min(targetWidth / image.Width, targetHeight / image.Height);
            var scaleWidth = (int)(image.Width * scale);
            var scaleHeight = (int)(image.Height * scale);
            var scaleRectangle = new Rectangle(((int)targetWidth - scaleWidth) / 2, ((int)targetHeight - scaleHeight) / 2, scaleWidth, scaleHeight);

            // Create a scaled image.
            Bitmap scaledImage = new Bitmap((int)targetWidth, (int)targetHeight);
            using (var graph = Graphics.FromImage(scaledImage))
            {
                graph.DrawImage(image, scaleRectangle);
            }

            return scaledImage;
        }
    }
}
