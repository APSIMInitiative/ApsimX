namespace APSIM.Services.Documentation
{
    /// <summary>A tag which displays an image.</summary>
    public class Image : ITag
    {
        /// <summary>The image to put into the doc.</summary>
        public System.Drawing.Image Raster { get; private set; }

        /// <summary>
        /// Create an Image tag instance.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="indent">Indentation level.</param>
        public Image(System.Drawing.Image image) => Raster = image;
    }
}
