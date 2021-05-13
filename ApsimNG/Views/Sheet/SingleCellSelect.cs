using Gdk;
using System;
using System.Linq;

namespace UserInterface.Views
{
    /// <summary>Implements single cell selection for the sheet widget.</summary>
    public class SingleCellSelect : ISheetSelection
    {
        /// <summary>The sheet widget.</summary>
        private SheetView sheet;

        /// <summary>The index of the current selected column.</summary>
        private int selectedColumnIndex;

        /// <summary>The index of the current selected row.</summary>
        private int selectedRowIndex;

        /// <summary>Constructor.</summary>
        /// <param name="sheetView">The sheet widget.</param>
        public SingleCellSelect(SheetView sheetView)
        {
            sheet = sheetView;
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

        /// <summary>The optional sheet editor</summary>
        public ISheetEditor Editor { get; set; }

        /// <summary>Gets whether a cell is selected.</summary>
        /// <param name="columnIndex">The index of the current selected column.</param>
        /// <param name="rowIndex">The index of the current selected row</param>
        /// <returns>True if selected, false otherwise.</returns>
        public bool IsSelected(int columnIndex, int rowIndex)
        {
            return selectedColumnIndex == columnIndex && selectedRowIndex == rowIndex;
        }

        /// <summary>Gets the currently selected cell..</summary>
        /// <param name="columnIndex">The index of the current selected column.</param>
        /// <param name="rowIndex">The index of the current selected row</param>
        public void GetSelection(out int columnIndex, out int rowIndex)
        {
            columnIndex = selectedColumnIndex;
            rowIndex = selectedRowIndex;
        }

        /// <summary>Invoked when the user presses a key.</summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="evnt">The event data.</param>
        private void OnKeyPressEvent(object sender, EventKey evnt)
        {
            if (Editor == null || !Editor.IsEditing)
            {
                if (evnt.Key == Gdk.Key.Right && evnt.State == ModifierType.ControlMask)
                    MoveToFarRight();
                else if (evnt.Key == Gdk.Key.Left && evnt.State == ModifierType.ControlMask)
                    MoveToFarLeft();
                else if (evnt.Key == Gdk.Key.Down && evnt.State == ModifierType.ControlMask)
                    MoveToBottom();
                else if (evnt.Key == Gdk.Key.Up && evnt.State == ModifierType.ControlMask)
                    MoveToTop();
                else if (evnt.Key == Gdk.Key.Left)
                    MoveLeft();
                else if (evnt.Key == Gdk.Key.Right)
                    MoveRight();
                else if (evnt.Key == Gdk.Key.Down)
                    MoveDown();
                else if (evnt.Key == Gdk.Key.Up)
                    MoveUp();
                else if (evnt.Key == Gdk.Key.Page_Down)
                    PageDown();
                else if (evnt.Key == Gdk.Key.Page_Up)
                    PageUp();

                sheet.Refresh();
            }
        }

        /// <summary>Invoked when the user clicks a mouse button.</summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="evnt">The event data.</param>
        private void OnMouseClickEvent(object sender, EventButton evnt)
        {
            if (evnt.Type == EventType.ButtonPress)
            {
                if (sheet.CellHitTest((int)evnt.X, (int)evnt.Y, out selectedColumnIndex, out selectedRowIndex) &&
                    selectedRowIndex >= sheet.NumberFrozenRows)
                {
                    sheet.GrabFocus();
                    sheet.Refresh();
                }
            }
        }

        /// <summary>Moves the selected cell to the left one column.</summary>
        private void MoveLeft()
        {
            selectedColumnIndex = Math.Max(selectedColumnIndex - 1, 0);
            if (!sheet.FullyVisibleColumnIndexes.Contains(selectedColumnIndex))
                sheet.ScrollLeft();
        }

        /// <summary>Moves the selected cell to the right one column.</summary>
        private void MoveRight()
        {
            selectedColumnIndex = Math.Min(selectedColumnIndex + 1, sheet.DataProvider.ColumnCount - 1);
            if (!sheet.FullyVisibleColumnIndexes.Contains(selectedColumnIndex))
                sheet.ScrollRight();
        }

        /// <summary>Moves the selected cell up one row.</summary>
        private void MoveUp()
        {
            selectedRowIndex = Math.Max(selectedRowIndex - 1, sheet.NumberFrozenRows);
            if (!sheet.FullyVisibleRowIndexes.Contains(selectedRowIndex))
                sheet.ScrollUp();
        }

        /// <summary>Moves the selected cell down one row.</summary>
        private void MoveDown()
        {
            selectedRowIndex = Math.Min(selectedRowIndex + 1, sheet.DataProvider.RowCount - 1);
            if (!sheet.FullyVisibleRowIndexes.Contains(selectedRowIndex))
                sheet.ScrollDown();
        }

        /// <summary>Moves the selected cell up one page of rows.</summary>
        private void PageUp()
        {
            int pageSize = sheet.FullyVisibleRowIndexes.Count() - sheet.NumberFrozenRows;
            selectedRowIndex = Math.Max(selectedRowIndex - pageSize, sheet.NumberFrozenRows);
            sheet.ScrollUpPage();
        }

        /// <summary>Moves the selected cell down one page of rows.</summary>
        private void PageDown()
        {
            int pageSize = sheet.FullyVisibleRowIndexes.Count() - sheet.NumberFrozenRows;
            selectedRowIndex = Math.Min(selectedRowIndex + pageSize, sheet.DataProvider.RowCount-1);
            sheet.ScrollDownPage();
        }

        /// <summary>Moves the selected cell to the far right column.</summary>
        private void MoveToFarRight()
        {
            selectedColumnIndex = sheet.DataProvider.ColumnCount - 1;
            sheet.NumberHiddenColumns = sheet.MaximumNumberHiddenColumns;
        }

        /// <summary>Moves the selected cell to the far left column.</summary>
        private void MoveToFarLeft()
        {
            selectedColumnIndex = 0;
            sheet.NumberHiddenColumns = 0;
        }

        /// <summary>Moves the selected cell to bottom row.</summary>
        private void MoveToBottom()
        {
            selectedRowIndex = sheet.DataProvider.RowCount - 1;
            sheet.NumberHiddenRows = sheet.MaximumNumberHiddenRows;
        }

        /// <summary>Moves the selected cell to the top row below headings.</summary>
        private void MoveToTop()
        {
            selectedRowIndex = sheet.NumberFrozenRows;
            sheet.NumberHiddenRows = 0;
        }
    }
}