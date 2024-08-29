using Cairo;
using Gdk;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnitTests")]
namespace Gtk.Sheet
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

        private readonly Action<Exception> onException;

        /// <summary>The number of rows to scroll (up or down) on mouse wheel.</summary>
        private const int mouseWheelScrollRows = 10;

        /// <summary>Constructor</summary>
        public SheetWidget(Container container,
                           IDataProvider dataProvider,
                           bool multiSelect,
                           int numberFrozenColumns = 0,
                           int numberFrozenRows = -1,
                           bool blankRowAtBottom = false,
                           int[] columnWidths = null,
                           bool gridIsEditable = true,
                           Action<Exception> onException = null)
        {
            this.onException = onException;
            CanFocus = true;

            Sheet = new Sheet(dataProvider,
                              numberFrozenColumns,
                              numberFrozenRows,
                              columnWidths,
                              blankRowAtBottom);

            if (multiSelect)
                Sheet.CellSelector = new MultiCellSelect(Sheet);
            else
                Sheet.CellSelector = new SingleCellSelect(Sheet);

            SetDataProvider(dataProvider);

            Sheet.ScrollBars = new SheetScrollBars(Sheet, this);
            if (gridIsEditable)
                Sheet.CellPainter = new DefaultCellPainter(Sheet, this);
            else
                Sheet.CellPainter = new CellPainterNoCellStates(Sheet, this);
            if (container != null)
                AddChild(container, Sheet.ScrollBars.MainWidget);

            AddEvents((int)EventMask.ScrollMask);
            Sheet.RedrawNeeded += (sender, e) => UpdateScrollBars();
        }

        /// <summary>The number of rows in the grid.</summary>
        public int RowCount => Sheet.RowCount;

        /// <summary>The number of rows that are frozen (can not be scrolled).</summary>
        public int NumberFrozenRows => Sheet.NumberFrozenRows;

        /// <summary>
        /// Set a new data provider for the sheet.
        /// </summary>
        /// <param name="dataProvider">The data provider.</param>
        public void SetDataProvider(IDataProvider dataProvider)
        {
            Sheet.SetDataProvider(dataProvider);
            if (dataProvider != null)
            {
                if (Sheet.CellEditor == null)
                    Sheet.CellEditor = new CellEditor(Sheet, this);
            }
        }

        /// <summary>The instance that contains the look and behaviour of the widget.</summary>
        private Sheet Sheet
        {
            get => _sheet;
            set
            {
                _sheet = value;
                _sheet.RedrawNeeded += OnRedrawNeeded;
                _sheet.OnException = onException;
                this.StyleContext.AddClass("sheet");
            }
        }

        public void SetClipboard(string text)
        {
            var clipboardName = "CLIPBOARD";
            Gdk.Atom modelClipboard = Gdk.Atom.Intern(clipboardName, false);
            Clipboard cb = Clipboard.Get(modelClipboard);
            if (text != null)
            {
                cb.Text = text;
            }
        }

        public string GetClipboard()
        {
            var clipboardName = "CLIPBOARD";
            Gdk.Atom modelClipboard = Gdk.Atom.Intern(clipboardName, false);
            Clipboard cb = Clipboard.Get(modelClipboard);
            return cb.WaitForText();
        }

        /// <summary>Return true if a xy pixel coordinates are in a specified cell.</summary>
        /// <param name="pixelX">X pixel coordinate.</param>
        /// <param name="pixelY">Y pixel coordinate.</param>
        public bool CellHitTest(int pixelX, int pixelY, out int columnIndex, out int rowIndex)
        {
            return Sheet.CellHitTest(pixelX, pixelY, out columnIndex, out rowIndex);
        }

        /// <summary>Calculates the bounds of a cell if it is visible (wholly or partially) to the user.</summary>
        /// <param name="columnIndex">The cell column index.</param>
        /// <param name="rowIndex">The cell row index.</param>
        /// <returns>The bounds if visible or null if not visible.</returns>
        public System.Drawing.Rectangle CalculateBounds(int columnIndex, int rowIndex)
        {
            return Sheet.CalculateBounds(columnIndex, rowIndex);
        }

        /// <summary>Return true if col/row coordinate denotes a readonly cell.</summary>
        /// <param name="columnIndex">Column index.</param>
        /// <param name="rowIndex">Row index.</param>
        public bool IsCellReadOnly(int columnIndex, int rowIndex)
        {
            return columnIndex < Sheet.NumberFrozenColumns || rowIndex < Sheet.NumberFrozenRows;
        }

        /// <summary>
        /// User has selected cut.
        /// </summary>
        public void Cut()
        {
            Copy();
            Sheet.CellSelector.Delete();
        }

        /// <summary>
        /// User has selected copy.
        /// </summary>
        public void Copy()
        {
            SetClipboard(Sheet.CellSelector.GetSelectedContents());
        }

        /// <summary>
        /// User has selected paste.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Paste()
        {
            Sheet.CellSelector.SetSelectedContents(GetClipboard());
        }

        /// <summary>
        /// User has selected cut.
        /// </summary>
        public void Delete()
        {
            Sheet.CellSelector.Delete();
        }

        /// <summary>
        /// User has selected cut.
        /// </summary>
        public void SelectAll()
        {
            Sheet.CellSelector.SelectAll();
        }

        /// <summary>Clean up the sheet components.</summary>
        public void Cleanup()
        {
            (Sheet.CellSelector as SingleCellSelect)?.Cleanup();
             Sheet.ScrollBars.Cleanup();
        }

        /// <summary>Scroll the sheet to the right one column.</summary>
        public void ScrollRight()
        {
            Sheet.ScrollRight();
        }

        /// <summary>Scroll the sheet to the left one column.</summary>
        public void ScrollLeft()
        {
            Sheet.ScrollLeft();
        }

        /// <summary>Scroll the sheet up one row.</summary>
        public void ScrollUp()
        {
            Sheet.ScrollUp();
        }

        /// <summary>Scroll the sheet down one row.</summary>
        public void ScrollDown()
        {
            Sheet.ScrollDown();
        }

        public void UpdateScrollBars()
        {
            int width = Sheet.Width;
            int column_widths = 0;
            if (Sheet.ColumnWidths != null && width > 0)
            {
                for (int i = 0; i < Sheet.ColumnWidths.Length; i++)
                    column_widths += Sheet.ColumnWidths[i];

                if (column_widths > width)
                    Sheet.ScrollBars.SetHorizontalScrollbarVisibility(true);
                else
                    Sheet.ScrollBars.SetHorizontalScrollbarVisibility(false);
            }
            else
                Sheet.ScrollBars.SetHorizontalScrollbarVisibility(false);

            int height = Sheet.Height;
            int row_heights = Sheet.RowHeight * (Sheet.RowCount + 1); //plus 1 for the empty row
            if (height > 0)
            {
                if (row_heights > height)
                    Sheet.ScrollBars.SetVerticalScrollbarVisibility(true);
                else
                    Sheet.ScrollBars.SetVerticalScrollbarVisibility(false);
            }
            else
                Sheet.ScrollBars.SetVerticalScrollbarVisibility(false);
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
                onException(err);
            }

            return true;
        }

        /// <summary>
        /// Add a GTK widget to a GTK container.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="child"></param>
        private static void AddChild(Container container, Widget child)
        {
            if (container.Children.Length > 0)
                container.Remove(container.Children[0]);
            if (container is Box box)
                box.PackStart(child, true, true, 0);
            else
                container.Add(child);
            container.ShowAll();
        }

        /// <summary>Initialise the widget.</summary>
        /// <param name="cr">The drawing context.</param>
        private void Initialise(Context cr)
        {

            Sheet.Width = this.AllocatedWidth;
            Sheet.Height = this.AllocatedHeight;
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
            onException(err);

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
                if (evnt.KeyValue > 0 && evnt.KeyValue < 255)
                {
                    if (evnt.KeyValue == 'c' && keyParams.Control)
                        Copy();
                    else if (evnt.KeyValue == 'v' && keyParams.Control)
                        Paste();
                    else if (Sheet.CellEditor != null)
                        Sheet.CellEditor.Edit((char)evnt.KeyValue);
                }
                else
                    Sheet.InvokeKeyPress(keyParams);
            }
            catch (Exception ex)
            {
                onException(ex);
            }
            return true;
        }

        /// <summary>Invoked when the user clicks a mouse button.</summary>
        /// <param name="evnt">The event data.</param>
        /// <returns></returns>
        protected override bool OnButtonPressEvent(EventButton evnt)
        {
            GrabFocus();

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
                onException(ex);
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
                onException(err);
            }
            return true;
        }
    }
}