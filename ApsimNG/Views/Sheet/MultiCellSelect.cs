using System;
using System.Linq;
using System.Text;

namespace UserInterface.Views
{
    /// <summary>Implements single cell selection for the sheet widget.</summary>
    public class MultiCellSelect : SingleCellSelect
    {
        /// <summary>The index of the selected right column.</summary>
        private int selectedColumnIndexRight;

        /// <summary>The index of the selected bottom row.</summary>
        private int selectedRowIndexBottom;

        /// <summary>Constructor.</summary>
        /// <param name="sheet">The sheet.</param>
        /// <param name="sheetWidget">The sheet widget.</param>
        public MultiCellSelect(Sheet sheet, SheetWidget sheetWidget) : base(sheet, sheetWidget)
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
                base.MoveLeft();
                selectedColumnIndexRight = selectedColumnIndex;
                selectedRowIndexBottom = selectedRowIndex;
            }
        }

        /// <summary>Moves the selected cell to the right one column.</summary>
        public override void MoveRight(bool shift)
        {
            if (shift)
            {
                selectedColumnIndexRight = Math.Min(selectedColumnIndexRight + 1, sheet.DataProvider.ColumnCount - 1);
                if (!sheet.FullyVisibleColumnIndexes.Contains(selectedColumnIndexRight))
                    sheet.ScrollRight();
            }
            else
            {
                base.MoveRight();
                selectedColumnIndexRight = selectedColumnIndex;
                selectedRowIndexBottom = selectedRowIndex;
            }
        }

        /// <summary>Moves the selected cell up one row.</summary>
        public override void MoveUp(bool shift)
        {
            if (shift)
                selectedRowIndexBottom = Math.Max(selectedRowIndexBottom - 1, selectedRowIndex);
            else
            {
                base.MoveUp();
                selectedColumnIndexRight = selectedColumnIndex;
                selectedRowIndexBottom = selectedRowIndex;
            }
        }

        /// <summary>Moves the selected cell down one row.</summary>
        public override void MoveDown(bool shift)
        {
            if (shift)
            {
                selectedRowIndexBottom = Math.Min(selectedRowIndexBottom + 1, sheet.RowCount - 1);
                if (!sheet.FullyVisibleRowIndexes.Contains(selectedRowIndex))
                    sheet.ScrollDown();
            }
            else
            {
                base.MoveDown();
                selectedColumnIndexRight = selectedColumnIndex;
                selectedRowIndexBottom = selectedRowIndex;
            }
        }

        /// <summary>Copy cells to clipboard.</summary>
        public override void Copy()
        {
            if (selectedColumnIndexRight == selectedColumnIndex &&
                selectedRowIndexBottom == selectedRowIndex)
                base.Copy();
            else
            {
                StringBuilder textToCopy = new StringBuilder();
                for (int rowIndex = selectedRowIndex; rowIndex <= selectedRowIndexBottom; rowIndex++)
                {
                    for (int columnIndex = selectedColumnIndex; columnIndex <= selectedColumnIndexRight; columnIndex++)
                    {
                        var cellText = sheet.DataProvider.GetCellContents(columnIndex, rowIndex);
                        textToCopy.Append(cellText);
                        if (columnIndex != selectedColumnIndexRight)
                            textToCopy.Append('\t');
                    }
                    textToCopy.AppendLine();
                }
                sheetWidget.SetClipboard(textToCopy.ToString());
            }
        }

        /// <summary>Delete contents of cells.</summary>
        public override void Delete()
        {
            for (int rowIndex = selectedRowIndex; rowIndex <= selectedRowIndexBottom; rowIndex++)
                for (int columnIndex = selectedColumnIndex; columnIndex <= selectedColumnIndexRight; columnIndex++)
                    sheet.DataProvider.SetCellContents(columnIndex, rowIndex, null);
        }
    }
}