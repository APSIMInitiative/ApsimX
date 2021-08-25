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
        /// <summary>The instance that contains the look and behaviour of the widget.</summary>
        private readonly Sheet sheet;

        /// <summary>The width of the grid lines in pixels.</summary>
        private const double lineWidth = 0.2;

        /// <summary>The number of rows to scroll (up or down) on mouse wheel.</summary>
        private const int mouseWheelScrollRows = 10;

        /// <summary>Constructor</summary>
        /// <param name="sheetEngine">The instance that contains the look and behaviour of the widget.</param>
        public SheetWidget(Sheet sheetEngine)
        {
            CanFocus = true;
            sheet = sheetEngine;
            sheet.RedrawNeeded += OnRedrawNeeded;
#if NETCOREAPP
            this.StyleContext.AddClass("sheet");
#endif
            AddEvents((int)EventMask.ScrollMask);
        }

        private void OnRedrawNeeded(object sender, EventArgs e)
        {
            QueueDraw();
        }

#if NETFRAMEWORK
        /// <summary>Called by base class to draw the sheet widget.</summary>
        /// <param name="expose">The expose event arguments.</param>
        protected override bool OnExposeEvent(EventExpose expose)
        {
            try
            {
                Context cr = CairoHelper.Create(expose.Window);

                // Do initialisation
                if (sheet.ColumnWidths == null)
                    Initialise(cr);

                base.OnExposeEvent(expose);

                sheet.Draw(new CairoContext(cr, this));

                ((IDisposable)cr.Target).Dispose();
                ((IDisposable)cr).Dispose();
            }
            catch (Exception err)
            {
                ViewBase.MasterView.ShowError(err);
            }

            return true;
        }
#else
        /// <summary>Called by base class to draw the sheet widget.</summary>
        /// <param name="cr">The context to draw in.</param>
        protected override bool OnDrawn(Context cr)
        {
            try
            {
                // Do initialisation
                if (sheet.ColumnWidths == null)
                    Initialise(cr);

                base.OnDrawn(cr);

                sheet.Draw(new CairoContext(cr, this));

            }
            catch (Exception err)
            {
                ViewBase.MasterView.ShowError(err);
            }

            return true;
        }
#endif

        /// <summary>Initialise the widget.</summary>
        /// <param name="cr">The drawing context.</param>
        private void Initialise(Context cr)
        {
#if NETFRAMEWORK
            sheet.Width = Parent.Allocation.Width;
            sheet.Height = Parent.Allocation.Height;
#else
            sheet.Width = this.AllocatedWidth;
            sheet.Height = this.AllocatedHeight;
#endif
            if (cr != null)
                sheet.Initialise(new CairoContext(cr, this));

            GrabFocus();
        }

        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            try
            {
                base.OnSizeAllocated(allocation);

                sheet.Resize(allocation.Width, allocation.Height);
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
                sheet.InvokeKeyPress(keyParams);
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
                    sheet.InvokeButtonPress(buttonParams);
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
#if NETFRAMEWORK
                delta = e.Direction == Gdk.ScrollDirection.Down ? -120 : 120;
#else
                if (e.Direction == Gdk.ScrollDirection.Smooth)
                    delta = e.DeltaY < 0 ? mouseWheelScrollRows : -mouseWheelScrollRows;
                else
                    delta = e.Direction == Gdk.ScrollDirection.Down ? -mouseWheelScrollRows : mouseWheelScrollRows;
#endif
                sheet.InvokeScroll(delta);
            }
            catch (Exception err)
            {
                ViewBase.MasterView.ShowError(err);
            }
            return true;
        }
    }
}