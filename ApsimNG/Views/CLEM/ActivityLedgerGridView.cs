
namespace UserInterface.Views
{
    using Interfaces;
    using Gtk;
    using System.Data;
    using System;
    using Models.Core;
    using System.Drawing;
    using System.IO;
    using System.Drawing.Imaging;
    using System.Collections.Generic;
    using Extensions;

    public interface IActivityLedgerGridView
    {
        /// <summary>Provides the name of the report for data collection.</summary>
        string ModelName { get; set; }

        /// <summary>Grid for holding data.</summary>
        System.Data.DataTable DataSource { get; set; }

        void LockLeftMostColumns(int number);

        /// <summary>
        /// Gets or sets a value indicating whether the grid is read only
        /// </summary>
        bool ReadOnly { get; set; }

    }

    /// <summary>
    /// An activity ledger disply grid view
    /// </summary>
    public class ActivityLedgerGridView : ViewBase, IActivityLedgerGridView
    {
        /// <summary>
        /// The data table that is being shown on the grid.
        /// </summary>
        private DataTable table;

        /// <summary>
        /// The default numeric format
        /// </summary>
        private string defaultNumericFormat = "F2";

        /// <summary>
        /// Flag to keep track of whether a cursor move was initiated internally
        /// </summary>
        private bool selfCursorMove = false;

        private ScrolledWindow scrolledwindow1 = null;
        public Gtk.TreeView Grid { get; set; }
        public Gtk.TreeView Fixedcolview { get; set; }
        private HBox hbox1 = null;
        private Gtk.Image image1 = null;
        /// <summary>
        /// The splitter between the fixed and non-fixed grids.
        /// </summary>
        private HPaned splitter = null;


        private Gdk.Pixbuf imagePixbuf = null;

        private ListStore gridmodel = new ListStore(typeof(string));
        private Dictionary<CellRenderer, int> colLookup = new Dictionary<CellRenderer, int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="GridView" /> class.
        /// </summary>
        public ActivityLedgerGridView(ViewBase owner) : base(owner)
        {
            Builder builder = ViewBase.BuilderFromResource("ApsimNG.Resources.Glade.GridView.glade");
            hbox1 = (HBox)builder.GetObject("hbox1");
            scrolledwindow1 = (ScrolledWindow)builder.GetObject("scrolledwindow1");
            Grid = (Gtk.TreeView)builder.GetObject("gridview");
            Fixedcolview = (Gtk.TreeView)builder.GetObject("fixedcolview");
            splitter = (HPaned)builder.GetObject("hpaned1");
            image1 = (Gtk.Image)builder.GetObject("image1");
            mainWidget = hbox1;
            Grid.Model = gridmodel;
            Grid.Selection.Mode = SelectionMode.Multiple;
            Fixedcolview.Model = gridmodel;
            Fixedcolview.Selection.Mode = SelectionMode.Multiple;
            Grid.EnableSearch = false;
            Fixedcolview.EnableSearch = false;
            image1.Pixbuf = null;
            image1.Visible = false;
            splitter.Child1.Hide();
            splitter.Child1.NoShowAll = true;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        /// <summary>
        /// Gets or sets the data to use to populate the grid.
        /// </summary>
        public System.Data.DataTable DataSource
        {
            get
            {
                return this.table;
            }

            set
            {
                this.table = value;
                LockLeftMostColumns(0);
                this.PopulateGrid();
            }
        }

        /// <summary>
        /// Does cleanup when the main widget is destroyed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            if (numberLockedCols > 0)
            {
                Grid.Vadjustment.ValueChanged -= Gridview_Vadjustment_Changed;
                Grid.Selection.Changed -= Gridview_CursorChanged;
                Fixedcolview.Vadjustment.ValueChanged -= Fixedcolview_Vadjustment_Changed1;
                Fixedcolview.Selection.Changed -= Fixedcolview_CursorChanged;
            }
            // It's good practice to disconnect the event handlers, as it makes memory leaks
            // less likely. However, we may not "own" the event handlers, so how do we 
            // know what to disconnect?
            // We can do this via reflection. Here's how it currently can be done in Gtk#.
            // Windows.Forms would do it differently.
            // This may break if Gtk# changes the way they implement event handlers.
            ClearGridColumns();
            gridmodel.Dispose();
            if (imagePixbuf != null)
            {
                imagePixbuf.Dispose();
            }

            if (image1 != null)
            {
                image1.Dispose();
            }

            if (table != null)
            {
                table.Dispose();
            }

            mainWidget.Destroyed -= _mainWidget_Destroyed;
            owner = null;
        }

