using Gtk;
using System;

namespace Gtk.Sheet
{
    internal class CellEditor : ISheetEditor
    {
        /// <summary>The sheet.</summary>
        private readonly Sheet sheet;

        /// <summary>The sheet widget.</summary>
        private readonly SheetWidget sheetWidget;

        /// <summary>The gtk entry box used when editing a sheet cell.</summary>
        private Entry entry;

        /// <summary>The gtk fixed positioning container for the entry box used when editing a sheet cell.</summary>
        private Fixed fix = new Fixed();

        ///// <summary></summary>
        //public event EventHandler<NeedContextItemsArgs> ShowIntellisense;

        /// <summary>Constructor.</summary>
        /// <param name="sheet">The sheet.</param>
        /// <param name="sheetWidget">The sheet widget.</param>
        public CellEditor(Sheet sheet, SheetWidget sheetWidget)
        {
            this.sheet = sheet;
            this.sheetWidget = sheetWidget;
            sheet.MouseClick += OnMouseClickEvent;
            sheet.KeyPress += OnKeyPressEvent;
        }

        public bool IsEditing => entry != null;


        /// <summary>Cleanup the instance.</summary>
        private void Cleanup()
        {
            sheet.MouseClick -= OnMouseClickEvent;
            sheet.KeyPress -= OnKeyPressEvent;
        }

        /// <summary>Invoked when the user clicks a mouse button.</summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="evnt">The event data.</param>
        private void OnMouseClickEvent(object sender, SheetEventButton evnt)
        {
        }

        /// <summary>Invoked when the user presses a key.</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="evnt">The event data.</param>
        private void OnKeyPressEvent(object sender, SheetEventKey evnt)
        {

        }
        
        /// <summary>Display an entry box for the user to edit the current selected cell data.</summary>
        public void Edit(char defaultChar = char.MinValue)
        {
            
            EndEdit();
            
            sheet.CellSelector.GetSelection(out int selectedColumnIndex, out int selectedRowIndex);
            if (sheet.DataProvider.GetCellState(selectedColumnIndex, selectedRowIndex) != SheetDataProviderCellState.ReadOnly)
            {
                var cellBounds = sheet.CalculateBounds(selectedColumnIndex, selectedRowIndex);
                
                entry = new Entry();
                entry.SetSizeRequest((int)cellBounds.Width - 3, (int)cellBounds.Height - 10);
                entry.WidthChars = 5;
                if (defaultChar == char.MinValue)
                    entry.Text = sheet.DataProvider.GetCellContents(selectedColumnIndex, selectedRowIndex);
                else
                    entry.Text = defaultChar.ToString();
                entry.KeyPressEvent += OnEntryKeyPress;
                if (!sheet.CellPainter.TextLeftJustify(selectedColumnIndex, selectedRowIndex))
                    entry.Alignment = 1; // right

                if (sheetWidget.Children.Length == 1)
                {
                    fix = (Fixed)sheetWidget.Child;
                }
                else
                {
                    fix = new Fixed();
                    sheetWidget.Add(fix);
                }
                fix.Put(entry, (int)cellBounds.Left + 1, (int)cellBounds.Top + 1);

                //////////////////////////////////////
                Paned parentPane = null;
                Widget parent = sheetWidget;
                while (parent != null && parentPane == null)
                {
                    if (!(parent is Paned))
                        parent = parent.Parent;
                    else
                        parentPane = parent as Paned;
                }
                int panedPos = 0;
                if (parentPane != null)
                    panedPos = parentPane.Position;

                //this causes paned windows to move, but it's GTK code. So we have to manually reset paned positions here.
                sheetWidget.ShowAll();
                sheet.Refresh();

                if (parentPane != null)
                    parentPane.Position = panedPos;
                if (sheetWidget.Parent.Parent is Paned)
                    (sheetWidget.Parent.Parent as Paned).Position = panedPos;
                //////////////////////////////////////
                
                entry.GrabFocus();
                if (defaultChar != char.MinValue)
                {
                    entry.SelectRegion(1, 0);
                    entry.Position = 1;
                }
            }

        }

        /// <summary>End edit mode.</summary>
        public void EndEdit(bool saveEdit = true)
        {
            if (entry != null)
            {
                if (saveEdit)
                {
                    sheet.CellSelector.GetSelection(out int selectedColumnIndex, out int selectedRowIndex);
                    sheet.DataProvider.SetCellContents(new int[]{selectedColumnIndex}, 
                                                        new int[]{selectedRowIndex}, 
                                                        new string[]{entry.Text});
                    sheet.CalculateBounds(selectedColumnIndex, selectedRowIndex);
                }

                entry.KeyPressEvent -= OnEntryKeyPress;
                fix.Remove(entry);
                entry = null;
                sheet.Refresh();
                sheet.RecalculateColumnWidths();
                sheetWidget.GrabFocus();
            }
        }

        /// <summary>Invoked when the user types in the entry box.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnEntryKeyPress(object sender, KeyPressEventArgs args)
        {
            if (args.Event.Key == Gdk.Key.Escape)
            {
                EndEdit(saveEdit: false);
            }
            else
            {
                // It's important to call ToSheetEventKey() here, as that function
                // contains logic for detecting unusual/non-standard return keys.
                SheetEventKey key = args.Event.ToSheetEventKey();
                if (key.Key == Keys.Return || key.Key == Keys.Tab)
                {
                    EndEdit();
                    if (key.Key == Keys.Return)
                        sheet.CellSelector.MoveDown(key.Shift);
                    else if (key.Key == Keys.Tab)
                    {
                        // User has clicked tab to finish editing the cell. If this
                        // row contains more cells to the right of the current cell
                        // then move the selection right. Otherwise, move the
                        // selection down.
                        sheet.CellSelector.GetSelection(out int col, out _);
                        if ( (col + 1) < sheet.DataProvider.ColumnCount)
                            sheet.CellSelector.MoveRight(key.Shift);
                        else
                            sheet.CellSelector.MoveDown(key.Shift);
                    }
                }
                // if (key.Key == Keys.Period)
                // {
                //     NeedContextItemsArgs contextArgs = new NeedContextItemsArgs()
                //     {
                //         Coordinates = GetPositionOfCursor(),
                //         Code = entry.Text,
                //         Offset = 0,
                //         ControlSpace = false,
                //         ControlShiftSpace = false,
                //         LineNo = 0,
                //         ColNo = 0
                //     };

                //     ShowIntellisense?.Invoke(sender, contextArgs);
                // }
            }
        }

        /// <summary>
        /// Gets the location (in screen coordinates) of the cursor.
        /// </summary>
        /// <returns>Tuple, where item 1 is the x-coordinate and item 2 is the y-coordinate.</returns>
        public System.Drawing.Point GetPositionOfCursor()
        {
            if (entry == null)
                return new System.Drawing.Point(0, 0);

            // Get the location of the cursor. This rectangle's x and y properties will be
            // the current line and column number.
            Gdk.Rectangle location = entry.Allocation;

            // Now, convert these coordinates to be relative to the GtkWindow's origin.
            entry.TranslateCoordinates(entry.Toplevel, location.X, location.Y, out int windowX, out int windowY);

            // Don't forget to account for the offset of the window within the screen.
            // (Remember that the screen is made up of multiple monitors, so this is
            // what accounts for which particular monitor the on which the window is
            // physically displayed.)
            Widget win = entry;
            while(win.Parent != null)
                win = win.Parent;

            win.Window.GetOrigin(out int frameX, out int frameY);

            return new System.Drawing.Point(frameX + windowX, frameY + windowY);
        }
    }
}