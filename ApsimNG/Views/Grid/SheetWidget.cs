using Cairo;
using Gdk;
using Gtk;
using System;

namespace UserInterface.Views
{
    /// <summary>Gtk grid class that offers similar functionality to an Excel sheet.</summary>
    class SheetWidget : EventBox
	{
        private Entry entry;
        private Fixed fix;
        private HScrollbar horizontalScrollbar;
        private VScrollbar verticalScrollbar;
        private SheetEngine view;
        private const int scrollBarWidth = 20;
        int windowWidth;
        int windowHeight;

        /// <summary>Constructor</summary>
        public SheetWidget()
        {
            CanFocus = true;
        }

        private void OnHorizontalScrollbarChanged(object sender, EventArgs e)
        {
            if (view.NumberColumnsScrolled != horizontalScrollbar.Value)
            {
                view.ScrollHorizontalTo(horizontalScrollbar.Value);
                QueueDraw();
            }
        }

        private void OnVerticalScrollbarChanged(object sender, EventArgs e)
        {
            view.ScrollVerticalTo(verticalScrollbar.Value);
            QueueDraw();
        }

        /// <summary>The provider the grid will use to get and set data.</summary>
        public ISheetDataProvider DataProvider { get; set; }

        /// <summary>The number of rows at the top of the grid that are headings.</summary>
        public int NumHeadingRows { get; set; } = 1;

        /// <summary>The number of frozen columns at the left of the grid that won't be scrolled.</summary>
        public int NumFrozenColumns { get; set; } = 0;

        /// <summary>Is the grid readonly?.</summary>
        public bool Readonly { get; set; } = true;

        /// <summary>Does the grid have its text left justified? If false, text will be right justified.</summary>
        public bool LeftJustify { get; set; } = false;  // default to right justify.

#if NETFRAMEWORK
        /// <summary>Called by base class to draw the grid widget.</summary>
        /// <param name="expose">The context to draw in.</param>
        protected override bool OnExposeEvent(EventExpose expose)
        {
            // Do initialisation
            if (view == null)
                InitialiseEngine();

            base.OnExposeEvent(expose);

            Context cr = CairoHelper.Create(expose.Window);
            view.Draw(cr);
            ((IDisposable)cr.Target).Dispose();
            ((IDisposable)cr).Dispose();

            if (horizontalScrollbar == null)
                AddScollBars();
            return true;
        }
#else
        /// <summary>Called by base class to draw the grid widget.</summary>
        /// <param name="cr">The context to draw in.</param>
        protected override bool OnDrawn(Context cr)
        {
            // Do initialisation
            if (view == null)
                InitialiseEngine();

            base.OnDrawn(cr);

            view.Draw(cr);
           
            if (horizontalScrollbar == null)
                AddScollBars();

            return true;
        }
#endif


        /// <summary>Invoked when the user presses a key.</summary>
        /// <param name="evnt">The event data.</param>
        /// <returns>True if event has handled.</returns>
        protected override bool OnKeyPressEvent(EventKey evnt)
        {
            if (view.SelectionBeingEdited)
                return base.OnKeyPressEvent(evnt);
            else
            {
                if (evnt.Key == Gdk.Key.Left)
                    view.Selected.MoveLeft();
                else if (evnt.Key == Gdk.Key.Right)
                    view.Selected.MoveRight();
                else if (evnt.Key == Gdk.Key.Down)
                    view.Selected.MoveDown();
                else if (evnt.Key == Gdk.Key.Up)
                    view.Selected.MoveUp();
                else if (evnt.Key == Gdk.Key.Return)
                    ShowEntryBox();

                QueueDraw();
                return true;
            }
        }

        /// <summary>Invoked when the user clicks a mouse button.</summary>
        /// <param name="evnt">The event data.</param>
        /// <returns></returns>
        protected override bool OnButtonPressEvent(EventButton evnt)
        {
            if (evnt.Type == EventType.ButtonPress)
            {
                if (view.Select(new Cairo.Point((int)evnt.X, (int)evnt.Y)))
                {
                    GrabFocus();
                    ShowEntryBox();
                    QueueDraw();
                }
                return true;
            }
            return base.OnButtonPressEvent(evnt);
        }

