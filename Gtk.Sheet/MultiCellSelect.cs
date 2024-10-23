using System;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Gtk.Sheet
{
    /// <summary>Implements single cell selection for the sheet widget.</summary>
    internal class MultiCellSelect : SingleCellSelect
    {
        /// <summary>The index of the selected right column.</summary>
        private int selectedColumnIndexRight;

        /// <summary>The index of the selected bottom row.</summary>
        private int selectedRowIndexBottom;

        /// <summary>Constructor.</summary>
        /// <param name="sheet">The sheet.</param>
        /// <param name="sheetWidget">The sheet widget.</param>
        public MultiCellSelect(Sheet sheet) : base(sheet)
        {
            selectedColumnIndexRight = selectedColumnIndex;
            selectedRowIndexBottom = selectedRowIndex;
        }

        /// <summary>Gets whether a cell is selected.</summary>
        /// <param name="columnIndex">The index of the current selected column.</param>
        /// <param name="rowIndex">The index of the current selected row</param>
        /// <returns>True if selected, false otherwise.</returns>
        public override bool IsSelected(int columnIndex, int rowIndex)
        {
            return columnIndex >= selectedColumnIndex && columnIndex <= selectedColumnIndexRight &&
                   rowIndex >= selectedRowIndex && rowIndex <= selectedRowIndexBottom;
        }

        /// <summary>
        /// Sets the cells to select
        /// </summary>
        /// <param name="columnIndex1">Top left column to select.</param>
        /// <param name="rowIndex1">Top left row to select.</param>
        /// <param name="columnIndex2">Bottom right column to select.</param>
        /// <param name="rowIndex2">Bottom right row to select.</param>
        public void SetSelection(int columnIndex1, int rowIndex1, int columnIndex2, int rowIndex2)
        {
            base.SetSelection(columnIndex1, rowIndex1);
            selectedColumnIndexRight = columnIndex2;
            selectedRowIndexBottom = rowIndex2;
        }

        /// <summary>Get selected cell contents.</summary>
        /// <param name="columnIndex">The index of the current selected column.</param>
        /// <param name="rowIndex">The index of the current selected row</param>
        public override string GetSelectedContents()
        {
            if (sheet.CellEditor != null)
                if (sheet.CellEditor.IsEditing)
                    sheet.CellEditor.EndEdit();

            if (selectedColumnIndexRight == selectedColumnIndex &&
                selectedRowIndexBottom == selectedRowIndex)
                return base.GetSelectedContents();
            else
            {
                StringBuilder textToCopy = new StringBuilder();
                for (int rowIndex = selectedRowIndex; rowIndex <= selectedRowIndexBottom; rowIndex++)
                {
                    for (int columnIndex = selectedColumnIndex; columnIndex <= selectedColumnIndexRight; columnIndex++)
                    {
                        int dataRowIndex = rowIndex - sheet.NumberFrozenRows;
                        var cellText = sheet.DataProvider.GetCellContents(columnIndex, dataRowIndex);
                        textToCopy.Append(cellText);
                        if (columnIndex != selectedColumnIndexRight)
                            textToCopy.Append('\t');
                    }
                    textToCopy.AppendLine();
                }
                return textToCopy.ToString();
            }
        }

        /// <summary>Invoked when the user clicks a mouse button.</summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="evnt">The event data.</param>
        protected override void OnMouseClickEvent(object sender, SheetEventButton evnt)
        {
            if (evnt.Shift && sheet.CellHitTest(evnt.X, evnt.Y, out int colIndex, out int rowIndex))
            {
                selectedColumnIndexRight = colIndex;
                selectedRowIndexBottom = rowIndex;

                //flip indexs around if cells were selected in a backwards order
                if (selectedColumnIndexRight < selectedColumnIndex)
                {
                    int temp = selectedColumnIndex;
                    selectedColumnIndex = selectedColumnIndexRight;
                    selectedColumnIndexRight = temp;
                }
                if (selectedRowIndexBottom < selectedRowIndex)
                {
                    int temp = selectedRowIndex;
                    selectedRowIndex = selectedRowIndexBottom;
                    selectedRowIndexBottom = temp;
                }
                sheet.Refresh();
            }
            else
            {
                sheet.CellEditor?.EndEdit();
                base.OnMouseClickEvent(sender, evnt);
                selectedColumnIndexRight = selectedColumnIndex;
                selectedRowIndexBottom = selectedRowIndex;
            }
        }

        /// <summary>Moves the selected cell to the left one column.</summary>
        public override void MoveLeft(bool shift)
        {
            if (shift)
                selectedColumnIndexRight = Math.Max(selectedColumnIndexRight - 1, selectedColumnIndex);
            else
            {
                int difference = selectedColumnIndexRight - selectedColumnIndex;
                base.MoveLeft();
                selectedColumnIndexRight = Math.Max(selectedColumnIndexRight - 1, difference);
            }
        }

        /// <summary>Moves the selected cell to the right one column.</summary>
        public override void MoveRight(bool shift)
        {
            int difference = selectedColumnIndexRight - selectedColumnIndex;
            selectedColumnIndexRight = Math.Min(selectedColumnIndexRight + 1, sheet.DataProvider.ColumnCount - 1);

            if (!shift)
                selectedColumnIndex = selectedColumnIndexRight - difference;

            if (!sheet.FullyVisibleColumnIndexes.Contains(selectedColumnIndexRight))
                sheet.ScrollRight();
        }

        /// <summary>Moves the selected cell up one row.</summary>
        public override void MoveUp(bool shift)
        {
            if (shift)
                selectedRowIndexBottom = Math.Max(selectedRowIndexBottom - 1, selectedRowIndex);
            else
            {
                int difference = selectedRowIndexBottom - selectedRowIndex;
                base.MoveUp();
                selectedRowIndexBottom = Math.Max(selectedRowIndexBottom - 1, sheet.NumberFrozenRows + difference);
            }
        }

        /// <summary>Moves the selected cell down one row.</summary>
        public override void MoveDown(bool shift)
        {
            int difference = selectedRowIndexBottom - selectedRowIndex;
            selectedRowIndexBottom = Math.Min(selectedRowIndexBottom + 1, sheet.RowCount - 1);

            if (!shift)
                selectedRowIndex = selectedRowIndexBottom - difference;

            if (!sheet.FullyVisibleRowIndexes.Contains(selectedRowIndex))
                sheet.ScrollDown();
        }

        /// <summary>Moves the selected cell up one page of rows.</summary>
        public override void PageUp()
        {
            base.PageUp();
            int pageSize = sheet.FullyVisibleRowIndexes.Count() - sheet.NumberFrozenRows;
            selectedRowIndexBottom = Math.Max(selectedRowIndexBottom - pageSize, sheet.NumberFrozenRows);
        }

        /// <summary>Moves the selected cell down one page of rows.</summary>
        public override void PageDown()
        {
            base.PageDown();
            int pageSize = sheet.FullyVisibleRowIndexes.Count() - sheet.NumberFrozenRows;
            selectedRowIndexBottom = Math.Min(selectedRowIndexBottom + pageSize, sheet.RowCount-1);
        }

        /// <summary>Moves the selected cell to the far right column.</summary>
        public override void MoveToFarRight()
        {
            int difference = selectedColumnIndexRight - selectedColumnIndex;
            base.MoveToFarRight();
            selectedColumnIndex = sheet.DataProvider.ColumnCount - 1 - difference;
            selectedColumnIndexRight = sheet.DataProvider.ColumnCount - 1;
        }

        /// <summary>Moves the selected cell to the far left column.</summary>
        public override void MoveToFarLeft()
        {
            int difference = selectedColumnIndexRight - selectedColumnIndex;
            base.MoveToFarLeft();
            selectedColumnIndex = 0;
            selectedColumnIndexRight = difference;
        }

        /// <summary>Moves the selected cell to bottom row.</summary>
        public override void MoveToBottom()
        {
            int difference = selectedRowIndexBottom - selectedRowIndex;
            base.MoveToBottom();
            selectedRowIndex = sheet.RowCount - 1 - difference;
            selectedRowIndexBottom = sheet.RowCount - 1;
        }

        /// <summary>Moves the selected cell to the top row below headings.</summary>
        public override void MoveToTop()
        {
            int difference = selectedRowIndexBottom - selectedRowIndex;
            base.MoveToTop();
            selectedRowIndex = sheet.NumberFrozenRows;
            selectedRowIndexBottom = sheet.NumberFrozenRows + difference;
        }

        /// <summary>Delete contents of cells.</summary>
        public override void Delete()
        {
            if (sheet.CellEditor != null)
                if (sheet.CellEditor.IsEditing)
                    sheet.CellEditor.EndEdit();

            int i = 0;
            int length = (selectedRowIndexBottom - selectedRowIndex + 1) * (selectedColumnIndexRight - selectedColumnIndex + 1);
            int[] rowIndices = new int[length];
            int[] columnIndices = new int[length];
            string[] values = new string[length];
            for (int rowIndex = selectedRowIndex; rowIndex <= selectedRowIndexBottom; rowIndex++)
            {
                for (int columnIndex = selectedColumnIndex; columnIndex <= selectedColumnIndexRight; columnIndex++)
                {
                    rowIndices[i] = rowIndex - sheet.NumberFrozenRows;
                    columnIndices[i] = columnIndex;
                    values[i] = string.Empty;
                    i++;
                }
            }

            // Detect when the entire row is selected.
            if (selectedColumnIndexRight - selectedColumnIndex + 1 == sheet.DataProvider.ColumnCount)
                sheet.DataProvider.DeleteRows(rowIndices.Distinct().ToArray());
            else
                sheet.DataProvider.SetCellContents(columnIndices, rowIndices, values);
        }

        /// <summary>Select all cells</summary>
        public override void SelectAll()
        {
            if (sheet.CellEditor != null)
                if (sheet.CellEditor.IsEditing)
                    sheet.CellEditor.EndEdit();

            selectedColumnIndex = 0;
            selectedColumnIndexRight = sheet.DataProvider.ColumnCount - 1;

            selectedRowIndex = sheet.NumberFrozenRows;
            selectedRowIndexBottom = sheet.NumberFrozenRows + sheet.DataProvider.RowCount - 1;
        }

        /// <summary>Delete contents of cells.</summary>
        public int GetNumberOfCellsSelected()
        {
            int width = selectedColumnIndexRight - selectedColumnIndex + 1;
            int height = selectedRowIndexBottom - selectedRowIndex + 1;

            return width * height;
        }
    }
}