using Gdk;
using Gtk;

namespace UserInterface.Views
{
    class SheetEditor : ISheetEditor
    {
        /// <summary>The sheet widget.</summary>
        private readonly SheetWidget sheet;

        /// <summary>The gtk entry box used when editing a sheet cell.</summary>
        private Entry entry;

        /// <summary>The gtk fixed positioning container for the entry box used when editing a sheet cell.</summary>
        private Fixed fix = new Fixed();

        /// <summary>Constructor.</summary>
        /// <param name="sheetView">The sheet widget.</param>
        public SheetEditor(SheetWidget sheetView)
        {
            sheet = sheetView;
            sheet.KeyPress += OnKeyPressEvent;
        }

        /// <summary>The cell selection instance.</summary>
        public ISheetSelection Selection { get; set; }

        public bool IsEditing => entry != null;

        /// <summary>Cleanup the instance.</summary>
        private void Cleanup()
        {
            sheet.KeyPress -= OnKeyPressEvent;
        }

        /// <summary>Invoked when the user presses a key.</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="evnt">The event data.</param>
        private void OnKeyPressEvent(object sender, EventKey evnt)
        {
            if (!IsEditing && evnt.Key == Gdk.Key.Return)
                ShowEntryBox();
        }
        
        /// <summary>Display an entry box for the user to edit cell data.</summary>
        private void ShowEntryBox()
        {
            Selection.GetSelection(out int selectedColumnIndex, out int selectedRowIndex);
            var cellBounds = sheet.CalculateBounds(selectedColumnIndex, selectedRowIndex);

            entry = new Entry();
            entry.SetSizeRequest((int)cellBounds.Width - 3, (int)cellBounds.Height - 10);
            entry.WidthChars = 5;
            entry.Text = sheet.DataProvider.GetCellContents(selectedColumnIndex, selectedRowIndex);
            entry.KeyPressEvent += OnEntryKeyPress;
            if (!sheet.CellPainter.TextLeftJustify(selectedColumnIndex, selectedRowIndex))
                entry.Alignment = 1; // right

            if (sheet.Children.Length == 1)
            {
                fix = (Fixed)sheet.Child;
            }
            else
            {
                fix = new Fixed();
                sheet.Add(fix);
            }
            fix.Put(entry, (int)cellBounds.Left + 1, (int)cellBounds.Top + 1);
            
            sheet.ShowAll();
            sheet.Refresh();

            entry.GrabFocus();
        }

        /// <summary>Invoked when the user types in the entry box.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnEntryKeyPress(object sender, KeyPressEventArgs args)
        {
            if (args.Event.Key == Gdk.Key.Escape)
            {
                entry.KeyPressEvent -= OnEntryKeyPress;
                fix.Remove(entry);
                entry = null;
                sheet.Refresh();
                sheet.GrabFocus();
            }
            else if (args.Event.Key == Gdk.Key.Return)
            {
                Selection.GetSelection(out int selectedColumnIndex, out int selectedRowIndex);
                sheet.DataProvider.SetCellContents(selectedColumnIndex, selectedRowIndex, entry.Text);

                entry.KeyPressEvent -= OnEntryKeyPress;
                fix.Remove(entry);
                entry = null;

                sheet.Refresh();
                sheet.GrabFocus();
            }
        }
    }
}