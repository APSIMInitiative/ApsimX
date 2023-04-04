namespace UserInterface.Views
{
    /// <summary>Render details for a cell.</summary>
    public class CellRendererDescription
    {
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public bool StrikeThrough { get; set; }
        public bool Bold { get; set; }
        public bool Italics { get; set; }
        public System.Drawing.Color Colour { get; set; }
    }
}