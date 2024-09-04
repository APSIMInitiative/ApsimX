using System;
using System.Linq;
using System.Text;

namespace Gtk.Sheet
{
    /// <summary>Implements single cell selection for the sheet widget.</summary>
    internal class SingleCellSelect : ISheetSelection
    {
        /// <summary>The sheet.</summary>
        protected Sheet sheet;

        /// <summary>The index of the current selected column.</summary>
        protected int selectedColumnIndex;

        /// <summary>The index of the current selected row.</summary>
        protected int selectedRowIndex;

        /// <summary>Constructor.</summary>
        /// <param name="sheet">The sheet.</param>
        /// <param name="sheetWidget">The sheet widget.</param>
        public SingleCellSelect(Sheet sheet)
        {
            this.sheet = sheet;
            selectedColumnIndex = 0;
            selectedRowIndex = sheet.NumberFrozenRows;
            sheet.KeyPress += OnKeyPressEvent;
            sheet.MouseClick += OnMouseClickEvent;
        }

        /// <summary>Clean up the instance.</summary>
        public void Cleanup()
        {
            sheet.KeyPress -= OnKeyPressEvent;
            sheet.MouseClick -= OnMouseClickEvent;
        }

        /// <summary>Gets whether a cell is selected.</summary>
        /// <param name="columnIndex">The index of the current selected column.</param>
        /// <param name="rowIndex">The index of the current selected row</param>
        /// <returns>True if selected, false otherwise.</returns>
        public virtual bool IsSelected(int columnIndex, int rowIndex)
        {
            return selectedColumnIndex == columnIndex && selectedRowIndex == rowIndex;
        }

        /// <summary>Gets the currently selected cell.</summary>
        /// <param name="columnIndex">The index of the current selected column.</param>
        /// <param name="rowIndex">The index of the current selected row</param>
        public void GetSelection(out int columnIndex, out int rowIndex)
        {
            columnIndex = selectedColumnIndex;
            rowIndex = selectedRowIndex;
        }

        /// <summary>
        /// Sets the cell to select
        /// </summary>
        /// <param name="columnIndex">Top left column to select.</param>
        /// <param name="rowIndex">Top left row to select.</param>
        public virtual void SetSelection(int columnIndex, int rowIndex)
        {
            selectedColumnIndex = columnIndex;
            selectedRowIndex = rowIndex;
        }

        /// <summary>Get selected cell contents.</summary>
        /// <param name="columnIndex">The index of the current selected column.</param>
        /// <param name="rowIndex">The index of the current selected row</param>
        public virtual string GetSelectedContents()
        {
            int dataRowIndex = selectedRowIndex - sheet.NumberFrozenRows;
            return sheet.DataProvider.GetCellContents(selectedColumnIndex, dataRowIndex);
        }

        /// <summary>Set selected cell contents.</summary>
        /// <param name="contents">The contents to set the selected cell to.</param>
        public virtual void SetSelectedContents(string contents)
        {
            if (sheet.CellEditor.IsEditing)
                sheet.CellEditor.EndEdit();

            int rowIndex = selectedRowIndex;

            foreach (string line in contents.Split("\n", StringSplitOptions.RemoveEmptyEntries))
            {
                int columnIndex = selectedColumnIndex;
                foreach (string word in line.Split('\t'))
                {
                    int dataRowIndex = rowIndex - sheet.NumberFrozenRows;
                    if (sheet.DataProvider.GetCellState(columnIndex, dataRowIndex) != SheetCellState.ReadOnly)
                        sheet.DataProvider.SetCellContents(new int[]{columnIndex}, new int[]{dataRowIndex}, new string[] {word});
                    columnIndex++;
                    if (columnIndex == sheet.DataProvider.ColumnCount)
                        break;
                }

                rowIndex++;
            }
            sheet.Refresh();
        }

        /// <summary>Invoked when the user presses a key.</summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="evnt">The event data.</param>
        protected virtual void OnKeyPressEvent(object sender, SheetEventKey evnt)
        {
            if (evnt.Key != Keys.None && (sheet.CellEditor == null || !sheet.CellEditor.IsEditing))
            {
                if (evnt.Key == Keys.Right && evnt.Control)
                    MoveToFarRight();
                else if (evnt.Key == Keys.Left && evnt.Control)
                    MoveToFarLeft();
                else if ((evnt.Key == Keys.Down && evnt.Control) || evnt.Key == Keys.End)
                    MoveToBottom();
                else if ((evnt.Key == Keys.Up && evnt.Control) || evnt.Key == Keys.Home)
                    MoveToTop();
                else if (evnt.Key == Keys.Left)
                    MoveLeft(evnt.Shift);
                else if (evnt.Key == Keys.Right)
                    MoveRight(evnt.Shift);
                else if (evnt.Key == Keys.Down)
                    MoveDown(evnt.Shift);
                else if (evnt.Key == Keys.Up)
                    MoveUp(evnt.Shift);
                else if (evnt.Key == Keys.PageDown)
                    PageDown();
                else if (evnt.Key == Keys.PageUp)
                    PageUp();
                else if (evnt.Key == Keys.Delete)
                    Delete();
                else if (evnt.Key == Keys.Period)
                    sheet.CellEditor.Edit('.');
                sheet.Refresh();
            }
            else if (evnt.KeyValue > 0 && evnt.KeyValue < 255)
            {
                if (sheet.CellEditor != null)
                    sheet.CellEditor.Edit(evnt.KeyValue);
            }
        }

