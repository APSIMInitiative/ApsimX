using APSIM.Shared.Utilities;
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
    public class SheetWidget : EventBox
    {
        private Sheet _sheet;

        /// <summary>The width of the grid lines in pixels.</summary>
        private const double lineWidth = 0.2;

        /// <summary>The number of rows to scroll (up or down) on mouse wheel.</summary>
        private const int mouseWheelScrollRows = 10;

        /// <summary>Constructor</summary>
        public SheetWidget()
        {
            CanFocus = true;
            AddEvents((int)EventMask.ScrollMask);
        }

        /// <summary>The instance that contains the look and behaviour of the widget.</summary>
        public Sheet Sheet
        {
            get => _sheet;
            set
            {
                _sheet = value;
                _sheet.RedrawNeeded += OnRedrawNeeded;
                this.StyleContext.AddClass("sheet");
            }
        }

        public void SetClipboard(string text)
        {
            var clipboardName = "CLIPBOARD";
            Gdk.Atom modelClipboard = Gdk.Atom.Intern(clipboardName, false);
            Clipboard cb = Clipboard.Get(modelClipboard);
            cb.Text = text;
        }

        public string GetClipboard()
        {
            var clipboardName = "CLIPBOARD";
            Gdk.Atom modelClipboard = Gdk.Atom.Intern(clipboardName, false);
            Clipboard cb = Clipboard.Get(modelClipboard);
            return cb.WaitForText();
        }

        private void OnRedrawNeeded(object sender, EventArgs e)
        {
            QueueDraw();
        }


        /// <summary>Called by base class to draw the sheet widget.</summary>
        /// <param name="cr">The context to draw in.</param>
        protected override bool OnDrawn(Context cr)
        {
            try
            {
                // Do initialisation
                if (Sheet.ColumnWidths == null)
                    Initialise(cr);

                base.OnDrawn(cr);

                Sheet.Draw(new CairoContext(cr, this));

            }
            catch (Exception err)
            {
                ViewBase.MasterView.ShowError(err);
            }

            return true;
        }


        /// <summary>Initialise the widget.</summary>
        /// <param name="cr">The drawing context.</param>
        private void Initialise(Context cr)
        {

            Sheet.Width = this.AllocatedWidth;
            Sheet.Height = this.AllocatedHeight;

            if (cr != null)
                Sheet.Initialise(new CairoContext(cr, this));

        }

        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            try
            {
                base.OnSizeAllocated(allocation);

                Sheet.Resize(allocation.Width, allocation.Height);
            }
            catch (Exception err)
            {
                ViewBase.MasterView.ShowError(err);
            }
        }

        /// <summary>Invoked when the user presses a key.</summary>
        /// <param name="evnt">The event data.</param>
        /// <returns>True if event has handled.</returns>
        protected override bool OnKeyPressEvent(EventKey evnt)
        {
            try
            {
                SheetEventKey keyParams = evnt.ToSheetEventKey();
                Sheet.InvokeKeyPress(keyParams);
            }
            catch (Exception ex)
            {
                MainView.MasterView.ShowError(ex);
            }
            return true;
        }

        /// <summary>Invoked when the user clicks a mouse button.</summary>
        /// <param name="evnt">The event data.</param>
        /// <returns></returns>
        protected override bool OnButtonPressEvent(EventButton evnt)
        {
            try
            {
                if (evnt.Type == EventType.ButtonPress)
                {
                    SheetEventButton buttonParams = evnt.ToSheetEventButton();
                    Sheet.InvokeButtonPress(buttonParams);
                }
            }
            catch (Exception ex)
            {
                MainView.MasterView.ShowError(ex);
            }

            return true;
        }

        protected override bool OnScrollEvent(EventScroll e)
        {
            try
            {
                int delta;

                if (e.Direction == Gdk.ScrollDirection.Smooth)
                    delta = e.DeltaY < 0 ? mouseWheelScrollRows : -mouseWheelScrollRows;
                else
                    delta = e.Direction == Gdk.ScrollDirection.Down ? -mouseWheelScrollRows : mouseWheelScrollRows;

                Sheet.InvokeScroll(delta);
            }
            catch (Exception err)
            {
                ViewBase.MasterView.ShowError(err);
            }
            return true;
        }
    }
}