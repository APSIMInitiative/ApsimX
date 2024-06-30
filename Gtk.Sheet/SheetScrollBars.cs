using Gtk;
using System;

namespace Gtk.Sheet
{
    /// <summary>Implements scroll bars for a sheet widget.</summary>
    internal class SheetScrollBars
    {
        /// <summary>The sheet widget.</summary>
        private Sheet sheet;

        /// <summary>The sheet widget.</summary>
        private SheetWidget sheetWidget;

        /// <summary>The horizontal scroll bar</summary>
        private HScrollbar horizontalScrollbar;

        /// <summary>The vertical scroll bar</summary>
        private VScrollbar verticalScrollbar;

        /// <summary>The vertical scroll bar</summary>
        private VBox horizontalScrollbarBox;

        /// <summary>The horizontal scroll bar</summary>
        private HBox verticalScrollbarBox;

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
            SetScrollbarAdjustments(sheet.MaximumNumberHiddenColumns, sheet.MaximumNumberHiddenRows);
        }

        /// <summary>A scroll bars to the sheet widget.</summary>
        private void Initialise()
        {
            var horizontalAdjustment = new Adjustment(1, 0, 100, 1, 1, 1);
            horizontalScrollbar = new HScrollbar(horizontalAdjustment);
            horizontalScrollbar.Value = 0;
            horizontalScrollbar.ValueChanged += OnHorizontalScrollbarChanged;

            var verticalAdjustment = new Adjustment(1, 0, 100, 1, 1, 1);
            verticalScrollbar = new VScrollbar(verticalAdjustment);
            verticalScrollbar.Value = 0;
            verticalScrollbar.ValueChanged += OnVerticalScrollbarChanged;

            verticalScrollbarBox = new HBox();
            horizontalScrollbarBox = new VBox();

            verticalScrollbarBox.PackStart(sheetWidget, true, true, 0);
            verticalScrollbarBox.PackEnd(verticalScrollbar, false, true, 0);

            horizontalScrollbarBox.PackStart(verticalScrollbarBox, true, true, 0);
            horizontalScrollbarBox.PackEnd(horizontalScrollbar, false, true, 0);

            MainWidget = horizontalScrollbarBox;
        }

        public void SetScrollbarAdjustments(int columns, int rows)
        {
            horizontalScrollbar.Adjustment.Upper = columns + 1;
            horizontalScrollbar.Adjustment.Lower = 0;
            verticalScrollbar.Adjustment.Upper = rows + 2;
            verticalScrollbar.Adjustment.Lower = 0;
        }

        /// <summary>Set the visibility of the horizontal scroll bar.</summary>
        /// <param name="visible">Is the scroll bar visible?</param>
        public void SetHorizontalScrollbarVisibility(bool visible)
        {
            horizontalScrollbar.Visible = visible;
        }        

        /// <summary>Set the visibility of the vertical scroll bar.</summary>
        /// <param name="visible">Is the scroll bar visible?</param>
        public void SetVerticalScrollbarVisibility(bool visible)
        {
            verticalScrollbar.Visible = visible;
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
                    sheet.RecalculateColumnWidths();
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
                sheet.RecalculateColumnWidths();
            }
        }
    }
}