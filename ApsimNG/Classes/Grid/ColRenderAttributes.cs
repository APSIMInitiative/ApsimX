namespace UserInterface.Classes
{
    /// <summary>
    /// Small data structure to hold information about how a column should be rendered
    /// </summary>
    public class ColRenderAttributes
    {
        /// <summary>
        /// Whether the column data are read-only
        /// </summary>
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// Background colour for normal text rendering
        /// </summary>
        public Gdk.Color BackgroundColor;

        /// <summary>
        /// Foreground colour for normal text rendering
        /// </summary>
        public Gdk.Color ForegroundColor;
    }

}
