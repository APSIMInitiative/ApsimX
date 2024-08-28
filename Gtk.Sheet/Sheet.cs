using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using APSIM.Interop.Drawing;

namespace Gtk.Sheet
{
    /// <summary>
    /// This is the engine for a sheet (grid) widget. It can display a table (rows and columns)
    /// of data. The grid widget is intentionally very simple and does not contain
    /// any cell selection or editing capability. Instead, this widget relies on 
    /// other classes that work with this widget e.g. SingleCellSelect.
    /// </summary>
    /// <remarks>
    /// The caller can provide column widths or the widget will auto-calculate them.
    /// NB: All GTK use has been abstracted behind interfaces. This class does NOT
    /// reference GTK.
    /// </remarks>
    internal class Sheet
    {
        /// <summary>The width of the grid lines in pixels.</summary>
        private const double lineWidth = 0.2;

        /// <summary>The number of rows to scroll (up or down) on mouse wheel.</summary>
        private const int mouseWheelScrollRows = 10;

        /// <summary>A backing field for NumberHiddenColumns property.</summary>
        private int numberHiddenColumnsBackingField;

        /// <summary>A backing field for NumberHiddenRows property.</summary>
        private int numberHiddenRowsBackingField;

        /// <summary>Stores how many columns were drawn last draw. We have to re-initialise if this changes</summary>
        private int prevNumColumns = -1;

        /// <summary>Stores how many rows were drawn last draw. We have to re-initialise if this changes</summary>
        private int prevNumRows = -1;

        private bool recalculateWidths = true;

        private bool autoCalculateColumnWidths;

        /// <summary>Put a blank row at bottom of grid?</summary>
        private bool blankRowAtBottom;

        /// <summary>
        /// Constructor
        /// </summary>
        public Sheet(IDataProvider dataProvider, 
                     int numberFrozenColumns,
                     int numberFrozenRows,
                     int[] columnWidths,
                     bool blankRowAtBottom)
        {
            NumberFrozenColumns = numberFrozenColumns;
            NumberFrozenRows = numberFrozenRows;
            ColumnWidths = columnWidths;
            autoCalculateColumnWidths = ColumnWidths == null;
            this.blankRowAtBottom = blankRowAtBottom;
            SetDataProvider(dataProvider);
        }

        /// <summary>Invoked when a key is pressed.</summary>
        public event EventHandler<SheetEventKey> KeyPress;

        /// <summary>Invoked when a mouse button is clicked.</summary>
        public event EventHandler<SheetEventButton> MouseClick;

        /// <summary>Invoked when a mouse button is clicked.</summary>
        public event EventHandler<SheetEventButton> ContextMenuClick;

        /// <summary>Invoked when the sheet has been initialised.</summary>
        public event EventHandler Initialised;

        /// <summary>Invoked when the sheet has been scrolled horizontally.</summary>
        public event EventHandler ScrolledHorizontally;

        /// <summary>Invoked when the sheet has been scrolled vertically.</summary>
        public event EventHandler ScrolledVertically;

        /// <summary>Invoked when the sheet needs a redraw.</summary>
        public event EventHandler RedrawNeeded;

        /// <summary>Invoked when the sheet needs a redraw.</summary>
        public Action<Exception> OnException;

        /// <summary>The provider of data for the sheet.</summary>
        public IDataProvider DataProvider { get; private set; }

        /// <summary>The painter to use to get style a cell.</summary>
        public ISheetCellPainter CellPainter { get; set; }

        /// <summary>The cell selector instance.</summary>
        public ISheetSelection CellSelector { get; set; }

        /// <summary>The cell editor instance.</summary>
        public ISheetEditor CellEditor { get; set; }

        /// <summary>The scroll bars instance.</summary>
        public SheetScrollBars ScrollBars { get; set; }

        /// <summary>The widths (in pixels) of each column in the sheet. Can be null to auto-calculate.</summary>
        public int[] ColumnWidths { get; set; }

        /// <summary>The height in pixels of each row..</summary>
        public int RowHeight { get; set; } = 35;

