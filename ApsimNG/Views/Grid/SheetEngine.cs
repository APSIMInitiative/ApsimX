using Cairo;
using System;
using System.Linq;
using System.Collections.Generic;

namespace UserInterface.Views
{
    class SheetEngine
    {
        int rowHeight = 35;
        const double lineWidth = 0.2;
        bool showLines = true;
        bool leftJustify = false;

        /// <summary>The padding (in pixels) to go on the left and right size of a column.</summary>
        const int columnPadding = 10;

        int width;
        int height;
        double numberVisibleRows;
        int numberFrozenColumns;
        int numberHeadingRows;
        ISheetDataProvider dataProvider;

        /// <summary>Columns widths for all data columns. Index into list is a data column index.</summary>
        List<ColumnMetadata> columns = new List<ColumnMetadata>();

        /// <summary>Constructor</summary>
        /// <param name="initialWidth">Initial width of grid.</param>
        /// <param name="initialHeight">Initial height of grid.</param>
        /// <param name="numberHeadingRows">Number of rows of headings.</param>
        /// <param name="numberFrozenColumns">Number of frozen columns.</param>
        /// <param name="leftJustifyCellContents">Left justify cell contents?</param>
        /// <param name="dataProvider">The provider of content for the grid.</param>
        public SheetEngine(int initialWidth, int initialHeight, int numberHeadingRows, int numberFrozenColumns,
                          bool leftJustifyCellContents,
                          ISheetDataProvider dataProvider)
        {
            width = initialWidth;
            height = initialHeight;
            leftJustify = leftJustifyCellContents;
            this.numberHeadingRows = numberHeadingRows;
            this.numberFrozenColumns = numberFrozenColumns;
            this.dataProvider = dataProvider;
        }

        public Cell Selected { get; set; }

        /// <summary>The maximum number of columns that can be scrolled.</summary>
        public int MaxColumnsToScroll => dataProvider.ColumnCount - VisibleColumns.Count();

        /// <summary>The maximum number of rows that can be scrolled.</summary>
        public int MaxRowsToScroll => dataProvider.RowCount - height / rowHeight;

        /// <summary>The number of columns that are currently scrolled.</summary>
        public int NumberColumnsScrolled { get; private set; }

        /// <summary>The number of rows that are currently scrolled.</summary>
        public int NumberRowsScrolled { get; private set; }

        /// <summary>Is the current selection being edited?</summary>
        public bool SelectionBeingEdited { get; set; }

        /// <summary>Grid was horizontally scrolled.</summary>
        public event EventHandler HorizontallyScrolled;

        /// <summary>Grid was vertically scrolled.</summary>
        public event EventHandler VerticallyScrolled;

        /// <summary>Called to draw the grid on a Cairo context.</summary>
        /// <param name="cr">The context to draw on.</param>
        public void Draw(Context cr)
        {
            cr.SelectFontFace("Segoe UI", FontSlant.Normal, FontWeight.Normal);
            cr.SetFontSize(18);

            // Initialise columns if necessary.
            if (columns.Count == 0)
                InitialiseColumns(cr);

            // Determine the number of columns to draw.
            numberVisibleRows = Math.Min(height * 1.0 / rowHeight, dataProvider.RowCount);

            SetColumnVisibility();

            foreach (var cell in Cells)
                cell.Draw(cr);
        }

        /// <summary>Gets a collection of visible columns.</summary>
        private IEnumerable<ColumnMetadata> VisibleColumns => columns.Where(c => c.Visible == ColumnMetadata.Visibility.FullyVisible ||
                                                                                 c.Visible == ColumnMetadata.Visibility.PartiallyVisible);

        private IEnumerable<Cell> Cells
        {
            get
            {
                foreach (var column in VisibleColumns)
                    for (int rowIndex = 0; rowIndex < numberVisibleRows; rowIndex++)
                        yield return new Cell(column, rowIndex, this);
            }
        }

        /// <summary>Select a specific cell.</summary>
        /// <param name="pixel">The pixel coordinates.</param>
        /// <returns>true if the cell was selected.</returns>
        public bool Select(Point pixel)
        {
            var screen = PixelToCell(pixel);
            if (screen != null)
            {
                Selected = screen;
                return true;
            }
            return false;
        }

