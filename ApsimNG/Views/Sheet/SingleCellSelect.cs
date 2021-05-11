using Gdk;
using System;
using System.Linq;

namespace UserInterface.Views
{
    public class SingleCellSelect : ISheetSelection
    {
        private SheetView sheet;
        private int selectedColumnIndex;
        private int selectedRowIndex;

        public SingleCellSelect(SheetView sheetView)
        {
            sheet = sheetView;
            selectedColumnIndex = 0;
            selectedRowIndex = sheet.NumberFrozenRows;
            sheet.KeyPress += OnKeyPressEvent;
            sheet.MouseClick += OnMouseClickEvent;
        }

        public ISheetEditor Editor { get; set; }

        public bool IsSelected(int columnIndex, int rowIndex)
        {
            return selectedColumnIndex == columnIndex && selectedRowIndex == rowIndex;
        }

        /// <summary>Invoked when the user presses a key.</summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="evnt">The event data.</param>
        /// <returns>True if event has handled.</returns>
        private void OnKeyPressEvent(object sender, EventKey evnt)
        {
            if (Editor == null || !Editor.IsEditing)
            {
                if (evnt.Key == Gdk.Key.Left)
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
        /// <returns></returns>
        private void OnMouseClickEvent(object sender, EventButton evnt)
        {
            if (evnt.Type == EventType.ButtonPress)
            {
                if (sheet.CellHitTest((int)evnt.X, (int)evnt.Y, out selectedColumnIndex, out selectedRowIndex))
                {
                    sheet.GrabFocus();
                    sheet.Refresh();
                }
            }
        }

        private void MoveLeft()
        {
            selectedColumnIndex = Math.Max(selectedColumnIndex - 1, 0);
            if (!sheet.FullyVisibleColumnIndexes.Contains(selectedColumnIndex))
                sheet.ScrollLeft();
        }

        private void MoveRight()
        {
            selectedColumnIndex = Math.Min(selectedColumnIndex + 1, sheet.DataProvider.ColumnCount - 1);
            if (!sheet.FullyVisibleColumnIndexes.Contains(selectedColumnIndex))
                sheet.ScrollRight();
        }

        private void MoveUp()
        {
            selectedRowIndex = Math.Max(selectedRowIndex - 1, sheet.NumberFrozenRows);
            if (!sheet.FullyVisibleRowIndexes.Contains(selectedRowIndex))
                sheet.ScrollUp();
        }
        private void MoveDown()
        {
            selectedRowIndex = Math.Min(selectedRowIndex + 1, sheet.DataProvider.RowCount - 1);
            if (!sheet.FullyVisibleRowIndexes.Contains(selectedRowIndex))
                sheet.ScrollDown();
        }

        private void PageUp()
        {
            int pageSize = sheet.FullyVisibleRowIndexes.Count() - sheet.NumberFrozenRows;
            selectedRowIndex = Math.Max(selectedRowIndex - pageSize, sheet.NumberFrozenRows);
            sheet.ScrollUpPage();
        }

        private void PageDown()
        {
            int pageSize = sheet.FullyVisibleRowIndexes.Count() - sheet.NumberFrozenRows;
            selectedRowIndex = Math.Min(selectedRowIndex + pageSize, sheet.DataProvider.RowCount-1);
            sheet.ScrollDownPage();
        }

    }
}
