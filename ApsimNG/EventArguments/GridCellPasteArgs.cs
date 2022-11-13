namespace UserInterface.EventArguments
{
    /// <summary>
    /// Event arguments used when pasting data into a grid.
    /// </summary>
    public class GridCellPasteArgs : GridCellActionArgs
    {
        /// <summary>
        /// Text to be pasted.
        /// </summary>
        public string Text { get; set; }
    }
}