        private void EnsureSelectedIsVisible()
        {

            if (Selected.Column.Visible == ColumnMetadata.Visibility.NotVisible ||
                Selected.Column.Visible == ColumnMetadata.Visibility.PartiallyVisible)
            {
                bool gridScrolledHorizontally = false;

                // Scroll to the right if the cell is off the screen to the right.
                while (Selected.Column.Left + Selected.Column.Width > width)
                {
                    NumberColumnsScrolled++;
                    gridScrolledHorizontally = true;
                    SetColumnVisibility();
                }

                while (Selected.Column.Visible != ColumnMetadata.Visibility.FullyVisible && NumberColumnsScrolled > 0)
                {
                    // Off the screen to the left.
                    NumberColumnsScrolled--;
                    gridScrolledHorizontally = true;
                    SetColumnVisibility();
                }

                if (gridScrolledHorizontally)
                    HorizontallyScrolled?.Invoke(this, new EventArgs());
            }

            int lastFullyVisibleRow = (int)numberVisibleRows;
            if (numberVisibleRows % 1 > 0)
                lastFullyVisibleRow--;

            bool gridScrolledVertically = false;
            var bounds = Selected.Bounds();
            while (bounds.Height != rowHeight && Selected.RowIndex < dataProvider.RowCount - 1)
            {
                NumberRowsScrolled++;
                gridScrolledVertically = true;
                bounds = Selected.Bounds();
            }
            if (gridScrolledVertically)
                VerticallyScrolled?.Invoke(this, new EventArgs());
        }

        /// <summary>Scroll the grid horizontally to a specific position.</summary>
        /// <param name="numColumnsScrolled">The number of columns to scroll the grid.</param>
        public void ScrollHorizontalTo(double numColumnsScrolled)
        {
            if (numColumnsScrolled <= MaxColumnsToScroll)
                NumberColumnsScrolled = (int)numColumnsScrolled;
        }

        /// <summary>Called when a vertical scroll bar has its value (position) changed.</summary>
        /// <param name="scrollPosition">The position of the scrollbar.</param>
        public void ScrollVerticalTo(double scrollPosition)
        {
            if (scrollPosition <= MaxRowsToScroll)
                NumberRowsScrolled = (int)scrollPosition;
        }

        private int FirstScrollingColumnIndex => numberFrozenColumns + NumberColumnsScrolled;

