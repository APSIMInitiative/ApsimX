namespace UserInterface.Extensions
{
    using System;
    using System.Drawing;
    using System.IO;
    using APSIM.Shared.Utilities;
    using System.Drawing.Imaging;
#if NETFRAMEWORK
    using MigraDoc.DocumentObjectModel;
#else
    using MigraDocCore.DocumentObjectModel;
    using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
#endif
    /// <summary>
    /// Extension methods for PdfSharp DOM objects.
    /// </summary>
    public static class PdfExtensions
    {
        /// <summary>
        /// Adds an image to the specified document object, resizing the
        /// image as necessary to ensure that it fits on the page.
        /// </summary>
        /// <param name="doc">The document object.</param>
        /// <param name="fullPath">Path to the image on disk.</param>
        public static void AddResizeImage(this DocumentObject doc, string fullPath)
        {
            Section section = doc.Section ?? doc as Section;
            if (section == null)
                return;

            // The image could potentially be too large. Therfore we read it,
            // adjust the size to fit the page better (if necessary), and add
            // the modified image to the paragraph.

            // fixme - ResizeImage() expects units in pixels
            double pageWidth = section.PageSetup.PageWidth.Point;
            double pageHeight = section.PageSetup.PageHeight.Point;
            if (pageWidth == 0 && pageHeight == 0)
            {
                pageWidth = doc.Document.DefaultPageSetup.PageWidth.Point;
                pageHeight = doc.Document.DefaultPageSetup.PageHeight.Point;
            }
#if NETFRAMEWORK
            string path = Path.ChangeExtension(Path.GetTempFileName(), ".png");
            ReadAndResizeImage(fullPath, pageWidth, pageHeight).Save(path, ImageFormat.Png);
            section.AddImage(path);
#else
            if (section != null)
            {
                Paragraph paragraph = section.AddParagraph();
                paragraph.AddImage(ImageSource.FromStream("img", () =>
                {
                    Image image = ReadAndResizeImage(fullPath, pageWidth, pageHeight);
                    Stream stream = new MemoryStream();
                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Seek(0, SeekOrigin.Begin);
                    return stream;
                }));
            }
#endif
        }

        /// <summary>
        /// Ensures that an image's dimensions are less than the specified target
        /// width and height, resizing the image as necessary without changing the
        /// aspect ratio.
        /// </summary>
        /// <param name="fullPath">Path to the image on disk.</param>
        /// <param name="targetWidth">Max allowed width of the image in pixels.</param>
        /// <param name="targetHeight">Max allowed height of the image in pixels.</param>
        private static Image ReadAndResizeImage(string fullPath, double targetWidth, double targetHeight)
        {
            var image = System.Drawing.Image.FromFile(fullPath);
            if ( (targetWidth > 0 && image.Width > targetWidth) || (targetHeight > 0 && image.Height > targetHeight) )
                image = ImageUtilities.ResizeImage(image, targetWidth, targetHeight);
            return image;
        }
    }
}