        /// <summary>
        /// Removes all grid columns, and cleans up any associated event handlers
        /// </summary>
        private void ClearGridColumns()
        {
            while (Grid.Columns.Length > 0)
            {
                TreeViewColumn col = Grid.GetColumn(0);
#if NETFRAMEWORK
                foreach (CellRenderer render in col.CellRenderers)
#else
                foreach (CellRenderer render in col.Cells)
#endif
                {
                    if (render is CellRendererText)
                    {
                        CellRendererText textRender = render as CellRendererText;
                        col.SetCellDataFunc(textRender, (CellLayoutDataFunc)null);
                    }
                    else if (render is CellRendererPixbuf)
                    {
                        CellRendererPixbuf pixRender = render as CellRendererPixbuf;
                        col.SetCellDataFunc(pixRender, (CellLayoutDataFunc)null);
                    }
                    render.Dispose();
                }
                Grid.RemoveColumn(Grid.GetColumn(0));
            }
            while (Fixedcolview.Columns.Length > 0)
            {
                TreeViewColumn col = Fixedcolview.GetColumn(0);
                foreach (CellRenderer render in col.GetCells())
                {
                    if (render is CellRendererText)
                    {
                        CellRendererText textRender = render as CellRendererText;
                        col.SetCellDataFunc(textRender, (CellLayoutDataFunc)null);
                    }
                }

                Fixedcolview.RemoveColumn(Fixedcolview.GetColumn(0));
            }
        }

        /// <summary>
        /// Repsonds to selection changes in the "fixed" columns area by
        /// selecting corresponding rows in the main grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Fixedcolview_CursorChanged(object sender, EventArgs e)
        {
            if (!selfCursorMove)
            {
                selfCursorMove = true;
                TreeSelection fixedSel = Fixedcolview.Selection;
                TreePath[] selPaths = fixedSel.GetSelectedRows();

                TreeSelection gridSel = Grid.Selection;
                gridSel.UnselectAll();
                foreach (TreePath path in selPaths)
                {
                    gridSel.SelectPath(path);
                }

                selfCursorMove = false;
            }
        }

        /// <summary>
        /// Repsonds to selection changes in the main grid by
        /// selecting corresponding rows in the "fixed columns" grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Gridview_CursorChanged(object sender, EventArgs e)
        {
            if (Fixedcolview.Visible && !selfCursorMove)
            {
                selfCursorMove = true;
                TreeSelection gridSel = Grid.Selection;
                TreePath[] selPaths = gridSel.GetSelectedRows();

                TreeSelection fixedSel = Fixedcolview.Selection;
                fixedSel.UnselectAll();
                foreach (TreePath path in selPaths)
                {
                    fixedSel.SelectPath(path);
                }

                selfCursorMove = false;
            }
        }

        private int numberLockedCols = 0;

