using Gtk;
using System;

namespace UserInterface.Views
{
    public class SheetScrollBars
    {
        private SheetView sheet;
        private Fixed fix;
        private HScrollbar horizontalScrollbar;
        private VScrollbar verticalScrollbar;

        public SheetScrollBars(SheetView sheetView)
        {
            sheet = sheetView;
            sheet.Initialised += OnInitialising;
            sheet.ScrolledHorizontally += OnViewScrolled;
            sheet.ScrolledVertically += OnViewScrolled;
        }

        private void OnInitialising(object sender, EventArgs e)
        {
            sheet.Width -= ScrollBarWidth;
            AddScollBars();
            sheet.Refresh();
        }

        public int ScrollBarWidth { get; set; } = 20;

        private void AddScollBars()
        {
            var horizontalAdjustment = new Adjustment(1, 0, sheet.MaximumNumberHiddenColumns + 1, 1, 1, 1);
            horizontalScrollbar = new HScrollbar(horizontalAdjustment);
            horizontalScrollbar.Value = 0;
            horizontalScrollbar.ValueChanged += OnHorizontalScrollbarChanged;
            horizontalScrollbar.SetSizeRequest(sheet.Width, ScrollBarWidth);

            var verticalAdjustment = new Adjustment(1, 0, sheet.MaximumNumberHiddenRows + 1, 1, 1, 1);
            verticalScrollbar = new VScrollbar(verticalAdjustment);
            verticalScrollbar.Value = 0;
            verticalScrollbar.ValueChanged += OnVerticalScrollbarChanged;
            verticalScrollbar.SetSizeRequest(ScrollBarWidth, sheet.Height);

            fix = new Fixed();
            fix.Put(horizontalScrollbar, 0, sheet.Height);
            fix.Put(verticalScrollbar, sheet.Width/* - ScrollBarWidth*/, 0);

            sheet.Add(fix);
            sheet.ShowAll();
        }

        /// <summary>Update the position of the scroll bars to match the view.</summary>
        private void OnViewScrolled(object sender, EventArgs args)
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

        private void OnHorizontalScrollbarChanged(object sender, EventArgs e)
        {
            if (sheet.NumberHiddenColumns != horizontalScrollbar.Value)
            {
                sheet.NumberHiddenColumns = (int)horizontalScrollbar.Value;
                sheet.Refresh();
            }
        }

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