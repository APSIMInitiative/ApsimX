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
        private const double pointsToPixels = 96.0 / 72;
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
            Paragraph paragraph = doc as Paragraph ?? section.AddParagraph();

            // The image could potentially be too large. Therfore we read it,
            // adjust the size to fit the page better (if necessary), and add
            // the modified image to the paragraph.

            // fixme - ResizeImage() expects units in pixels
            GetPageSize(section, out double pageWidth, out double pageHeight);
#if NETFRAMEWORK
            string path = Path.ChangeExtension(Path.GetTempFileName(), ".png");
            ReadAndResizeImage(fullPath, pageWidth, pageHeight).Save(path, ImageFormat.Png);
            section.AddImage(path);
#else
            if (paragraph != null)
            {
                // Note: the first argument passed to the FromStream() function
                // is the name of the image. Thist must be unique throughout the document,
                // otherwise you will run into problems with duplicate imgages.
                paragraph.AddImage(ImageSource.FromStream(fullPath, () =>
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
        /// Get the page size in pixels.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="pageWidth"></param>
        /// <param name="pageHeight"></param>
        private static void GetPageSize(Section section, out double pageWidth, out double pageHeight)
        {
            PageSetup pageSetup = section.PageSetup;
            if (pageSetup.PageWidth.Point == 0 || pageSetup.PageHeight.Point == 0)
                pageSetup = section.Document.DefaultPageSetup;
            pageWidth = (pageSetup.PageWidth.Point - pageSetup.LeftMargin.Point - pageSetup.RightMargin.Point) * pointsToPixels;
            pageHeight = (pageSetup.PageHeight.Point - pageSetup.TopMargin.Point - pageSetup.BottomMargin.Point) * pointsToPixels;
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
            // Using Image.FromFile() will cause the file to be locked until the image is disposed.
            // This is a hack to read the file and ensure the file is not locked afterward.
            Image image;
            using (Bitmap bmp = new Bitmap(fullPath))
                image = new Bitmap(bmp);
            if ( (targetWidth > 0 && image.Width > targetWidth) || (targetHeight > 0 && image.Height > targetHeight) )
                image = ImageUtilities.ResizeImage(image, targetWidth, targetHeight);
            return image;
        }
    }
}