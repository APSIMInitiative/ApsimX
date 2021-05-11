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
    public class SheetView : EventBox
    {
        const double lineWidth = 0.2;
        const bool showLines = true;
        const int columnPadding = 10;   // The padding (in pixels) to go on the left and right size of a column.

        //private HScrollbar horizontalScrollbar;
        //private VScrollbar verticalScrollbar;
        //private SheetView view;
        //private const int scrollBarWidth = 20;

        /// <summary>Constructor</summary>
        public SheetView()
        {
            CanFocus = true;
        }

        public ISheetDataProvider DataProvider { get; set; }

        public ISheetCellPainter CellPainter { get; set; }

        public int[] ColumnWidths { get; set; }

        public int RowHeight { get; set; } = 35;

        public int NumberFrozenRows { get; set; }

        public int NumberFrozenColumns { get; set; }

        /// <summary>Number of hidden columns. Hidden columns are always after the frozen columns.</summary>        
        public int NumberHiddenColumns { get; set; }

        /// <summary>Number of hidden rows. Hidden rows are always after the heading rows.</summary>
        public int NumberHiddenRows { get; set; }

        public int MaximumNumberHiddenColumns { get; private set; }

        public int MaximumNumberHiddenRows { get; private set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public IEnumerable<int> VisibleColumnIndexes {  get { return DetermineVisibleColumnIndexes(fullyVisible: false);  } }

        public IEnumerable<int> VisibleRowIndexes { get { return DetermineVisibleRowIndexes(fullyVisible: false); } }
        
        public IEnumerable<int> FullyVisibleColumnIndexes { get { return DetermineVisibleColumnIndexes(fullyVisible: true); } }

        public IEnumerable<int> FullyVisibleRowIndexes { get { return DetermineVisibleRowIndexes(fullyVisible: true); } }

        public event EventHandler<EventKey> KeyPress;

        public event EventHandler<EventButton> MouseClick;

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

        public void ScrollRight()
        {
            // Get the right most fully visible column.
            var rightColumnIndex = FullyVisibleColumnIndexes.Last();

            // Calculate the new right most column index.
            var newRightColumnIndex = Math.Min(rightColumnIndex + 1, DataProvider.ColumnCount-1);

            NumberHiddenColumns = CalculateNumberOfHiddenColumnsToMakeColumnVisible(newRightColumnIndex);
        }

        public void ScrollLeft()
        {
            if (NumberHiddenColumns > 0)
                NumberHiddenColumns--;
        }

        public void ScrollUp()
        {
            if (NumberHiddenRows > 0)
                NumberHiddenRows--;
        }

        public void ScrollDown()
        {
            var bottomRowIndex = FullyVisibleRowIndexes.Last();
            if (bottomRowIndex < DataProvider.RowCount - 1)
                NumberHiddenRows++;
        }

        public void ScrollDownPage()
        {
            int pageSize = FullyVisibleRowIndexes.Count() - NumberFrozenRows; 
            NumberHiddenRows = Math.Min(NumberHiddenRows + pageSize, DataProvider.RowCount - 1);
        }

        public void ScrollUpPage()
        {
            int pageSize = FullyVisibleRowIndexes.Count() - NumberFrozenRows; 
            NumberHiddenRows = Math.Max(NumberHiddenRows - pageSize, 0);
        }

        public void Refresh()
        {
            QueueDraw();
        }

        public bool CellHitTest(int pixelX, int pixelY, out int columnIndex, out int rowIndex)
        {
            columnIndex = 0;
            rowIndex = VisibleRowIndexes.ElementAt(pixelY / RowHeight);
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
        /// <summary>Called by base class to draw the grid widget.</summary>
        /// <param name="expose">The context to draw in.</param>
        protected override bool OnExposeEvent(EventExpose expose)
        {
            Context cr = CairoHelper.Create(expose.Window);

            cr.SelectFontFace("Segoe UI", FontSlant.Normal, FontWeight.Normal);
            cr.SetFontSize(18);

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
        /// <summary>Called by base class to draw the grid widget.</summary>
        /// <param name="cr">The context to draw in.</param>
        protected override bool OnDrawn(Context cr)
        {
            cr.SelectFontFace("Segoe UI", FontSlant.Normal, FontWeight.Normal);
            cr.SetFontSize(18);

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
        private void Initialise(Context cr)
        {
#if NETFRAMEWORK
            Width = Parent.Allocation.Width;
            Height = Parent.Allocation.Height;
#else
            Width = Parent.AllocatedWidth;
            Height = Parent.AllocatedHeight;
#endif
            CalculateColumnWidths(cr);
        }

        /// <summary>Invoked when the user presses a key.</summary>
        /// <param name="evnt">The event data.</param>
        /// <returns>True if event has handled.</returns>
        protected override bool OnKeyPressEvent(EventKey evnt)
        {
            KeyPress?.Invoke(this, evnt);
            return true;
        }

        /// <summary>Invoked when the user clicks a mouse button.</summary>
        /// <param name="evnt">The event data.</param>
        /// <returns></returns>
        protected override bool OnButtonPressEvent(EventButton evnt)
        {
            MouseClick?.Invoke(this, evnt);
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

        private void OnHorizontalScrollbarChanged(object sender, EventArgs e)
        {
            //if (view.NumberColumnsScrolled != horizontalScrollbar.Value)
            //{
            //    view.ScrollHorizontalTo(horizontalScrollbar.Value);
            //    QueueDraw();
            //}
        }

        private void OnVerticalScrollbarChanged(object sender, EventArgs e)
        {
            //view.ScrollVerticalTo(verticalScrollbar.Value);
            //QueueDraw();
        }

        private void OnHorizontallyScrolled(object sender, EventArgs e)
        {
            //if (horizontalScrollbar.Value != view.NumberColumnsScrolled)
            //    horizontalScrollbar.Value = view.NumberColumnsScrolled;
        }

        private void AddScollBars()
        {
            /*var horizontalAdjustment = new Adjustment(1, 0, view.MaximumNumberHiddenColumns + 1, 1, 1, 1);
            horizontalScrollbar = new HScrollbar(horizontalAdjustment);
            horizontalScrollbar.Value = 0;
            horizontalScrollbar.ValueChanged += OnHorizontalScrollbarChanged;
            horizontalScrollbar.SetSizeRequest(windowWidth, scrollBarWidth);

            var verticalAdjustment = new Adjustment(1, 0, view.MaximumNumberHiddenRows + 1, 1, 1, 1);
            verticalScrollbar = new VScrollbar(verticalAdjustment);
            verticalScrollbar.Value = 0;
            verticalScrollbar.ValueChanged += OnVerticalScrollbarChanged;
            verticalScrollbar.SetSizeRequest(scrollBarWidth, windowHeight);

            fix = new Fixed();
            fix.Put(horizontalScrollbar, 0, windowHeight);
            fix.Put(verticalScrollbar, windowWidth - scrollBarWidth, 0);

            Add(fix);
            fix.ShowAll();*/
        }

        /// <summary>Update the position of the scroll bars to match the view.</summary>
        private void UpdateScrollBars()
        {
            //if (horizontalScrollbar.Value != view.NumberColumnsScrolled)
            //{
            //    // Disconnect the ValueChanged event handler from the scroll bar
            //    // before we change the value, otherwise we will trigger the 
            //    // event which isn't wanted.
            //    horizontalScrollbar.ValueChanged -= OnHorizontalScrollbarChanged;
            //    horizontalScrollbar.Value = view.NumberColumnsScrolled;
            //    horizontalScrollbar.ValueChanged += OnHorizontalScrollbarChanged;
            //}
            //if (verticalScrollbar.Value != view.NumberRowsScrolled)
            //{
            //    // Disconnect the ValueChanged event handler from the scroll bar
            //    // before we change the value, otherwise we will trigger the 
            //    // event which isn't wanted.
            //    verticalScrollbar.ValueChanged -= OnVerticalScrollbarChanged;
            //    verticalScrollbar.Value = view.NumberRowsScrolled;
            //    verticalScrollbar.ValueChanged -= OnVerticalScrollbarChanged;
            //}
        }

        private int CalculateNumberOfHiddenColumnsToMakeColumnVisible(int columnIndex)
        {
            int savedNumberHiddenColumns = NumberHiddenColumns;

            // Keep incrementing the number of hidden columns until the column
            // is fully visible.
            NumberHiddenColumns = 0;
            while (!FullyVisibleColumnIndexes.Contains(columnIndex))
                NumberHiddenColumns++;

            int returnColumnNumber = NumberHiddenColumns;
            NumberHiddenColumns = savedNumberHiddenColumns;
            return returnColumnNumber;
        }

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
                    var extents = cr.TextExtents(text);
                    columnWidth = Math.Max(columnWidth, (int)extents.Width);
                }
                ColumnWidths[columnIndex] = columnWidth + columnPadding * 2;
            }
            MaximumNumberHiddenColumns = CalculateNumberOfHiddenColumnsToMakeColumnVisible(DataProvider.ColumnCount - 1);
            MaximumNumberHiddenRows = DataProvider.RowCount - FullyVisibleRowIndexes.Last();
        }

        /// <summary>Draw a single cell to the context.</summary>
        /// <param name="cr">The context to draw in.</param>
        /// <param name="columnIndex">The column index.</param>
        /// <param name="rowIndex">The row index.</param>
        private void DrawCell(Context cr, int columnIndex, int rowIndex)
        {
            var text = DataProvider.GetCellContents(columnIndex, rowIndex);
            var cellBounds = CalculateBounds(columnIndex, rowIndex);

            if (text == null)
                text = string.Empty;

            cr.Rectangle(cellBounds.ToClippedRectangle(Width, Height));
            cr.Clip();

            cr.LineWidth = lineWidth;

            cr.Rectangle(cellBounds.ToRectangle());
            if (!CellPainter.PaintCell(columnIndex, rowIndex))
            {
                cr.SetSourceColor(CellPainter.GetForegroundColour(columnIndex, rowIndex));
                cr.Stroke();
            }
            else
            {
                cr.SetSourceColor(CellPainter.GetBackgroundColour(columnIndex, rowIndex));
                cr.Fill();
                cr.SetSourceColor(CellPainter.GetForegroundColour(columnIndex, rowIndex));
                if (showLines)
                {
                    cr.Rectangle(cellBounds.ToRectangle());
                    cr.Stroke();
                }

                // Set text font options for cell.
                var italics = FontSlant.Normal;
                if (CellPainter.TextItalics(columnIndex, rowIndex))
                    italics = FontSlant.Italic;
                var bold = FontWeight.Normal;
                if (CellPainter.TextBold(columnIndex, rowIndex))
                    bold = FontWeight.Bold;
                cr.SelectFontFace("sans-serif", italics, bold);

                TextExtents extents = cr.TextExtents(text);
                var maxHeight = cr.TextExtents("j").Height - cr.TextExtents("D").Height;
                maxHeight = 10;

                //var textExtents = cr.TextExtents("j")
                if (CellPainter.TextLeftJustify(columnIndex, rowIndex))
                {
                    // left justify
                    cr.MoveTo(cellBounds.Left + columnPadding, cellBounds.Top + cellBounds.Height - maxHeight);
                    cr.TextPath(text);
                }
                else
                {
                    // right justify
                    var textExtents = cr.TextExtents(text);
                    cr.MoveTo(cellBounds.Right - columnPadding - textExtents.Width, cellBounds.Bottom - maxHeight);
                    cr.TextPath(text);
                }

                cr.Fill();
            }
            cr.ResetClip();
        }
    }
}