        /// <summary>Calculate column widths.</summary>
        private void InitialiseColumns(Context cr)
        {
            // Use the heading rows and the first row of data to determine the column widths.
            columns.Clear();
            int left = 0;
            for (int col = 0; col < dataProvider.ColumnCount; col++)
            {
                int columnWidth = 0;
                for (int row = 0; row < numberHeadingRows+1; row++)
                {
                    var text = dataProvider.GetCellContents(col, row);
                    if (text == null)
                        text = string.Empty;
                    var extents = cr.TextExtents(text);
                    columnWidth = Math.Max(columnWidth, (int)extents.Width);
                }

                // Add column padding to left and right side of column (hence 2 x padding)
                columnWidth += columnPadding * 2;  

                columns.Add(new ColumnMetadata(left, columnWidth, col));
                left += columnWidth;
            }
            SetColumnVisibility();
            Selected = new Cell(columns[0], numberHeadingRows, this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private void SetColumnVisibility()
        {
            int left = 0;
            for (int col = 0; col < dataProvider.ColumnCount; col++)
            {
                var right = left + columns[col].Width;

                columns[col].Left = left;

                if (col >= numberFrozenColumns && col < FirstScrollingColumnIndex)
                    columns[col].Visible = ColumnMetadata.Visibility.NotVisible; // hidden because of scrolling
                else
                {
                    if (right <= width)
                        columns[col].Visible = ColumnMetadata.Visibility.FullyVisible;
                    else if (left <= width && right > width)
                        columns[col].Visible = ColumnMetadata.Visibility.PartiallyVisible;
                    else if (left > width)
                        columns[col].Visible = ColumnMetadata.Visibility.NotVisible; // hidden because of right of screen.

                    // Advance to next column
                    left = right;
                }
            }
        }

        /// <summary>Helper function to convert x / y coordinates to a cell.</summary>
        /// <param name="pixel">Pixel coordinates</param>
        /// <returns>Screen coordinates or null if not valid.</returns>
        private Cell PixelToCell(Point pixel)
        {
            var column = VisibleColumns.FirstOrDefault(c => c.Contains(pixel.X));
            if (column == null)
                return null;

            var screenY = pixel.Y / rowHeight;
            if (screenY >= numberVisibleRows)
                return null;
            else
                return new Cell(column, screenY, this);
        }

        public class ColumnMetadata
        {
            public enum Visibility
            {
                NotVisible,
                FullyVisible,
                PartiallyVisible
            }
            public ColumnMetadata(int left, int width, int dataIndex)
            {
                Left = left;
                Width = width;
                DataIndex = dataIndex;
            }
            public int Left { get; set; }
            public int Width { get; set; }
            public Visibility Visible { get; set; }
            public int DataIndex { get; }

            public bool Contains(int x)
            {
                return x >= Left && x <= Left + Width;
            }
        }

        public class Cell
        {
            private readonly SheetEngine sheet;

            public Cell(ColumnMetadata column, int rowIndex, SheetEngine sheet)
            {
                this.Column = column;
                this.sheet = sheet;
                this.RowIndex = rowIndex;
            }

            public ColumnMetadata Column { get; private set; }

            public int RowIndex { get; private set; }

            public Rectangle Bounds()
            {
                int height = sheet.rowHeight;
                int y = RowIndex * sheet.rowHeight - sheet.NumberRowsScrolled * sheet.rowHeight;
                if (y + sheet.rowHeight > sheet.height)
                    height = sheet.height - y;

                var columnWidth = Column.Width;
                var rightSide = Column.Left + Column.Width;
                if (rightSide > sheet.width)
                    columnWidth = sheet.width - Column.Left;

                return new Rectangle(Column.Left, y, columnWidth, height);
            }

            public void MoveLeft()
            {
                if (Column.DataIndex > 0)
                {
                    Column = sheet.columns[Column.DataIndex - 1];
                    sheet.EnsureSelectedIsVisible();
                }
            }

            public void MoveRight()
            {
                if (Column.DataIndex < sheet.columns.Count - 1)
                {
                    Column = sheet.columns[Column.DataIndex + 1];
                    sheet.EnsureSelectedIsVisible();
                }
            }

            public void MoveUp()
            {
                if (RowIndex > sheet.numberHeadingRows)
                {
                    RowIndex--;
                    sheet.EnsureSelectedIsVisible();
                }
            }

            public void MoveDown()
            {
                if (RowIndex < sheet.dataProvider.RowCount - 1)
                {
                    RowIndex++;
                    sheet.EnsureSelectedIsVisible();
                }
            }


            public string Text
            {
                get
                {
                    return sheet.dataProvider.GetCellContents(Column.DataIndex, RowIndex);
                }
                set
                {
                    sheet.dataProvider.SetCellContents(Column.DataIndex, RowIndex - sheet.NumberRowsScrolled, value);
                }
            }
                

            /// <summary>Draw a single cell to the context.</summary>
            /// <param name="cr">The context to draw in.</param>
            /// <param name="cell">The cell to draw.</param>
            public void Draw(Context cr)
            {
                var text = Text;
                if (text == null)
                    text = string.Empty;

                var cellBounds = Bounds();
                cr.Rectangle(cellBounds);
                cr.Clip();

                cr.LineWidth = lineWidth;

                cr.SetSourceColor(BackgroundColour);

                cr.Rectangle(cellBounds);
                if (sheet.SelectionBeingEdited && IsSelected)
                {
                    cr.SetSourceRGB(0, 0, 0); // black
                    cr.Stroke();
                }
                else
                {
                    cr.Fill();
                    if (sheet.showLines)
                    {
                        cr.Rectangle(cellBounds);
                        cr.SetSourceRGB(0.0, 0.0, 0);
                        cr.Stroke();
                    }

                    TextExtents extents = cr.TextExtents(text);

                    var maxHeight = cr.TextExtents("j").Height - cr.TextExtents("D").Height;
                    maxHeight = 10;


                    //var textExtents = cr.TextExtents("j")
                    cr.SetSourceColor(ForegroundColour);
                    if (sheet.leftJustify)
                    {
                        cr.MoveTo(cellBounds.X + columnPadding, cellBounds.Y + cellBounds.Height - maxHeight);
                        cr.TextPath(text);
                    }
                    else
                    {
                        // right justify
                        var textExtents = cr.TextExtents(text);
                        cr.MoveTo(cellBounds.X + cellBounds.Width - columnPadding - textExtents.Width, cellBounds.Y + cellBounds.Height - maxHeight);
                        cr.TextPath(text);
                    }

                    cr.Fill();
                }
                cr.ResetClip();
            }

            private Color BackgroundColour
            {
                get
                {
                    if (IsSelected)
                        return new Color(198 / 255.0, 198 / 255.0, 198 / 255.0);  // light grey
                    else if (IsHeading)
                        return new Color(102 / 255.0, 102 / 255.0, 102 / 255.0);  // dark grey
                    else
                        return new Color(1, 1, 1); // white
                }
            }
            private Color ForegroundColour
            {
                get
                {
                    if (IsHeading)
                        return new Cairo.Color(1, 1, 1); // white
                    else
                        return new Cairo.Color(0, 0, 0); // black
                }
            }

            private bool IsSelected => Column == sheet.Selected.Column && RowIndex == sheet.Selected.RowIndex;

            private bool IsHeading => RowIndex < sheet.numberHeadingRows;
        }
    }
}