        /// <summary>Invoked when the user clicks a mouse button.</summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="evnt">The event data.</param>
        protected virtual void OnMouseClickEvent(object sender, SheetEventButton evnt)
        {
            int colIndex;
            int rowIndex;
            if (sheet.CellHitTest(evnt.X, evnt.Y, out colIndex, out rowIndex) &&
                rowIndex >= sheet.NumberFrozenRows)
            {
                // If the cell is already selected then go into edit mode.
                if (IsSelected(colIndex, rowIndex))
                {
                    sheet.CellEditor?.Edit();
                }
                else
                {
                    sheet.CellEditor?.EndEdit();
                    selectedColumnIndex = colIndex;
                    selectedRowIndex = rowIndex;
                    sheet.Refresh();
                }
            }
        }

        /// <summary>Moves the selected cell to the left one column.</summary>
        public virtual void MoveLeft(bool shift = false)
        {
            selectedColumnIndex = Math.Max(selectedColumnIndex - 1, 0);
            if (!sheet.FullyVisibleColumnIndexes.Contains(selectedColumnIndex))
                sheet.ScrollLeft();
        }

        /// <summary>Moves the selected cell to the right one column.</summary>
        public virtual void MoveRight(bool shift = false)
        {
            selectedColumnIndex = Math.Min(selectedColumnIndex + 1, sheet.DataProvider.ColumnCount - 1);
            if (!sheet.FullyVisibleColumnIndexes.Contains(selectedColumnIndex))
                sheet.ScrollRight();
        }

        /// <summary>Moves the selected cell up one row.</summary>
        public virtual void MoveUp(bool shift = false)
        {
            selectedRowIndex = Math.Max(selectedRowIndex - 1, sheet.NumberFrozenRows);
            if (!sheet.FullyVisibleRowIndexes.Contains(selectedRowIndex))
                sheet.ScrollUp();
        }

        /// <summary>Moves the selected cell down one row.</summary>
        public virtual void MoveDown(bool shift = false)
        {
            selectedRowIndex = Math.Min(selectedRowIndex + 1, sheet.RowCount - 1);
            if (!sheet.FullyVisibleRowIndexes.Contains(selectedRowIndex))
                sheet.ScrollDown();
        }

        /// <summary>Moves the selected cell up one page of rows.</summary>
        public virtual void PageUp()
        {
            int pageSize = sheet.FullyVisibleRowIndexes.Count() - sheet.NumberFrozenRows;
            selectedRowIndex = Math.Max(selectedRowIndex - pageSize, sheet.NumberFrozenRows);
            sheet.ScrollUpPage();
        }

        /// <summary>Moves the selected cell down one page of rows.</summary>
        public virtual void PageDown()
        {
            int pageSize = sheet.FullyVisibleRowIndexes.Count() - sheet.NumberFrozenRows;
            selectedRowIndex = Math.Min(selectedRowIndex + pageSize, sheet.RowCount-1);
            sheet.ScrollDownPage();
        }

        /// <summary>Moves the selected cell to the far right column.</summary>
        public virtual void MoveToFarRight()
        {
            selectedColumnIndex = sheet.DataProvider.ColumnCount - 1;
            if (sheet.MaximumNumberHiddenColumns >= 0)
                sheet.NumberHiddenColumns = sheet.MaximumNumberHiddenColumns;
        }

        /// <summary>Moves the selected cell to the far left column.</summary>
        public virtual void MoveToFarLeft()
        {
            selectedColumnIndex = 0;
            sheet.NumberHiddenColumns = 0;
        }

        /// <summary>Moves the selected cell to bottom row.</summary>
        public virtual void MoveToBottom()
        {
            selectedRowIndex = sheet.RowCount - 1;
            if (sheet.MaximumNumberHiddenRows >= 0)
                sheet.NumberHiddenRows = sheet.MaximumNumberHiddenRows;
        }

        /// <summary>Moves the selected cell to the top row below headings.</summary>
        public virtual void MoveToTop()
        {
            selectedRowIndex = sheet.NumberFrozenRows;
            sheet.NumberHiddenRows = 0;
        }

        /// <summary>Delete contents of cells.</summary>
        public virtual void Delete()
        {
            if (sheet.CellEditor.IsEditing)
                sheet.CellEditor.EndEdit();

            sheet.DataProvider.SetCellContents(new int[1]{selectedColumnIndex}, new int[1]{selectedRowIndex}, new string[1]{""});
        }

        /// <summary>Select all cells</summary>
        public virtual void SelectAll()
        {
            return;
        }
    }
}