        /// <summary>Initialise the widget.</summary>
        private void InitialiseEngine()
        {
#if NETFRAMEWORK
            windowWidth = Parent.Allocation.Width;
            windowHeight = Parent.Allocation.Height;
#else
            windowWidth = Parent.AllocatedWidth;
            windowHeight = Parent.AllocatedHeight;
#endif
            view = new SheetEngine(windowWidth - scrollBarWidth, windowHeight, NumHeadingRows, NumFrozenColumns,
                                   LeftJustify,
                                   DataProvider);
            view.HorizontallyScrolled += OnHorizontallyScrolled;
        }

        private void OnHorizontallyScrolled(object sender, EventArgs e)
        {
            if (horizontalScrollbar.Value != view.NumberColumnsScrolled)
                horizontalScrollbar.Value = view.NumberColumnsScrolled;
        }

        private void AddScollBars()
        { 
            var horizontalAdjustment = new Adjustment(1, 0, view.MaxColumnsToScroll + 1, 1, 1, 1);
            horizontalScrollbar = new HScrollbar(horizontalAdjustment);
            horizontalScrollbar.Value = 0;
            horizontalScrollbar.ValueChanged += OnHorizontalScrollbarChanged;
            horizontalScrollbar.SetSizeRequest(windowWidth, scrollBarWidth);

            var verticalAdjustment = new Adjustment(1, 0, view.MaxRowsToScroll + 1, 1, 1, 1);
            verticalScrollbar = new VScrollbar(verticalAdjustment);
            verticalScrollbar.Value = 0;
            verticalScrollbar.ValueChanged += OnVerticalScrollbarChanged;
            verticalScrollbar.SetSizeRequest(scrollBarWidth, windowHeight);

            fix = new Fixed();
            fix.Put(horizontalScrollbar, 0, windowHeight);
            fix.Put(verticalScrollbar, windowWidth- scrollBarWidth, 0);

            Add(fix);
            fix.ShowAll();
        }

        /// <summary>Update the position of the scroll bars to match the view.</summary>
        private void UpdateScrollBars()
        {
            if (horizontalScrollbar.Value != view.NumberColumnsScrolled)
            {
                // Disconnect the ValueChanged event handler from the scroll bar
                // before we change the value, otherwise we will trigger the 
                // event which isn't wanted.
                horizontalScrollbar.ValueChanged -= OnHorizontalScrollbarChanged;
                horizontalScrollbar.Value = view.NumberColumnsScrolled;
                horizontalScrollbar.ValueChanged += OnHorizontalScrollbarChanged;
            }
            if (verticalScrollbar.Value != view.NumberRowsScrolled)
            {
                // Disconnect the ValueChanged event handler from the scroll bar
                // before we change the value, otherwise we will trigger the 
                // event which isn't wanted.
                verticalScrollbar.ValueChanged -= OnVerticalScrollbarChanged;
                verticalScrollbar.Value = view.NumberRowsScrolled;
                verticalScrollbar.ValueChanged -= OnVerticalScrollbarChanged;
            }
        }

        /// <summary>Display an entry box for the user to edit cell data.</summary>
        private void ShowEntryBox()
        {
            if (!Readonly)
            {
                view.SelectionBeingEdited = true;
                var cellBounds = view.Selected.Bounds();

                entry = new Entry();
                entry.SetSizeRequest((int)cellBounds.Width - 3, (int)cellBounds.Height - 10);
                entry.WidthChars = 5;
                entry.Text = view.Selected.Text;
                entry.KeyPressEvent += OnEntryKeyPress;
                if (!LeftJustify)
                    entry.Alignment = 1; // right

                fix.Put(entry, (int)cellBounds.X + 1, (int)cellBounds.Y + 1);
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
        }
    }
}