namespace UserInterface.Interfaces
{
    /// <summary>
    /// An interface for a grid column.
    /// </summary>
    public interface IGridColumn
    {
        /// <summary>
        /// Gets the column index of the column
        /// </summary>
        int ColumnIndex { get; }

        /// <summary>
        /// Gets or sets the column width in pixels. A value of -1 indicates auto sizing.
        /// </summary>
        int Width { get; set; }

        /// <summary>
        /// Gets or sets the minimum column width in pixels.
        /// </summary>
        int MinimumWidth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is left aligned. If not then right is assumed.
        /// </summary>
        bool LeftJustification { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is read only.
        /// </summary>
        bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the column format e.g. N3
        /// </summary>
        string Format { get; set; }

        /// <summary>
        /// Gets or sets the background color
        /// </summary>
        System.Drawing.Color BackgroundColour { get; set; }

        /// <summary>
        /// Gets or sets the foreground color
        /// </summary>
        System.Drawing.Color ForegroundColour { get; set; }

        /// <summary>
        /// Gets or sets the column tool tip.
        /// </summary>
        string ToolTip { get; set; }

        /// <summary>
        /// Gets or sets the header background color
        /// </summary>
        System.Drawing.Color HeaderBackgroundColour { get; set; }

        /// <summary>
        /// Gets or sets the header foreground color
        /// </summary>
        System.Drawing.Color HeaderForegroundColour { get; set; }

        /// <summary>
        /// Gets or sets the text of the header
        /// </summary>
        string HeaderText { get; set; }

        /// <summary>Gets or sets the left justification of the header</summary>
        bool HeaderLeftJustification { get; set; }
    }
}