        /// <summary>
        /// Populate the grid from the DataSource.
        /// Note that we don't statically set the contents of the grid cells, but rather do this 
        /// dynamically in OnSetCellData. However, we do set up appropriate attributes for 
        /// cell columns, and a set of cell renderers.
        /// </summary>
        private void PopulateGrid()
        {
            // WaitCursor = true;
            // Set the cursor directly rather than via the WaitCursor property, as the property setter
            // runs a message loop. This is normally desirable, but in this case, we have lots
            // of events associated with the grid data, and it's best to let them be handled in the 
            // main message loop. 

            if (MasterView.MainWindow != null)
            {
                MasterView.MainWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
            }

            ClearGridColumns();
            Fixedcolview.Visible = false;
            colLookup.Clear();
            // Begin by creating a new ListStore with the appropriate number of
            // columns. Use the string column type for everything.
            int nCols = DataSource != null ? this.DataSource.Columns.Count : 0;
            Type[] colTypes = new Type[nCols];
            for (int i = 0; i < nCols; i++)
            {
                colTypes[i] = typeof(string);
            }

            gridmodel = new ListStore(colTypes);
#if NETFRAMEWORK
            Grid.ModifyBase(StateType.Active, Fixedcolview.Style.Base(StateType.Selected));
            Grid.ModifyText(StateType.Active, Fixedcolview.Style.Text(StateType.Selected));
            Fixedcolview.ModifyBase(StateType.Active, Grid.Style.Base(StateType.Selected));
            Fixedcolview.ModifyText(StateType.Active, Grid.Style.Text(StateType.Selected));
#endif

            image1.Visible = false;
            // Now set up the grid columns
            for (int i = 0; i < nCols; i++)
            {
                // Design plan: include renderers for text, toggles and combos, but hide all but one of them
                CellRendererText textRender = new Gtk.CellRendererText();
                CellRendererPixbuf pixbufRender = new CellRendererPixbuf();
                pixbufRender.Pixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.MenuImages.Save.png");
                pixbufRender.Xalign = 0.5f;

                if (i == 0 || i == nCols-1)
                {
                    colLookup.Add(textRender, i);
                }
                else
                {
                    colLookup.Add(pixbufRender, i);
                }

                textRender.FixedHeightFromFont = 1; // 1 line high

                pixbufRender.Height = 19; //TODO change based on zoom rate of UI //previously 23 before smaller UI font
                textRender.Editable = !isReadOnly;
                textRender.Xalign = ((i == 0) || (i == 1) && isPropertyMode) ? 0.0f : 1.0f; // For right alignment of text cell contents; left align the first column

                TreeViewColumn column = new TreeViewColumn();
                column.Title = this.DataSource.Columns[i].Caption;

                if (i==0 || i == nCols - 1)
                {
                    column.PackStart(textRender, true);     // 0
                }
                else
                {
                    column.PackStart(pixbufRender, false);  // 3
                }

                if (i == 0 || i == nCols - 1)
                {
                    column.SetCellDataFunc(textRender, OnSetCellData);
                }
                else
                {
                    column.SetCellDataFunc(pixbufRender, RenderActivityStatus);
                }
                if (i == 1 && isPropertyMode)
                {
                    column.Alignment = 0.0f;
                }
                else
                {
                    column.Alignment = 0.5f; // For centered alignment of the column header
                }

                Grid.AppendColumn(column);

                // Gtk Treeview doesn't support "frozen" columns, so we fake it by creating a second, identical, TreeView to display
                // the columns we want frozen
                // For now, these frozen columns will be treated as read-only text
                TreeViewColumn fixedColumn = new TreeViewColumn(this.DataSource.Columns[i].ColumnName, textRender, "text", i);
                //fixedColumn.Sizing = TreeViewColumnSizing.GrowOnly;
                fixedColumn.Resizable = false;
                if (i == 0)
                {
                    fixedColumn.SetCellDataFunc(textRender, OnSetCellData);
                }
                else
                {
                    fixedColumn.SetCellDataFunc(pixbufRender, RenderActivityStatus);
                }
                fixedColumn.Alignment = 0.0f; // For centered alignment of the column header
                fixedColumn.Visible = false;
                Fixedcolview.AppendColumn(fixedColumn);
            }

            if (!isPropertyMode)
            {
                // Add an empty column at the end; auto-sizing will give this any "leftover" space
                TreeViewColumn fillColumn = new TreeViewColumn();
                Grid.AppendColumn(fillColumn);
                fillColumn.Sizing = TreeViewColumnSizing.Autosize;
            }

            int nRows = DataSource != null ? this.DataSource.Rows.Count : 0;

            Grid.Model = null;
            Fixedcolview.Model = null;
            for (int row = 0; row < nRows; row++)
            {
                // We could store data into the grid model, but we don't.
                // Instead, we retrieve the data from our datastore when the OnSetCellData function is called
                gridmodel.Append();
            }
            Grid.Model = gridmodel;

            SetColumnHeaders(Grid);
            SetColumnHeaders(Fixedcolview);

            Grid.EnableSearch = false;
            Fixedcolview.EnableSearch = false;

            Grid.Show();

            if (MasterView.MainWindow != null)
            {
                MasterView.MainWindow.Cursor = null;
            }
        }

