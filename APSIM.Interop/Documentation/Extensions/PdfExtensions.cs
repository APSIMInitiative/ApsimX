namespace APSIM.Interop.Documentation.Extensions
{
    using System;
    using System.Drawing;
    using System.IO;
    using APSIM.Shared.Utilities;
    using System.Drawing.Imaging;
#if NETFRAMEWORK
    using MigraDoc.DocumentObjectModel;
    using MigraDoc.DocumentObjectModel.Tables;
#else
    using MigraDocCore.DocumentObjectModel;
    using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
    using static MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes.ImageSource;
    using MigraDocCore.DocumentObjectModel.Tables;
#endif
    /// <summary>
    /// Extension methods for PdfSharp DOM objects.
    /// </summary>
    internal static class PdfExtensions
    {
        private const double pointsToPixels = 96.0 / 72;

        /// <summary>
        /// Adds an image to the specified document object, resizing the
        /// image as necessary to ensure that it fits on the page while
        /// maintaining the image's aspect ratio.
        /// </summary>
        /// <param name="doc">The document object.</param>
        /// <param name="image">Image to be added.</param>
        public static void AddResizeImage(this DocumentObject doc, Image image)
        {
            Section section = doc.Section ?? doc as Section;
            if (section == null)
                return;
            Paragraph paragraph = doc as Paragraph ?? (doc as Cell)?.AddParagraph() ?? section.AddParagraph();

            // The image could potentially be too large. Therfore we read it,
            // adjust the size to fit the page better (if necessary), and add
            // the modified image to the paragraph.

            // fixme - ResizeImage() expects units in pixels
            GetPageSize(section, out double pageWidth, out double pageHeight);
#if NETFRAMEWORK
            string path = Path.ChangeExtension(Path.GetTempFileName(), ".png");
            ReadAndResizeImage(image, pageWidth, pageHeight).Save(path, ImageFormat.Png);
            section.AddImage(path);
#else
            if (paragraph != null)
            {
                // Note: the first argument passed to the FromStream() function
                // is the name of the image. Thist must be unique throughout the document,
                // otherwise you will run into problems with duplicate imgages.
                paragraph.AddImage(ImageSource.FromStream(Guid.NewGuid().ToString().Replace("-", ""), () =>
                {
                    image = ReadAndResizeImage(image, pageWidth, pageHeight);
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
        /// <param name="image">The image to be resized.</param>
        /// <param name="targetWidth">Max allowed width of the image in pixels.</param>
        /// <param name="targetHeight">Max allowed height of the image in pixels.</param>
        private static Image ReadAndResizeImage(Image image, double targetWidth, double targetHeight)
        {
            if ( (targetWidth > 0 && image.Width > targetWidth) || (targetHeight > 0 && image.Height > targetHeight) )
                image = ImageUtilities.ResizeImage(image, targetWidth, targetHeight);
            return image;
        }
    }
}
