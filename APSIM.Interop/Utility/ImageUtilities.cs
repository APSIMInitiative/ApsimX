using System;
using System.Drawing;
using System.IO;
using DocumentFormat.OpenXml.Drawing.Charts;
using SkiaSharp;
using Svg;

namespace APSIM.Interop.Utility
{
    public static class ImageUtilities
    {
        /// <summary>
        /// Resize the specified image ensuring that it fits within the specified target
        /// width and height without changing the aspect ratio. The return value will
        /// be a new image instance.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="targetWidth">The width to aim for when rescaling.</param>
        /// <param name="targetHeight">The height to aim for when rescaling.</param>
        public static SkiaSharp.SKImage ResizeImage(SkiaSharp.SKImage image, double targetWidth, double targetHeight)
        {
            // Determine scaling.
            double scale = Math.Min(targetWidth / image.Width, targetHeight / image.Height);
            var scaleWidth = (int)(image.Width * scale);
            var scaleHeight = (int)(image.Height * scale);

            // Create a scaled image.
            using (var surface = SKSurface.Create(new SKImageInfo { Width = scaleWidth, Height = scaleHeight, ColorType = SKImageInfo.PlatformColorType, AlphaType = SKAlphaType.Premul }))
                using (var paint = new SKPaint())
                {
                    // high quality with antialiasing
                    paint.IsAntialias = true;
                    paint.FilterQuality = SKFilterQuality.High;

                    // draw the bitmap to fill the surface
                    surface.Canvas.DrawImage(image, new SKRectI(0, 0, scaleWidth, scaleHeight), paint);
                    surface.Canvas.Flush();

                return surface.Snapshot();
                }
        }

        /// <summary>
        /// Convert an svg image into a raster.
        /// </summary>
        /// <param name="stream">The input svg stream.</param>
        /// <param name="width">Desired image width.</param>
        /// <param name="height">Desired image height.</param>
        /// <remarks>
        /// If one of width or height is zero, the returned image will have
        /// its "natural" aspect ratio.
        /// </remarks>
        public static Image ReadSvg(Stream stream, int width, int height)
        {
            SvgDocument doc = SvgDocument.Open<SvgDocument>(stream);
            return doc.Draw(width, height);
        }
    }
}
