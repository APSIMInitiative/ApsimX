using APSIM.Shared.Utilities;
using Cairo;
using Gdk;
using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UserInterface.Views
{
    /// <summary>
    /// This is a sheet (grid) widget for GTK. It can display a table (rows and columns)
    /// of data. The grid widget is intentionally very simple and does not contain
    /// any cell selection or editing capability. Instead, this widget relies on 
    /// other classes that work with this widget e.g. SingleCellSelect.
    /// </summary>
    /// <remarks>
    /// The caller can provide column widths or the widget will auto-calculate them.
    /// 
    /// </remarks>
    public class SheetWidget : EventBox
    {
        /// <summary>The width of the grid lines in pixels.</summary>
        private const double lineWidth = 0.2;

        /// <summary>The number of rows to scroll (up or down) on mouse wheel.</summary>
        private const int mouseWheelScrollRows = 10;

        /// <summary>A backing field for NumberHiddenColumns property.</summary>
        private int numberHiddenColumnsBackingField;

        /// <summary>A backing field for NumberHiddenRows property.</summary>
        private int numberHiddenRowsBackingField;

        /// <summary>Constructor</summary>
        public SheetWidget()
        {
            CanFocus = true;
#if NETCOREAPP
            this.StyleContext.AddClass("sheet");
#endif
            AddEvents((int)EventMask.ScrollMask);
        }

        /// <summary>Invoked when a key is pressed.</summary>
        public event EventHandler<EventKey> KeyPress;

        /// <summary>Invoked when a mouse button is clicked.</summary>
        public event EventHandler<EventButton> MouseClick;

        /// <summary>Invoked when the sheet has been initialised.</summary>
        public event EventHandler Initialised;

        /// <summary>Invoked when the sheet has been scrolled horizontally.</summary>
        public event EventHandler ScrolledHorizontally;

        /// <summary>Invoked when the sheet has been scrolled vertically.</summary>
        public event EventHandler ScrolledVertically;

        /// <summary>The provider of data for the sheet.</summary>
        public ISheetDataProvider DataProvider { get; set; }

        /// <summary>The painter to use to get style a cell.</summary>
        public ISheetCellPainter CellPainter { get; set; }

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

        /// <summary>A collection of column indexes that are currently visible or partially visible.</summary>        
        public IEnumerable<int> VisibleColumnIndexes {  get { return DetermineVisibleColumnIndexes(fullyVisible: false);  } }

        /// <summary>A collection of row indexes that are currently visible or partially visible.</summary>        
        public IEnumerable<int> VisibleRowIndexes { get { return DetermineVisibleRowIndexes(fullyVisible: false); } }

        /// <summary>A collection of column indexes that are currently fully visible.</summary>        
        public IEnumerable<int> FullyVisibleColumnIndexes { get { return DetermineVisibleColumnIndexes(fullyVisible: true); } }

        /// <summary>A collection of row indexes that are currently fully visible.</summary>        
        public IEnumerable<int> FullyVisibleRowIndexes { get { return DetermineVisibleRowIndexes(fullyVisible: true); } }

        /// <summary>Calculates the bounds of a cell if it is visible (wholly or partially) to the user.</summary>
        /// <param name="columnIndex">The cell column index.</param>
        /// <param name="rowIndex">The cell row index.</param>
        /// <returns>The bounds if visible or null if not visible.</returns>
        public CellBounds CalculateBounds(int columnIndex, int rowIndex)
        {
            // Convert rowIndex that is relative to all rows to 
            // an index that is relative to the visible rows.
            var visibleRowIndex = rowIndex;
            if (rowIndex >= NumberFrozenRows)
            {
                if (rowIndex < NumberHiddenRows + NumberFrozenRows)
                    return null;

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

                return new CellBounds(left, top, width, height);
            }
            else
                return null; // cell isn't visible.
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
        }

        /// <summary>Scroll the sheet down one row.</summary>
        /// <param name="numRows">Number of rows to scroll.</param>
        public void ScrollDown(int numRows = 1)
        {
            var bottomFullyVisibleRowIndex = FullyVisibleRowIndexes.Last();
            if (bottomFullyVisibleRowIndex < DataProvider.RowCount - 1)
                NumberHiddenRows++;
        }

        /// <summary>Scroll the sheet down one page of rows.</summary>
        public void ScrollDownPage()
        {
            int pageSize = FullyVisibleRowIndexes.Count() - NumberFrozenRows; 
            if (NumberHiddenRows + pageSize < DataProvider.RowCount - 1)
                NumberHiddenRows += pageSize;
        }

        /// <summary>Scroll the sheet up one page of rows.</summary>
        public void ScrollUpPage()
        {
            int pageSize = FullyVisibleRowIndexes.Count() - NumberFrozenRows; 
            NumberHiddenRows = Math.Max(NumberHiddenRows - pageSize, 0);
        }

        /// <summary>Refresh the sheet.</summary>
        public void Refresh()
        {
            QueueDraw();
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

#if NETFRAMEWORK
        /// <summary>Called by base class to draw the sheet widget.</summary>
        /// <param name="expose">The expose event arguments.</param>
        protected override bool OnExposeEvent(EventExpose expose)
        {
            Context cr = CairoHelper.Create(expose.Window);

            // Do initialisation
            if (ColumnWidths == null)
                Initialise(cr);

            base.OnExposeEvent(expose);

            foreach (var columnIndex in VisibleColumnIndexes)
                foreach (var rowIndex in VisibleRowIndexes)
                    DrawCell(cr, columnIndex, rowIndex);

            ((IDisposable)cr.Target).Dispose();
            ((IDisposable)cr).Dispose();

            return true;
        }
#else
        /// <summary>Called by base class to draw the sheet widget.</summary>
        /// <param name="cr">The context to draw in.</param>
        protected override bool OnDrawn(Context cr)
        {
            // Do initialisation
            if (ColumnWidths == null)
                Initialise(cr);

            base.OnDrawn(cr);

            foreach (var columnIndex in VisibleColumnIndexes)
                foreach (var rowIndex in VisibleRowIndexes)
                    DrawCell(cr, columnIndex, rowIndex);

            return true;
        }
#endif

        /// <summary>Initialise the widget.</summary>
        /// <param name="cr">The drawing context.</param>
        private void Initialise(Context cr)
        {
#if NETFRAMEWORK
            Width = Parent.Allocation.Width;
            Height = Parent.Allocation.Height;
#else
            Width = this.AllocatedWidth;
            Height = this.AllocatedHeight;
#endif
            if (cr != null)
                CalculateColumnWidths(cr);

            // The first time through here calculate maximum number of hidden rows.
            if (MaximumNumberHiddenRows == 0)
                MaximumNumberHiddenRows = DataProvider.RowCount - FullyVisibleRowIndexes.Last();

            Initialised?.Invoke(this, new EventArgs());
            GrabFocus();
        }

        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated(allocation);

            if (allocation.Width != Width || allocation.Height != Height)
            {
                ColumnWidths = null;
                MaximumNumberHiddenRows = 0;
                Refresh();
            }
        }

        /// <summary>Invoked when the user presses a key.</summary>
        /// <param name="evnt">The event data.</param>
        /// <returns>True if event has handled.</returns>
        protected override bool OnKeyPressEvent(EventKey evnt)
        {
            try
            {
                KeyPress?.Invoke(this, evnt);
            }
            catch (Exception ex)
            {
                MainView.MasterView.ShowError(ex);
            }
            return true;
        }

        /// <summary>Invoked when the user clicks a mouse button.</summary>
        /// <param name="evnt">The event data.</param>
        /// <returns></returns>
        protected override bool OnButtonPressEvent(EventButton evnt)
        {
            try
            {
                MouseClick?.Invoke(this, evnt);
            }
            catch (Exception ex)
            {
                MainView.MasterView.ShowError(ex);
            }

            return true;
        }

        protected override bool OnScrollEvent(EventScroll e)
        {
            int delta;
#if NETFRAMEWORK
            delta = e.Direction == Gdk.ScrollDirection.Down ? -120 : 120;
#else
            if (e.Direction == Gdk.ScrollDirection.Smooth)
                delta = e.DeltaY < 0 ? mouseWheelScrollRows : -mouseWheelScrollRows;
            else
                delta = e.Direction == Gdk.ScrollDirection.Down ? -mouseWheelScrollRows : mouseWheelScrollRows;
#endif
            if (delta < 0)
                ScrollDown();
            else
                ScrollUp();
            Refresh();
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
            while (rowIndex < DataProvider.RowCount && top < Height)
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
        private void CalculateColumnWidths(Context cr)
        {
            ColumnWidths = new int[DataProvider.ColumnCount];
            for (int columnIndex = 0; columnIndex < DataProvider.ColumnCount; columnIndex++)
            {
                int columnWidth = 0;
                for (int rowIndex = 0; rowIndex < NumberFrozenRows + 1; rowIndex++)
                {
                    var text = DataProvider.GetCellContents(columnIndex, rowIndex);
                    if (text == null)
                        text = string.Empty;

                    var layout = this.CreatePangoLayout(text);
                    layout.FontDescription = new Pango.FontDescription();
                    layout.FontDescription.Weight = Pango.Weight.Bold;
                    layout.GetPixelExtents(out Pango.Rectangle inkRectangle, out Pango.Rectangle logicalRectangle);

                    columnWidth = Math.Max(columnWidth, (int)inkRectangle.Width);
                }
                ColumnWidths[columnIndex] = columnWidth + ColumnPadding * 2;
            }
        }

        /// <summary>Draw a single cell to the context.</summary>
        /// <param name="cr">The context to draw in.</param>
        /// <param name="columnIndex">The column index.</param>
        /// <param name="rowIndex">The row index.</param>
        private void DrawCell(Context cr, int columnIndex, int rowIndex)
        {
            try
            {
                var text = DataProvider.GetCellContents(columnIndex, rowIndex);
                var cellBounds = CalculateBounds(columnIndex, rowIndex);
                if (cellBounds != null)
                {
                    if (text == null)
                        text = string.Empty;

                    cr.Rectangle(cellBounds.ToClippedRectangle(Width-20, Height));
                    cr.Clip();

                    cr.LineWidth = lineWidth;

                    cr.Rectangle(cellBounds.ToRectangle());
                    if (!CellPainter.PaintCell(columnIndex, rowIndex))
                    {
                        //cr.SetSourceColor(CellPainter.GetForegroundColour(columnIndex, rowIndex));
                        //cr.Stroke();
                    }
                    else
                    {
                        // Draw the filled in cell.
#if NETCOREAPP
                        this.StyleContext.State = CellPainter.GetCellState(columnIndex, rowIndex);
                        this.StyleContext.RenderBackground(cr, cellBounds.Left, cellBounds.Top, cellBounds.Width-5, cellBounds.Height-5);
                        var c = this.StyleContext.GetColor(this.StyleContext.State);
                        cr.SetSourceColor(new Cairo.Color(c.Red, c.Green, c.Blue, c.Alpha));
#else
                        cr.SetSourceColor(CellPainter.GetBackgroundColour(columnIndex, rowIndex));
                        cr.Fill();
                        cr.SetSourceColor(CellPainter.GetForegroundColour(columnIndex, rowIndex));

#endif
                        // Draw cell outline.
                        if (ShowLines)
                        {
                            cr.Rectangle(cellBounds.ToRectangle());
                            cr.Stroke();
                        }

                        //Set text font options for cell.
                        var layout = this.CreatePangoLayout(text);
                        layout.FontDescription = new Pango.FontDescription();
                        if (CellPainter.TextItalics(columnIndex, rowIndex))
                            layout.FontDescription.Style = Pango.Style.Italic;
                        if (CellPainter.TextBold(columnIndex, rowIndex))
                            layout.FontDescription.Weight = Pango.Weight.Bold;
                        layout.GetPixelExtents(out Pango.Rectangle inkRectangle, out Pango.Rectangle logicalRectangle);

                        // Vertically center the text.
                        double y = cellBounds.Top + (cellBounds.Height - logicalRectangle.Height) / 2;

                        // Horizontal alignment is determined by the cell painter.
                        double x;
                        if (CellPainter.TextLeftJustify(columnIndex, rowIndex))
                            x = cellBounds.Left + ColumnPadding;
                        else
                            x = cellBounds.Right - ColumnPadding - inkRectangle.Width;

                        cr.MoveTo(x, y);
                        Pango.CairoHelper.ShowLayout(cr, layout);
                    }
                    cr.ResetClip();
#if NETCOREAPP
                    this.StyleContext.State = StateFlags.Normal;
#endif

                }
            }
            catch (Exception ex)
            {
                MainView.MasterView.ShowError(ex);
            }
        }
    }
}