        /// <summary>
        /// Sets the contents of a cell being display on a grid
        /// </summary>
        /// <param name="col"></param>
        /// <param name="cell"></param>
        /// <param name="model"></param>
        /// <param name="iter"></param>
#if NETFRAMEWORK
        public void RenderActivityStatus(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
#else
        public void RenderActivityStatus(TreeViewColumn col, CellRenderer cell, ITreeModel model, TreeIter iter)
#endif
        {
            TreePath path = model.GetPath(iter);
            int rowNo = path.Indices[0];
            int colNo;
            string text = String.Empty;
            if (colLookup.TryGetValue(cell, out colNo) && rowNo < this.DataSource.Rows.Count && colNo < this.DataSource.Columns.Count)
            {
                object dataVal = this.DataSource.Rows[rowNo][colNo];
                cell.Visible = true;
                string iconName = "blank";
                switch (dataVal.ToString())
                {
                    case "Success":
                        iconName = "Success";
                        break;
                    case "Partial":
                        iconName = "Partial";
                        break;
                    case "Ignored":
                        iconName = "NoTask";
                        break;
                    case "Critical":
                        iconName = "Critical";
                        break;
                    case "Timer":
                        iconName = "Timer";
                        break;
                    case "Calculation":
                        iconName = "Calculation";
                        break;
                    case "NotNeeded":
                        iconName = "NotNeeded";
                        break;
                    case "Warning":
                        iconName = "Ignored";
                        break;
                    case "NoTask":
                        iconName = "NoTask";
                        break;
                }
                (cell as CellRendererPixbuf).Pixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.MenuImages."+iconName+".png");
            }
        }

        /// <summary>
        /// Sets the contents of a cell being display on a grid
        /// </summary>
        /// <param name="col"></param>
        /// <param name="cell"></param>
        /// <param name="model"></param>
        /// <param name="iter"></param>
#if NETFRAMEWORK
        public void OnSetCellData(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
#else
        public void OnSetCellData(TreeViewColumn col, CellRenderer cell, ITreeModel model, TreeIter iter)
#endif
        {
            TreePath path = model.GetPath(iter);
            Gtk.TreeView view = col.TreeView as Gtk.TreeView;
            int rowNo = path.Indices[0];
            int colNo;
            string text = String.Empty;
            if (colLookup.TryGetValue(cell, out colNo) && rowNo < this.DataSource.Rows.Count && colNo < this.DataSource.Columns.Count)
            {
                object dataVal = this.DataSource.Rows[rowNo][colNo];
                text = AsString(dataVal);
            }
            cell.Visible = true;
            (cell as CellRendererText).Text = text;
        }

        /// <summary>
        /// Modify the settings of all column headers
        /// We apply center-justification to all the column headers, just for the heck of it
        /// Note that "justification" here refers to justification of wrapped lines, not 
        /// justification of the header as a whole, which is handled with column.Alignment
        /// We create new Labels here, and use markup to make them bold, since other approaches 
        /// don't seem to work consistently
        /// </summary>
        /// <param name="view">The treeview for which headings are to be modified</param>
        private void SetColumnHeaders(Gtk.TreeView view)
        {
            int nCols = DataSource != null ? this.DataSource.Columns.Count : 0;
            for (int i = 0; i < nCols; i++)
            {
                Label newLabel = new Label();
                view.Columns[i].Widget = newLabel;
                newLabel.Wrap = true;
                newLabel.Justify = Justification.Center;
                if (i == 1 && isPropertyMode)  // Add a tiny bit of extra space when left-aligned
                {
                    (newLabel.Parent as Alignment).LeftPadding = 2;
                }

                newLabel.UseMarkup = true;
                newLabel.Markup = "<b>" + System.Security.SecurityElement.Escape(Grid.Columns[i].Title) + "</b>";
                if (this.DataSource.Columns[i].Caption != this.DataSource.Columns[i].ColumnName)
                {
                    newLabel.Parent.Parent.Parent.TooltipText = this.DataSource.Columns[i].ColumnName;
                }

                newLabel.Show();
            }
        }


        /// <summary>
        /// Handle vertical scrolling changes to keep the gridview and fixedcolview at the same scrolled position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Fixedcolview_Vadjustment_Changed1(object sender, EventArgs e)
        {
            Grid.Vadjustment.Value = Fixedcolview.Vadjustment.Value;
        }

        /// <summary>
        /// Handle vertical scrolling changes to keep the gridview and fixedcolview at the same scrolled position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Gridview_Vadjustment_Changed(object sender, EventArgs e)
        {
            Fixedcolview.Vadjustment.Value = Grid.Vadjustment.Value;
        }

        /// <summary>
        /// The name of the associated model.
        /// </summary>
        public string ModelName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of rows in grid.
        /// </summary>
        public int RowCount
        {
            get
            {
                return gridmodel.IterNChildren();
            }

            set
            {
                // The main use of this will be to allow "empty" rows at the bottom of the grid to allow for
                // additional data to be entered (primarily soil profile stuff). 
                if (value > RowCount) // Add new rows
                {
                    for (int i = RowCount; i < value; i++)
                    {
                        gridmodel.Append(); // Will this suffice?
                    }
                }
                else if (value < RowCount) // Remove existing rows. But let's check first to be sure they're empty
                {
                    // TBI
                }
                // TBI this.Grid.RowCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the numeric grid format e.g. N3
        /// </summary>
        public string NumericFormat
        {
            get
            {
                return this.defaultNumericFormat;
            }

            set
            {
                this.defaultNumericFormat = value;
            }
        }

        private bool isPropertyMode = false;

        /// <summary>
        /// Gets or sets a value indicating whether "property" mode is enabled
        /// </summary>
        public bool PropertyMode
        {
            get
            {
                return isPropertyMode;
            }
            set
            {
                if (value != isPropertyMode)
                {
                    this.PopulateGrid();
                }
                isPropertyMode = value;
            }
        }

        /// <summary>
        /// Stores whether our grid is readonly. Internal value.
        /// </summary>
        private bool isReadOnly = false;

        /// <summary>
        /// Gets or sets a value indicating whether the grid is read only
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                return isReadOnly;
            }

            set
            {
                if (value != isReadOnly)
                {
                    foreach (TreeViewColumn col in Grid.Columns)
                    {
                        foreach (CellRenderer render in col.GetCells())
                        {
                            if (render is CellRendererText)
                            {
                                (render as CellRendererText).Editable = !value;
                            }
                        }
                    }
                }
                isReadOnly = value;
            }
        }

        /// <summary>
        /// Returns the string representation of an object. For most objects,
        /// this will be the same as "ToString()", but for Crops, it will give
        /// the crop name
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private string AsString(object obj)
        {
            string result;
            if (obj is IPlant)
            {
                result = (obj as IModel).Name;
            }
            else
            {
                result = obj.ToString();
            }

            return result;
        }

        /// <summary>Lock the left most number of columns.</summary>
        /// <param name="number"></param>
        public void LockLeftMostColumns(int number)
        {
            if (number == numberLockedCols || !Grid.IsMapped)
            {
                return;
            }

            for (int i = 0; i < gridmodel.NColumns; i++)
            {
                if (Fixedcolview.Columns.Length > i)
                {
                    Fixedcolview.Columns[i].Visible = i < number;
                }

                if (Grid.Columns.Length > i)
                {
                    Grid.Columns[i].Visible = i >= number;
                }
            }
            if (number > 0)
            {
                if (numberLockedCols == 0)
                {
                    Grid.Vadjustment.ValueChanged += Gridview_Vadjustment_Changed;
                    Grid.Selection.Changed += Gridview_CursorChanged;
                    Fixedcolview.Vadjustment.ValueChanged += Fixedcolview_Vadjustment_Changed1;
                    Fixedcolview.Selection.Changed += Fixedcolview_CursorChanged;
                    Gridview_CursorChanged(this, EventArgs.Empty);
                    Gridview_Vadjustment_Changed(this, EventArgs.Empty);
                }
                if (!splitter.Child1.Visible)
                {
                    //Grid.Vadjustment.ValueChanged += GridviewVadjustmentChanged;
                    //Grid.Selection.Changed += GridviewCursorChanged;
                    //fixedcolview.Vadjustment.ValueChanged += FixedcolviewVadjustmentChanged;
                    //fixedcolview.Selection.Changed += FixedcolviewCursorChanged;
                    //GridviewCursorChanged(this, EventArgs.Empty);
                    //GridviewVadjustmentChanged(this, EventArgs.Empty);
                }

                Fixedcolview.Model = gridmodel;
                Fixedcolview.Visible = true;
                splitter.Child1.NoShowAll = false;
                splitter.ShowAll();
                splitter.PositionSet = true;
                int splitterWidth = (int)splitter.StyleGetProperty("handle-size");
                if (splitter.Allocation.Width > 1)
                {
                    splitter.Position = Math.Min(Fixedcolview.WidthRequest + splitterWidth, splitter.Allocation.Width / 2);
                }
                else
                {
                    splitter.Position = Fixedcolview.WidthRequest + splitterWidth;
                }

            }
            else
            {
                Grid.Vadjustment.ValueChanged -= Gridview_Vadjustment_Changed;
                Grid.Selection.Changed -= Gridview_CursorChanged;
                Fixedcolview.Vadjustment.ValueChanged -= Fixedcolview_Vadjustment_Changed1;
                Fixedcolview.Selection.Changed -= Fixedcolview_CursorChanged;
                Fixedcolview.Visible = false;
                splitter.Position = 0;
                splitter.Child1.Hide();
            }
            numberLockedCols = number;
        }

        /// <summary>Get screenshot of grid.</summary>
        public System.Drawing.Image GetScreenshot()
        {
#if NETFRAMEWORK
            // Create a Bitmap and draw the DataGridView on it.
            int width;
            int height;
            Gdk.Window gridWindow = hbox1.GdkWindow;  // Should we draw from hbox1 or from gridview?
            gridWindow.GetSize(out width, out height);
            Gdk.Pixbuf screenshot = Gdk.Pixbuf.FromDrawable(gridWindow, gridWindow.Colormap, 0, 0, 0, 0, width, height);
            byte[] buffer = screenshot.SaveToBuffer("png");
            MemoryStream stream = new MemoryStream(buffer);
            System.Drawing.Bitmap bitmap = new Bitmap(stream);
            return bitmap;
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// Called when the window is resized to resize all grid controls.
        /// </summary>
        public void ResizeControls()
        {
            if (gridmodel.NColumns == 0)
            {
                return;
            }

            if (gridmodel.IterNChildren() == 0)
            {
                Grid.Visible = false;
            }
            else
            {
                Grid.Visible = true;
            }
        }

        private void GridView_Resize(object sender, EventArgs e)
        {
            ResizeControls();
        }

        /// <summary>
        /// Does some cleanup work on the Grid.
        /// </summary>
        public void Dispose()
        {
            ClearGridColumns();
            gridmodel.Dispose();
            if (table != null)
            {
                table.Dispose();
            }
            mainWidget.Destroyed -= MainWidgetDestroyed;
            owner = null;
        }

        /// <summary>
        /// Does cleanup when the main widget is destroyed.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event arguments.</param>
        private void MainWidgetDestroyed(object sender, EventArgs e)
        {
            Dispose();
        }

    }
}
