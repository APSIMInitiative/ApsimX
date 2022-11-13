using Gtk;
using System;

namespace UserInterface.Views
{
    /// <summary>Implements scroll bars for a sheet widget.</summary>
    public class SheetScrollBars
    {
        /// <summary>The sheet widget.</summary>
        private Sheet sheet;

        /// <summary>The sheet widget.</summary>
        private SheetWidget sheetWidget;

        /// <summary>The horizontal scroll bar</summary>
        private HScrollbar horizontalScrollbar;

        /// <summary>The vertical scroll bar</summary>
        private VScrollbar verticalScrollbar;

        /// <summary>Constructor.</summary>
        /// <param name="sheet">The sheet.</param>
        /// <param name="sheetWidget">The sheet widget.</param>
        public SheetScrollBars(Sheet sheet, SheetWidget sheetWidget)
        {
            this.sheet = sheet;
            this.sheetWidget = sheetWidget;
            sheet.Initialised += OnSheetInitialised;
            sheet.ScrolledHorizontally += OnSheetScrolled;
            sheet.ScrolledVertically += OnSheetScrolled;
            Initialise();
        }

        /// <summary>A container for the sheet and scroll bars.</summary>
        public Widget MainWidget { get; private set; }

        /// <summary>The width in pixels of a scroll bar.</summary>
        public int ScrollBarWidth { get; set; } = 20;

        /// <summary>Clean up the instance.</summary>
        public void Cleanup()
        {
            sheet.Initialised -= OnSheetInitialised;
            sheet.ScrolledHorizontally -= OnSheetScrolled;
            sheet.ScrolledVertically -= OnSheetScrolled;
        }

        /// <summary>Invoked when the sheet widget has initialised.</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSheetInitialised(object sender, EventArgs e)
        {
            SetScrollbarAdjustments();
            //sheet.Width -= ScrollBarWidth;
            //AddScollBars();
            //sheet.Refresh();
        }

        /// <summary>A scroll bars to the sheet widget.</summary>
        private void Initialise()
        {
            //// Remove existing child.
            //if (sheet.Children.Length > 0)
            //    sheet.Remove(sheet.Children[0]);

            var horizontalAdjustment = new Adjustment(1, 0, 100, 1, 1, 1);
            horizontalScrollbar = new HScrollbar(horizontalAdjustment);
            horizontalScrollbar.Value = 0;
            horizontalScrollbar.ValueChanged += OnHorizontalScrollbarChanged;
            //horizontalScrollbar.SetSizeRequest(sheet.Width, ScrollBarWidth);

            var verticalAdjustment = new Adjustment(1, 0, 100, 1, 1, 1);
            verticalScrollbar = new VScrollbar(verticalAdjustment);
            verticalScrollbar.Value = 0;
            verticalScrollbar.ValueChanged += OnVerticalScrollbarChanged;
            //verticalScrollbar.SetSizeRequest(ScrollBarWidth, sheet.Height);

            var hbox = new HBox();
            var vbox = new VBox();

            hbox.PackEnd(verticalScrollbar, false, true, 0);
            hbox.PackStart(sheetWidget, true, true, 0);

            vbox.PackEnd(horizontalScrollbar, false, true, 0);
            vbox.PackStart(hbox, true, true, 0);

            MainWidget = vbox;
        }

        private void SetScrollbarAdjustments()
        {
            horizontalScrollbar.Adjustment = new Adjustment(1, 0, sheet.MaximumNumberHiddenColumns + 1, 1, 1, 1);
            verticalScrollbar.Adjustment = new Adjustment(1, 0, sheet.MaximumNumberHiddenRows + 1, 1, 1, 1);
            OnSheetScrolled(this, null);
        }

        /// <summary>Invoked when the sheet has been scrolled.</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="args">The event arguments.</param>
        private void OnSheetScrolled(object sender, EventArgs args)
        {
            if (horizontalScrollbar != null)
            {
                if (horizontalScrollbar.Value != sheet.NumberHiddenColumns)
                {
                    // Disconnect the ValueChanged event handler from the scroll bar
                    // before we change the value, otherwise we will trigger the 
                    // event which isn't wanted.
                    horizontalScrollbar.ValueChanged -= OnHorizontalScrollbarChanged;
                    horizontalScrollbar.Value = sheet.NumberHiddenColumns;
                    horizontalScrollbar.ValueChanged += OnHorizontalScrollbarChanged;
                }
                if (verticalScrollbar.Value != sheet.NumberHiddenRows)
                {
                    // Disconnect the ValueChanged event handler from the scroll bar
                    // before we change the value, otherwise we will trigger the 
                    // event which isn't wanted.
                    verticalScrollbar.ValueChanged -= OnVerticalScrollbarChanged;
                    verticalScrollbar.Value = sheet.NumberHiddenRows;
                    verticalScrollbar.ValueChanged += OnVerticalScrollbarChanged;
                }
            }
        }

        /// <summary>Invoked when the user has moved the horizontal scroll bar.</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments.</param>
        private void OnHorizontalScrollbarChanged(object sender, EventArgs e)
        {
            if (sheet.NumberHiddenColumns != horizontalScrollbar.Value)
            {
                sheet.NumberHiddenColumns = (int)horizontalScrollbar.Value;
                sheet.Refresh();
            }
        }

        /// <summary>Invoked when the user has moved the vertical scroll bar.</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments.</param>
        private void OnVerticalScrollbarChanged(object sender, EventArgs e)
        {
            if (sheet.NumberHiddenRows != verticalScrollbar.Value)
            {
                sheet.NumberHiddenRows = (int)verticalScrollbar.Value;
                sheet.Refresh();
            }
        }
    }
}