        /// <summary>The number of columns that are frozen (can not be scrolled).</summary>
        public int NumberFrozenColumns { get; set; }

        /// <summary>The number of rows that are frozen (can not be scrolled).</summary>
        public int NumberFrozenRows { get; set; }

        /// <summary>Number of hidden columns - columns that have been scrolled off screen.</summary>        
        public int NumberHiddenColumns
        {
            get
            {
                return numberHiddenColumnsBackingField;
            }
            set
            {
                if (NumberHiddenColumns != value)
                    numberHiddenColumnsBackingField = value;
                ScrolledHorizontally?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>Number of hidden rows - rows that have been scrolled off screen.</summary>        
        public int NumberHiddenRows
        {
            get
            {
                return numberHiddenRowsBackingField;
            }
            set
            {
                if (NumberHiddenRows != value)
                    numberHiddenRowsBackingField = value;
                ScrolledVertically?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>Maximum number of columns that can be hidden (scrolled).</summary>        
        public int MaximumNumberHiddenColumns { get { return CalculateNumberOfHiddenColumnsToMakeColumnVisible(DataProvider.ColumnCount - 1); } }

                /// <summary>Maximum number of rows that can be hidden (scrolled).</summary>        
        public int MaximumNumberHiddenRows { get; private set; }

        /// <summary>Width of the sheet in pixels.</summary>        
        public int Width { get; set; }

        /// <summary>Height of the sheet in pixels.</summary>        
        public int Height { get; set; }

        /// <summary>Show grid lines?</summary>
        public bool ShowLines { get; set; } = true;

        /// <summary>The padding (in pixels) to go on the left and right size of a column.</summary>
        public int ColumnPadding { get; set; } = 20;

        /// <summary>The number of rows to paint in the grid. If zero, then the data provider will determine the number of rows in the grid.</summary>
        public int RowCount { get; private set; } = 0;

        /// <summary>A collection of column indexes that are currently visible or partially visible.</summary>        
        public IEnumerable<int> VisibleColumnIndexes {  get { return DetermineVisibleColumnIndexes(fullyVisible: false);  } }

        /// <summary>A collection of row indexes that are currently visible or partially visible.</summary>        
        public IEnumerable<int> VisibleRowIndexes { get { return DetermineVisibleRowIndexes(fullyVisible: false); } }

        /// <summary>A collection of column indexes that are currently fully visible.</summary>        
        public IEnumerable<int> FullyVisibleColumnIndexes { get { return DetermineVisibleColumnIndexes(fullyVisible: true); } }

        /// <summary>A collection of row indexes that are currently fully visible.</summary>        
        public IEnumerable<int> FullyVisibleRowIndexes { get { return DetermineVisibleRowIndexes(fullyVisible: true); } }

        public void SetDataProvider(IDataProvider provider)
        {
            DataProvider = provider;

            if (DataProvider != null)
            {
                bool hasUnits = false;
                for (int colIndex = 0; colIndex < DataProvider.ColumnCount; colIndex++)
                    hasUnits = hasUnits || DataProvider.GetColumnUnits(colIndex) != null;
            
                // Set the number of frozen rows
                if (NumberFrozenRows == -1)
                {
                    if (hasUnits)
                        NumberFrozenRows = 2;
                    else
                        NumberFrozenRows = 1;
                }                

                RowCount = DataProvider.RowCount + NumberFrozenRows;
                if (blankRowAtBottom)
                    RowCount++;
            }
        }

        /// <summary>Resize the sheet.</summary>        
        /// <param name="allocationWidth">The width of the sheet.</param>
        /// <param name="allocationHeight">The height of the sheet.</param>
        public void Resize(int allocationWidth, int allocationHeight)
        {
            if (allocationWidth != Width || allocationHeight != Height)
            {
                if (autoCalculateColumnWidths)
                    ColumnWidths = null;
                MaximumNumberHiddenRows = 0;
                RedrawNeeded?.Invoke(this, new EventArgs());
            }
        }


        public void InvokeKeyPress(SheetEventKey key)
        {
                KeyPress.Invoke(this, key);
        }

        public void InvokeButtonPress(SheetEventButton button)
        {
            if (button.LeftButton)
                MouseClick?.Invoke(this, button);
            else
                ContextMenuClick?.Invoke(this, button);
        }

        public void InvokeScroll(int delta)
        {
            if (delta < 0)
                ScrollDown();
            else
                ScrollUp();
            Refresh();
        }

        public void Refresh()
        {
            RowCount = DataProvider.RowCount + NumberFrozenRows;
                if (blankRowAtBottom)
                    RowCount++;
            RedrawNeeded?.Invoke(this, new EventArgs());
        }
        
        /// <summary>
        /// The number of rows that might potentially be currently visible, whether they hold data or not, including those partially visible
        /// </summary>
        private int VisibleRowCount
        {
            get { return (Height + RowHeight - 1) / RowHeight; }
        }

        /// <summary>Calculates the bounds of a cell if it is visible (wholly or partially) to the user.</summary>
        /// <param name="columnIndex">The cell column index.</param>
        /// <param name="rowIndex">The cell row index.</param>
        /// <returns>The bounds if visible or null if not visible.</returns>
        public Rectangle CalculateBounds(int columnIndex, int rowIndex)
        {
            // Convert rowIndex that is relative to all rows to 
            // an index that is relative to the visible rows.
            var visibleRowIndex = rowIndex;
            if (rowIndex >= NumberFrozenRows)
            {
                if (rowIndex < NumberHiddenRows + NumberFrozenRows)
                    return Rectangle.Empty;

                visibleRowIndex -= NumberHiddenRows;
            }

            // Convert columnIndex that is relative to all columns to 
            // an index that is relative to the visible columns.
            var visibleColumnIndexes = VisibleColumnIndexes.ToList();
            var visibleColumnIndex = visibleColumnIndexes.IndexOf(columnIndex);

            var rowTop = visibleRowIndex * RowHeight;
            if (visibleColumnIndex != -1 && rowTop < Height)
            {
                int left = visibleColumnIndexes.Take(visibleColumnIndex).Sum(c => ColumnWidths[c]);
                int top = rowTop;
                int width = ColumnWidths[columnIndex];
                int height = RowHeight;

                return new Rectangle(left, top, width, height);
            }
            else
                return Rectangle.Empty; // cell isn't visible.
        }

        /// <summary>Scroll the sheet to the right one column.</summary>
        public void ScrollRight()
        {
            // Get the right most fully visible column.
            var rightColumnIndex = FullyVisibleColumnIndexes.Last();

            // Calculate the new right most column index.
            var newRightColumnIndex = Math.Min(rightColumnIndex + 1, DataProvider.ColumnCount-1);

            NumberHiddenColumns = CalculateNumberOfHiddenColumnsToMakeColumnVisible(newRightColumnIndex);
        }

        /// <summary>Scroll the sheet to the left one column.</summary>
        public void ScrollLeft()
        {
            if (NumberHiddenColumns > 0)
                NumberHiddenColumns--;
        }

        /// <summary>Scroll the sheet up one row.</summary>
        /// <param name="numRows">Number of rows to scroll.</param>
        public void ScrollUp(int numRows = 1)
        {
            NumberHiddenRows = Math.Max(NumberHiddenRows - numRows, 0);
            RecalculateColumnWidths();
        }

        /// <summary>Scroll the sheet down one row.</summary>
        /// <param name="numRows">Number of rows to scroll.</param>
        public void ScrollDown(int numRows = 1)
        {
            var bottomFullyVisibleRowIndex = FullyVisibleRowIndexes.Last();
            if (bottomFullyVisibleRowIndex < DataProvider.RowCount - 1)
                NumberHiddenRows++;
            RecalculateColumnWidths();
        }

        /// <summary>Scroll the sheet down one page of rows.</summary>
        public void ScrollDownPage()
        {
            int pageSize = FullyVisibleRowIndexes.Count() - NumberFrozenRows; 
            if (NumberHiddenRows + pageSize < DataProvider.RowCount - 1)
                NumberHiddenRows += pageSize;
            RecalculateColumnWidths();
        }

        /// <summary>Scroll the sheet up one page of rows.</summary>
        public void ScrollUpPage()
        {
            int pageSize = FullyVisibleRowIndexes.Count() - NumberFrozenRows; 
            NumberHiddenRows = Math.Max(NumberHiddenRows - pageSize, 0);
            RecalculateColumnWidths();
        }

        /// <summary>After this is called, the next time the view is drawn, columns widths will be recalculated</summary>
        public void RecalculateColumnWidths()
        {
            recalculateWidths = true;
        }

        /// <summary>Return true if a xy pixel coordinates are in a specified cell.</summary>
        /// <param name="pixelX">X pixel coordinate.</param>
        /// <param name="pixelY">Y pixel coordinate.</param>
        /// <param name="columnIndex">The column index of a cell.</param>
        /// <param name="rowIndex">The row index of a cell.</param>
        public bool CellHitTest(int pixelX, int pixelY, out int columnIndex, out int rowIndex)
        {
            columnIndex = 0;
            int index = pixelY / RowHeight;
            if (index >= VisibleRowIndexes.Count())
            {
                rowIndex = 0;
                return false;
            }
            rowIndex = VisibleRowIndexes.ElementAt(index);
            int leftPixelX = 0;
            foreach (int colIndex in VisibleColumnIndexes)
            {
                int rightPixelX = leftPixelX + ColumnWidths[colIndex];
                if (pixelX < rightPixelX)
                {
                    columnIndex = colIndex;
                    return true;
                }
                leftPixelX += ColumnWidths[colIndex];
            }
            return false;
        }

        /// <summary>Called by base class to draw the sheet widget.</summary>
        /// <param name="cr">The context to draw in.</param>
        public bool Draw(IDrawContext cr)
        {
            try
            {
                // Do initialisation
                if (ColumnWidths == null || prevNumColumns != DataProvider.ColumnCount || prevNumRows != DataProvider.RowCount)
                    Initialise(cr);

                if (recalculateWidths)
                {
                    if (cr != null)
                    {
                        CalculateColumnWidths(cr);
                        recalculateWidths = false;
                    }
                }

                if (DataProvider != null)
                {
                    prevNumColumns = DataProvider.ColumnCount;
                    prevNumRows = DataProvider.RowCount;

                    foreach (var columnIndex in VisibleColumnIndexes)
                        foreach (var rowIndex in VisibleRowIndexes)
                            DrawCell(cr, columnIndex, rowIndex);

                    // Optionally add in blank rows at bottom of grid.
                    int numRowsOfData = VisibleRowIndexes.Count();
                    int numBlankRowsToAdd = 0;
                    foreach (var columnIndex in VisibleColumnIndexes)
                        for (int rowIndex = 0; rowIndex < numBlankRowsToAdd; rowIndex++)
                            DrawCell(cr, columnIndex, rowIndex + numRowsOfData);
                }
            }
            catch (Exception err)
            {
                OnException(err);
            }

            return true;
        }

        /// <summary>Initialise the widget.</summary>
        /// <param name="cr">The drawing context.</param>
        public void Initialise(IDrawContext cr)
        {
            if (cr != null)
                CalculateColumnWidths(cr);

            // The first time through here calculate maximum number of hidden rows.
            if (MaximumNumberHiddenRows == 0 && DataProvider != null)
            {
                MaximumNumberHiddenRows = DataProvider.RowCount - Height/RowHeight; 
                Initialised?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>Invoked when the user presses a key.</summary>
        /// <param name="evnt">The event data.</param>
        /// <returns>True if event has handled.</returns>
        public bool OnKeyPressEvent(SheetEventKey evnt)
        {
            try
            {
                KeyPress?.Invoke(this, evnt);
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
            return true;
        }

        /// <summary>Invoked when the user clicks a mouse button.</summary>
        /// <param name="evnt">The event data.</param>
        /// <returns></returns>
        public bool OnButtonPressEvent(SheetEventButton evnt)
        {
            try
            {
                MouseClick?.Invoke(this, evnt);
            }
            catch (Exception ex)
            {
                OnException(ex);
            }

            return true;
        }

        public bool OnScrollEvent(IEventScroll e)
        {
            try
            {
                int delta;
                if (e.DirectionIsSmooth)
                    delta = e.DeltaY < 0 ? mouseWheelScrollRows : -mouseWheelScrollRows;
                else
                    delta = e.IsDirectionDown ? -mouseWheelScrollRows : mouseWheelScrollRows;

                if (delta < 0)
                    ScrollDown();
                else
                    ScrollUp();
            }
            catch (Exception err)
            {
                OnException(err);
            }
            return true;
        }

        /// <summary>Returns fully visible or partially visible enumeration of column indexes.</summary>
        /// <param name="fullyVisible">Return enumeration of fully visible column indexes?</param>
        private IEnumerable<int> DetermineVisibleColumnIndexes(bool fullyVisible)
        {
            int left = 0;   // horizontal pixel position

            int columnIndex = 0;
            while (columnIndex < DataProvider.ColumnCount && left < Width)
            {
                var firstScrollableColumnIndex = NumberHiddenColumns + NumberFrozenColumns;
                if (columnIndex < NumberFrozenColumns || columnIndex >= firstScrollableColumnIndex)
                {
                    if (!fullyVisible || left + ColumnWidths[columnIndex] < Width)
                        yield return columnIndex;
                    left += ColumnWidths[columnIndex];
                }

                columnIndex++;
            }
        }

        /// <summary>Returns fully visible or partially visible enumeration of row indexes.</summary>
        /// <param name="fullyVisible">Return enumeration of fully visible row indexes?</param>
        private IEnumerable<int> DetermineVisibleRowIndexes(bool fullyVisible)
        {
            int top = 0;
            int rowIndex = 0;
            while (rowIndex < RowCount && top < Height)
            {
                var firstScrollableRowIndex = NumberHiddenRows + NumberFrozenRows;

                if (rowIndex < NumberFrozenRows || rowIndex >= firstScrollableRowIndex)
                {
                    if (!fullyVisible || top + RowHeight < Height)
                        yield return rowIndex;
                    top += RowHeight;
                }

                rowIndex++;
            }
        }

        /// <summary>Calculate the number of hidden columns to make a column visible.</summary>
        /// <param name="columnIndex"></param>
        private int CalculateNumberOfHiddenColumnsToMakeColumnVisible(int columnIndex)
        {
            int savedNumberHiddenColumns = NumberHiddenColumns;

            // Keep incrementing the number of hidden columns until the column
            // is fully visible.
            NumberHiddenColumns = 0;
            while (NumberHiddenColumns < DataProvider.ColumnCount-1 && !FullyVisibleColumnIndexes.Contains(columnIndex))
                NumberHiddenColumns++;

            if (NumberHiddenColumns == DataProvider.ColumnCount - 1)
            {
                NumberHiddenColumns = savedNumberHiddenColumns + 1; // No hidden columns as there isn't room to display any.
                NumberFrozenColumns = 0;
            }
            
            int returnColumnNumber = NumberHiddenColumns;
            NumberHiddenColumns = savedNumberHiddenColumns;
            return returnColumnNumber;
        }

        /// <summary>Calculte the column widths in pixels.</summary>
        /// <param name="cr">The current draing context.</param>
        private void CalculateColumnWidths(IDrawContext cr)
        {
            if (autoCalculateColumnWidths && DataProvider != null)
            {
                int visibleRows = FullyVisibleRowIndexes.Count() + NumberHiddenRows;
                ColumnWidths = new int[DataProvider.ColumnCount];
                for (int columnIndex = 0; columnIndex < DataProvider.ColumnCount; columnIndex++)
                {
                    int columnWidth = GetWidthOfCell(cr, columnIndex, 0);
                    for (int rowIndex = NumberHiddenRows; rowIndex < visibleRows; rowIndex++)
                        columnWidth = Math.Max(columnWidth, GetWidthOfCell(cr, columnIndex, rowIndex));

                    ColumnWidths[columnIndex] = columnWidth + ColumnPadding * 2;
                }
                RedrawNeeded?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Get the width of a cell.
        /// </summary>
        /// <param name="cr"></param>
        /// <param name="columnIndex"></param>
        /// <param name="rowIndex"></param>
        /// <returns></returns>
        private int GetWidthOfCell(IDrawContext cr, int columnIndex, int rowIndex)
        {
            string text = GetCellText(columnIndex, rowIndex);
            if (text == null)
                text = string.Empty;

            var rectangle = cr.GetPixelExtents(text, true, false);
            return rectangle.Width;
        }

        /// <summary>Draw a single cell to the context.</summary>
        /// <param name="cr">The context to draw in.</param>
        /// <param name="columnIndex">The column index.</param>
        /// <param name="rowIndex">The row index.</param>
        private void DrawCell(IDrawContext cr, int columnIndex, int rowIndex)
        {
            try
            {
                string text = null;
                text = GetCellText(columnIndex, rowIndex);

                if (DataProvider.GetColumnUnits(columnIndex) != null)
                {
                    string units = DataProvider.GetColumnUnits(columnIndex);
                    if (units.CompareTo("boolean") == 0 && rowIndex > 0)
                    {
                        text = "\u2713";
                    }
                }


                var cellBounds = CalculateBounds(columnIndex, rowIndex);
                if (cellBounds != Rectangle.Empty)
                {
                    if (text == null)
                        text = string.Empty;

                    cr.Rectangle(cellBounds.Clip(Width - 20, Height));
                    cr.Clip();

                    cr.SetLineWidth(lineWidth);

                    cr.Rectangle(cellBounds);
                    if (CellPainter.PaintCell(columnIndex, rowIndex))
                    {
                        // Draw the filled in cell.

                        cr.State = CellPainter.GetCellState(columnIndex, rowIndex);
                        cr.DrawFilledRectangle(cellBounds.Left, cellBounds.Top, cellBounds.Width - 5, cellBounds.Height - 5);


                        // Draw cell outline.
                        if (ShowLines)
                        {
                            cr.Rectangle(cellBounds);
                            cr.Stroke();
                        }

                        // Measure text for cell.
                        var r = cr.GetPixelExtents(text,
                                                    CellPainter.TextBold(columnIndex, rowIndex),
                                                    CellPainter.TextItalics(columnIndex, rowIndex));

                        // Vertically center the text.
                        double y = cellBounds.Top + (cellBounds.Height - r.Height) / 2;

                        // Horizontal alignment is determined by the cell painter.
                        double x;
                        if (CellPainter.TextLeftJustify(columnIndex, rowIndex))
                            x = cellBounds.Left + ColumnPadding;
                        else
                            x = cellBounds.Right - ColumnPadding - r.Width;

                        cr.MoveTo(x, y);
                        cr.DrawText(text, CellPainter.TextBold(columnIndex, rowIndex),
                                            CellPainter.TextItalics(columnIndex, rowIndex));

                    }
                    cr.ResetClip();
                    cr.State = States.Normal;
                }
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
        }

        /// <summary>
        /// Get text for a grid cell.
        /// </summary>
        /// <param name="columnIndex">The column index.</param>
        /// <param name="rowIndex">The row index.</param>
        private string GetCellText(int columnIndex, int rowIndex)
        {
            string text = null;
            if (rowIndex == 0)
                text = DataProvider.GetColumnName(columnIndex);
            else if (rowIndex == 1 && NumberFrozenRows == 2)
                text = DataProvider.GetColumnUnits(columnIndex);
            else if (rowIndex < DataProvider.RowCount + NumberFrozenRows)
                text = DataProvider.GetCellContents(columnIndex, rowIndex - NumberFrozenRows);
            return text;
        }
    }
}