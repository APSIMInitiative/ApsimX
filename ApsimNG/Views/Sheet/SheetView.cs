using Cairo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UserInterface.Views
{
    public class SheetView
    {
        const int rowHeight = 35;
        const double lineWidth = 0.2;
        const bool showLines = true;
        bool leftJustify = false; // right justify 
        const int columnPadding = 10;   // The padding (in pixels) to go on the left and right size of a column.

        private ISheetDataProvider dataProvider;
        private int windowWidth;
        private int windowHeight;

        private int numberHeadingRows;
        private int numberFrozenColumns;

        /// <summary>Number of hidden columns. Hidden columns are always after the frozen columns.</summary>        
        private int numberHiddenColumns;

        /// <summary>Number of hidden rows. Hidden rows are always after the heading rows.</summary>
        private int numberHiddenRows;

        private int[] columnWidths;

        private ISheetCellPainter cellPainter;

        /// <summary>Constructor</summary>
        public SheetView(ISheetDataProvider data, ITextExtents textExtents, 
                         int width, int height, int numHeadingRows, int numFrozenColumns,
                         ISheetCellPainter cellPainterInstance)
        {
            dataProvider = data;
            windowWidth = width;
            windowHeight = height;
            numberHeadingRows = numHeadingRows;
            numberFrozenColumns = numFrozenColumns;
            cellPainter = cellPainterInstance;
            CalculateColumnWidths(textExtents);
        }

        /// <summary>Draw the view.</summary>
        public void Draw(Context cr)
        {
            foreach (var columnIndex in VisibleColumnIndexes())
                foreach (var rowIndex in VisibleRowIndexes())
                    DrawCell(cr, columnIndex, rowIndex);
        }

        public int MaximumNumberHiddenColumns { get; private set; }

        public int MaximumNumberHiddenRows { get; private set; }

        /// <summary>Calculates the bounds of a cell if it is visible (wholly or partially) to the user.</summary>
        /// <param name="columnIndex">The cell column index.</param>
        /// <param name="rowIndex">The cell row index.</param>
        /// <returns>The bounds if visible or null if not visible.</returns>
        public CellBounds CalculateBounds(int columnIndex, int rowIndex)
        {
            // Convert rowIndex that is relative to all rows to 
            // an index that is relative to the visible rows.
            var visibleRowIndex = rowIndex;
            if (rowIndex >= numberHeadingRows)
            {
                if (rowIndex < numberHiddenRows + numberHeadingRows)
                    return null;

                visibleRowIndex -= numberHiddenRows;
            }

            // Convert columnIndex that is relative to all columns to 
            // an index that is relative to the visible columns.
            var visibleColumnIndexes = VisibleColumnIndexes().ToList();
            var visibleColumnIndex = visibleColumnIndexes.IndexOf(columnIndex);

            var rowTop = visibleRowIndex * rowHeight;
            if (visibleColumnIndex != -1 && rowTop < windowHeight)
            {
                int left = visibleColumnIndexes.Take(visibleColumnIndex).Sum(c => columnWidths[c]);
                int top = rowTop;
                int width = columnWidths[visibleColumnIndex];
                int height = rowHeight;

                return new CellBounds(left, top, width, height);
            }
            else
                return null; // cell isn't visible.
        }

        public void ScrollRight()
        {
            // Get the right most fully visible column.
            var rightColumnIndex = VisibleColumnIndexes().Last();
            if (!IsColumnFullyVisible(rightColumnIndex))
                rightColumnIndex--;

            // Calculate the new right most column index.
            var newRightColumnIndex = rightColumnIndex + 1;

            numberHiddenColumns = CalculateNumberOfHiddenColumnsToMakeColumnVisible(newRightColumnIndex);
        }

        public void ScrollLeft()
        {
            if (numberHiddenColumns > 0)
                numberHiddenColumns--;
        }

        public void ScrollUp()
        {
            if (numberHiddenRows > 0)
                numberHiddenRows--;
        }

        public void ScrollDown()
        {
            var bottomRowIndex = LastFullyVisibleRowIndex();
            if (bottomRowIndex < dataProvider.RowCount - 1)
                numberHiddenRows++;
        }

        private int CalculateNumberOfHiddenColumnsToMakeColumnVisible(int columnIndex)
        {
            int savedNumberHiddenColumns = numberHiddenColumns;

            // Keep incrementing the number of hidden columns until the column
            // is fully visible.
            numberHiddenColumns = 0;
            while (columnIndex < dataProvider.ColumnCount - 1 && !IsColumnFullyVisible(columnIndex))
                numberHiddenColumns++;

            int returnColumnNumber = numberHiddenColumns;
            numberHiddenColumns = savedNumberHiddenColumns;
            return returnColumnNumber;
        }

        private bool IsColumnFullyVisible(int columnIndex)
        {
            var bounds = CalculateBounds(columnIndex, 0);
            if (bounds == null)
                return false;
            return bounds.Right < windowWidth;
        }

        private bool IsRowFullyVisible(int rowIndex)
        {
            var bounds = CalculateBounds(0, rowIndex);
            if (bounds == null)
                return false;
            return bounds.Bottom < windowHeight;
        }

        private void CalculateColumnWidths(ITextExtents cr)
        {
            columnWidths = new int[dataProvider.ColumnCount];
            for (int columnIndex = 0; columnIndex < dataProvider.ColumnCount; columnIndex++)
            {
                int columnWidth = 0;
                for (int rowIndex = 0; rowIndex < numberHeadingRows + 1; rowIndex++)
                {
                    var text = dataProvider.GetCellContents(columnIndex, rowIndex);
                    if (text == null)
                        text = string.Empty;
                    var extents = cr.TextExtents(text);
                    columnWidth = Math.Max(columnWidth, (int)extents.Width);
                }
                columnWidths[columnIndex] = columnWidth + columnPadding * 2;
            }
            MaximumNumberHiddenColumns = CalculateNumberOfHiddenColumnsToMakeColumnVisible(dataProvider.ColumnCount - 1);
            MaximumNumberHiddenRows = dataProvider.RowCount - LastFullyVisibleRowIndex();
        }

        private int LastFullyVisibleRowIndex()
        {
            var bottomRowIndex = VisibleRowIndexes().Last();
            // Get the bottom fully visible row.
            if (!IsRowFullyVisible(bottomRowIndex))
                bottomRowIndex--;
            return bottomRowIndex;
        }

        private IEnumerable<int> VisibleColumnIndexes()
        {
            int left = 0;   // horizontal pixel position

            int columnIndex = 0;
            while (columnIndex < dataProvider.ColumnCount && left < windowWidth)
            {
                if (columnIndex < numberFrozenColumns || columnIndex >= numberHiddenColumns + numberFrozenColumns)
                {
                    yield return columnIndex;
                    left += columnWidths[columnIndex];
                }

                columnIndex++;
            }
        }

        /// <summary>Draw the view.</summary>
        private IEnumerable<int> VisibleRowIndexes()
        {

            int top = 0;
            int rowIndex = 0;
            while (rowIndex < dataProvider.RowCount && top < windowHeight)
            {
                if (rowIndex < numberHeadingRows || rowIndex >= numberHiddenRows + numberHeadingRows)
                {
                    yield return rowIndex;
                    top += rowHeight;
                }

                rowIndex++;
            }
        }

        /// <summary>Draw a single cell to the context.</summary>
        /// <param name="cr">The context to draw in.</param>
        /// <param name="columnIndex">The column index.</param>
        /// <param name="rowIndex">The row index.</param>
        private void DrawCell(Context cr, int columnIndex, int rowIndex)
        {
            var text = dataProvider.GetCellContents(columnIndex, rowIndex);
            var cellBounds = CalculateBounds(columnIndex, rowIndex);

            if (text == null)
                text = string.Empty;

            cr.Rectangle(cellBounds.ToClippedRectangle(windowWidth, windowHeight));
            cr.Clip();

            cr.LineWidth = lineWidth;

            cr.Rectangle(cellBounds.ToRectangle());
            if (!cellPainter.PaintCell(columnIndex, rowIndex))
            {
                cr.SetSourceColor(cellPainter.GetForegroundColour(columnIndex, rowIndex));
                cr.Stroke();
            }
            else
            {
                cr.SetSourceColor(cellPainter.GetBackgroundColour(columnIndex, rowIndex));
                cr.Fill();
                cr.SetSourceColor(cellPainter.GetForegroundColour(columnIndex, rowIndex));
                if (showLines)
                {
                    cr.Rectangle(cellBounds.ToRectangle());
                    cr.Stroke();
                }

                TextExtents extents = cr.TextExtents(text);

                var maxHeight = cr.TextExtents("j").Height - cr.TextExtents("D").Height;
                maxHeight = 10;


                //var textExtents = cr.TextExtents("j")
                if (leftJustify)
                {
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