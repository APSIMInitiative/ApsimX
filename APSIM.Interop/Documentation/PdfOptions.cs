namespace APSIM.Interop.Documentation
{
    public class PdfOptions
    {
        /// <summary>
        /// Path at which to search for images to be included in the PDF.
        /// </summary>
        /// <value></value>
        public string ImagePath { get; private set; }

        /// <summary>
        /// Creates an empty <see cref="PdfOptions"/> instance.
        /// </summary>
        private PdfOptions()
        {
        }

        /// <summary>
        /// Create a <see cref="PdfOptions"/> instance.
        /// </summary>
        /// <param name="imagePath">Path at which to search for images to be included in the PDF.</param>
        public PdfOptions(string imagePath)
        {
            ImagePath = imagePath;
        }

        public static PdfOptions Default
        {
            get
            {
                return new PdfOptions();
            }
        }
    }
}