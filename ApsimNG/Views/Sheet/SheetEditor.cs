using Gdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.Views
{
    class SheetEditor : ISheetEditor
    {
        //private Entry entry;
        //private Fixed fix;

        public bool IsEditing => false;


        /// <summary>Invoked when the user presses a key.</summary>
        /// <param name="sender"></param>
        /// <param name="evnt">The event data.</param>
        /// <returns>True if event has handled.</returns>
        private void OnKeyPressEvent(object sender, EventKey evnt)
        {
                //if (evnt.Key == Gdk.Key.Return)
                //    ShowEntryBox();
        }
        /*
                /// <summary>Display an entry box for the user to edit cell data.</summary>
                private void ShowEntryBox()
                {
                    if (!Readonly)
                    {
                        var cellBounds = view.CalculateBounds(selectedColumnIndex, selectedRowIndex);

                        entry = new Entry();
                        entry.SetSizeRequest((int)cellBounds.Width - 3, (int)cellBounds.Height - 10);
                        entry.WidthChars = 5;
                        entry.Text = DataProvider.GetCellContents(selectedColumnIndex, selectedRowIndex);
                        entry.KeyPressEvent += OnEntryKeyPress;
                        if (!LeftJustify)
                            entry.Alignment = 1; // right

                        fix.Put(entry, (int)cellBounds.Left + 1, (int)cellBounds.Top + 1);
                        fix.ShowAll();

                        entry.GrabFocus();
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
                        entry.KeyPressEvent -= OnEntryKeyPress;
                        fix.Remove(entry);
                        view.SelectionBeingEdited = false;
                        QueueDraw();
                    }
                    else if (args.Event.Key == Gdk.Key.Return)
                    {
                        view.Selected.Text = entry.Text;
                        entry.KeyPressEvent -= OnEntryKeyPress;
                        fix.Remove(entry);
                        view.SelectionBeingEdited = false;
                        QueueDraw();
                    }
                }*/
    }
}
