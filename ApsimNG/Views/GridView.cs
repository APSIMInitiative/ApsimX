// -----------------------------------------------------------------------
// <copyright file="GridView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Reflection;
    using Classes;
    using DataGridViewAutoFilter;
    using EventArguments;
    using Glade;
    using Gtk;
    using Interfaces;

    /// <summary>
    /// A grid control that implements the grid view interface.
    /// </summary>
    public class GridView : ViewBase, IGridView
    {
        /// <summary>
        /// This event is invoked when the values of 1 or more cells have changed.
        /// </summary>
        public event EventHandler<GridCellsChangedArgs> CellsChanged;

        /// <summary>
        /// Invoked when a grid cell header is clicked.
        /// </summary>
        public event EventHandler<GridHeaderClickedArgs> ColumnHeaderClicked;

        /// <summary>Occurs when user clicks a button on the cell.</summary>
        public event EventHandler<GridCellsChangedArgs> ButtonClick;

        /// <summary>
        /// Is the user currently editing a cell?
        /// </summary>
        private bool userEditingCell = false;

        /// <summary>
        /// The value before the user starts editing a cell.
        /// </summary>
        private object valueBeforeEdit;

        /// <summary>
        /// The data table that is being shown on the grid.
        /// </summary>
        private DataTable table;

        /// <summary>
        /// A value indicating whether auto filter is turned on.
        /// </summary>
        private bool isAutoFilterOn = false;

        /// <summary>
        /// The default numeric format
        /// </summary>
        private string defaultNumericFormat = "F2";

        [Widget]
        private ScrolledWindow scrolledwindow1 = null;
        //[Widget]
        //private ScrolledWindow scrolledwindow2 = null;

        [Widget]
        public TreeView gridview = null;
        [Widget]
        public TreeView fixedcolview = null;

        [Widget]
        private HBox hbox1 = null;
        [Widget]
        private Gtk.Image image1 = null;

        private Gdk.Pixbuf imagePixbuf;

        private ListStore gridmodel = new ListStore(typeof(string));
        private Dictionary<CellRenderer, int> colLookup = new Dictionary<CellRenderer, int>();
        private Menu Popup = new Menu();
        private AccelGroup accel = new AccelGroup();

        /// <summary>
        /// Initializes a new instance of the <see cref="GridView" /> class.
        /// </summary>
        public GridView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.GridView.glade", "hbox1");
            gxml.Autoconnect(this);
            _mainWidget = hbox1;
            gridview.Model = gridmodel;
            gridview.Selection.Mode = SelectionMode.Multiple;
            fixedcolview.Model = gridmodel;
            fixedcolview.Selection.Mode = SelectionMode.Multiple;
            Popup.AttachToWidget(gridview, null);
            AddContextActionWithAccel("Copy", OnCopyToClipboard, "Ctrl+C");
            AddContextActionWithAccel("Paste", OnPasteFromClipboard, "Ctrl+V");
            AddContextActionWithAccel("Delete", OnDeleteClick, "Delete");
            gridview.ButtonPressEvent += OnButtonDown;
            fixedcolview.ButtonPressEvent += OnButtonDown;
            gridview.FocusInEvent += FocusInEvent;
            gridview.FocusOutEvent += FocusOutEvent;
            fixedcolview.FocusInEvent += FocusInEvent;
            fixedcolview.FocusOutEvent += FocusOutEvent;
            image1.Visible = false;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private bool selfCursorMove = false;

        private void Fixedcolview_CursorChanged(object sender, EventArgs e)
        {
            if (!selfCursorMove)
            {
                selfCursorMove = true;
                TreeSelection fixedSel = fixedcolview.Selection;
                TreePath[] selPaths = fixedSel.GetSelectedRows();

                TreeSelection gridSel = gridview.Selection;
                gridSel.UnselectAll();
                foreach (TreePath path in selPaths)
                    gridSel.SelectPath(path);
                selfCursorMove = false;
            }
        }

        private void Gridview_CursorChanged(object sender, EventArgs e)
        {
            if (fixedcolview.Visible && !selfCursorMove)
            {
                selfCursorMove = true;
                TreeSelection gridSel = gridview.Selection;
                TreePath[] selPaths = gridSel.GetSelectedRows();

                TreeSelection fixedSel = fixedcolview.Selection;
                fixedSel.UnselectAll();
                foreach (TreePath path in selPaths)
                    fixedSel.SelectPath(path);
                selfCursorMove = false;
            }
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            if (numberLockedCols > 0)
            {
                gridview.Vadjustment.ValueChanged -= Gridview_Vadjustment_Changed;
                gridview.Selection.Changed -= Gridview_CursorChanged;
                fixedcolview.Vadjustment.ValueChanged -= Fixedcolview_Vadjustment_Changed1;
                fixedcolview.Selection.Changed -= Fixedcolview_CursorChanged;
            }
            gridview.ButtonPressEvent -= OnButtonDown;
            fixedcolview.ButtonPressEvent -= OnButtonDown;
            // It's good practice to disconnect the event handlers, as it makes memory leaks
            // less likely. However, we may not "own" the event handlers, so how do we 
            // know what to disconnect?
            // We can do this via reflection. Here's how it currently can be done in Gtk#.
            // Windows.Forms would do it differently.
            // This may break if Gtk# changes the way they implement event handlers.
            foreach (Widget w in Popup)
            {
                if (w is ImageMenuItem)
                {
                    PropertyInfo pi = w.GetType().GetProperty("AfterSignals", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (pi != null)
                    {
                        System.Collections.Hashtable handlers = (System.Collections.Hashtable)pi.GetValue(w);
                        if (handlers != null && handlers.ContainsKey("activate"))
                        {
                            EventHandler handler = (EventHandler)handlers["activate"];
                            (w as ImageMenuItem).Activated -= handler;
                        }
                    }
                }
            }
            ClearGridColumns();
        }

        private void ClearGridColumns()
        {
            while (gridview.Columns.Length > 0)
            {
                TreeViewColumn col = gridview.GetColumn(0);
                foreach (CellRenderer render in col.CellRenderers)
                    if (render is CellRendererText)
                    {
                        CellRendererText textRender = render as CellRendererText;
                        textRender.EditingStarted -= OnCellBeginEdit;
                        textRender.Edited -= OnCellValueChanged;
                        col.SetCellDataFunc(textRender, (CellLayoutDataFunc)null);
                    }
                gridview.RemoveColumn(gridview.GetColumn(0));
            }
            while (fixedcolview.Columns.Length > 0)
            {
                TreeViewColumn col = fixedcolview.GetColumn(0);
                foreach (CellRenderer render in col.CellRenderers)
                    if (render is CellRendererText)
                    {
                        CellRendererText textRender = render as CellRendererText;
                        textRender.EditingStarted -= OnCellBeginEdit;
                        textRender.Edited -= OnCellValueChanged;
                        col.SetCellDataFunc(textRender, (CellLayoutDataFunc)null);
                    }
                fixedcolview.RemoveColumn(fixedcolview.GetColumn(0));
            }
        }

        private int numberLockedCols = 0;

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
        /// Populate the grid from the DataSource.
        /// </summary>
        private void PopulateGrid()
        {
            WaitCursor = true;
            ClearGridColumns();
            fixedcolview.Visible = false;
            colLookup.Clear();
            // Begin by creating a new ListStore with the appropriate number of
            // columns. Use the string column type for everything.
            int nCols = DataSource != null ? this.DataSource.Columns.Count : 0;
            Type[] colTypes = new Type[nCols];
            for (int i = 0; i < nCols; i++)
                colTypes[i] = typeof(string);
            gridmodel = new ListStore(colTypes);
            gridview.ModifyBase(StateType.Active, fixedcolview.Style.Base(StateType.Selected));
            gridview.ModifyText(StateType.Active, fixedcolview.Style.Text(StateType.Selected));
            fixedcolview.ModifyBase(StateType.Active, gridview.Style.Base(StateType.Selected));
            fixedcolview.ModifyText(StateType.Active, gridview.Style.Text(StateType.Selected));

            image1.Visible = false;
            // Now set up the grid columns
            for (int i = 0; i < nCols; i++)
            {
                /// Design plan: include renderers for text, toggles and combos, but hide all but one of them
                CellRendererText textRender = new Gtk.CellRendererText();
                colLookup.Add(textRender, i);

                textRender.Editable = !isReadOnly;
                textRender.EditingStarted += OnCellBeginEdit;
                textRender.Edited += OnCellValueChanged;
                textRender.Xalign = i == 0 ? 0.0f : 1.0f; // For right alignment of text cell contents; left align the first column

                TreeViewColumn column = new TreeViewColumn(this.DataSource.Columns[i].ColumnName, textRender, "text", i);
                column.Sizing = TreeViewColumnSizing.Autosize;
                //column.FixedWidth = 100;
                column.Resizable = true;
                column.SetCellDataFunc(textRender, OnSetCellData);
                column.Alignment = 0.5f; // For centered alignment of the column header
                gridview.AppendColumn(column);

                // Gtk Treeview doesn't support "frozen" columns, so we fake it by creating a second, identical, TreeView to display
                // the columns we want frozen
                TreeViewColumn fixedColumn = new TreeViewColumn(this.DataSource.Columns[i].ColumnName, textRender, "text", i);
                fixedColumn.Sizing = TreeViewColumnSizing.Autosize;
                fixedColumn.Resizable = true;
                fixedColumn.SetCellDataFunc(textRender, OnSetCellData);
                fixedColumn.Alignment = 0.5f; // For centered alignment of the column header
                fixedColumn.Visible = false;
                fixedcolview.AppendColumn(fixedColumn);
            }
            // Add an empty column at the end; auto-sizing will give this any "leftover" space
            TreeViewColumn fillColumn = new TreeViewColumn();
            gridview.AppendColumn(fillColumn);
            fillColumn.Sizing = TreeViewColumnSizing.Autosize;

            // Now let's apply center-justification to all the column headers, just for the heck of it
            for (int i = 0; i < nCols; i++)
            {
                Label label = GetColumnHeaderLabel(i);
                label.Justify = Justification.Center;
                label = GetColumnHeaderLabel(i, fixedcolview);
                label.Justify = Justification.Center;
            }

            int nRows = DataSource != null ? this.DataSource.Rows.Count : 0;

            gridview.Model = null;
            fixedcolview.Model = null;
            for (int row = 0; row < nRows; row++)
            {
                // We could store data into the grid model, but we don't.
                // Instead, we retrieve the data from our datastore when the OnSetCellData function is called
                gridmodel.Append();
            }
            gridview.FixedHeightMode = true;
            fixedcolview.FixedHeightMode = true;
            gridview.Model = gridmodel;

            gridview.Show();
            while (Gtk.Application.EventsPending())
                Gtk.Application.RunIteration();
            WaitCursor = false;
        }
        private void Fixedcolview_Vadjustment_Changed1(object sender, EventArgs e)
        {
            gridview.Vadjustment.Value = fixedcolview.Vadjustment.Value;
        }

        private void Gridview_Vadjustment_Changed(object sender, EventArgs e)
        {
            fixedcolview.Vadjustment.Value = gridview.Vadjustment.Value;
        }

        public void OnSetCellData(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
        {
            TreePath startPath;
            TreePath endPath;
            TreePath path = model.GetPath(iter);
            int rowNo = path.Indices[0];
            // This gets called a lot, even when it seemingly isn't necessary.
            if (numberLockedCols == 0 && gridview.GetVisibleRange(out startPath, out endPath) &&  (rowNo < startPath.Indices[0] || rowNo > endPath.Indices[0]))
                return;
            int colNo;
            if (colLookup.TryGetValue(cell, out colNo) && rowNo < this.DataSource.Rows.Count && colNo < this.DataSource.Columns.Count)
            {
                object dataVal = this.DataSource.Rows[rowNo][colNo];
                Type dataType = dataVal.GetType();
                string text;
                if (dataType == typeof(DBNull))
                    text = String.Empty;
                else if ((dataType == typeof(float) && !float.IsNaN((float)dataVal)) ||
                    (dataType == typeof(double) && !Double.IsNaN((double)dataVal)))
                    text = String.Format("{0:" + NumericFormat + "}", dataVal);
                else if (dataType == typeof(DateTime))
                    text = String.Format("{0:d}", dataVal);
                else
                    text = dataVal.ToString();
                (cell as CellRendererText).Text = text;
            }
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
                        gridmodel.Append(); // Will this suffice?
                }
                else if (value < RowCount) // Remove existing rows. But let's check first to be sure they're empty
                {
                    /// TBI
                }
                /// TBI this.Grid.RowCount = value;
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
                    foreach (TreeViewColumn col in gridview.Columns)
                        foreach (CellRenderer render in col.CellRenderers)
                            if (render is CellRendererText)
                                (render as CellRendererText).Editable = !value;
                }
                isReadOnly = value;
            } 
        }

        /// <summary>
        /// Gets or sets a value indicating whether the grid has an auto filter
        /// </summary>
        public bool AutoFilterOn
        {
            get
            {
                return this.isAutoFilterOn;
            }
            
            set 
            {

                // MONO doesn't seem to like the auto filter option.
                if (Environment.OSVersion.Platform == PlatformID.Win32NT ||
                    Environment.OSVersion.Platform == PlatformID.Win32Windows)
                {
                    this.isAutoFilterOn = value;
                    this.PopulateGrid();
                }    
            }
        }

        /// <summary>
        /// Gets or sets the currently selected cell. Null if none selected.
        /// </summary>
        public IGridCell GetCurrentCell
        {
            get
            {
                TreePath path;
                TreeViewColumn col;
                gridview.GetCursor(out path, out col);
                if (path != null)
                {
                    int colNo, rowNo;
                    rowNo = path.Indices[0];
                    if (colLookup.TryGetValue(col.Cells[0], out colNo))
                      return this.GetCell(colNo, rowNo);
                }
                return null;
            }

            set
            {
                if (value != null)
                {
                    TreePath row = new TreePath(new int[1] { value.RowIndex });
                    gridview.SetCursor(row, gridview.GetColumn(value.ColumnIndex), false);
                }
            }
        }

        /// <summary>
        /// Return a particular cell of the grid.
        /// </summary>
        /// <param name="columnIndex">The column index</param>
        /// <param name="rowIndex">The row index</param>
        /// <returns>The cell</returns>
        public IGridCell GetCell(int columnIndex, int rowIndex)
        {
            return new GridCell(this, columnIndex, rowIndex);
        }

        /// <summary>
        /// Return a particular column of the grid.
        /// </summary>
        /// <param name="columnIndex">The column index</param>
        /// <returns>The column</returns>
        public IGridColumn GetColumn(int columnIndex)
        {
            return new GridColumn(this, columnIndex);
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        public void AddContextSeparator()
        {
            Popup.Append(new SeparatorMenuItem());
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        public void AddContextAction(string menuItemText, System.EventHandler onClick)
        {
            ImageMenuItem item = new ImageMenuItem(menuItemText);
            item.Activated += onClick;
            Popup.Append(item);
            Popup.ShowAll();
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        public void AddContextActionWithAccel(string menuItemText, System.EventHandler onClick, string shortcut)
        {
            ImageMenuItem item = new ImageMenuItem(menuItemText);
            if (!String.IsNullOrEmpty(shortcut))
            {
                string keyName = String.Empty;
                Gdk.ModifierType modifier = Gdk.ModifierType.None;
                string[] keyNames = shortcut.Split(new Char[] { '+' });
                foreach (string name in keyNames)
                {
                    if (name == "Ctrl")
                        modifier |= Gdk.ModifierType.ControlMask;
                    else if (name == "Shift")
                        modifier |= Gdk.ModifierType.ShiftMask;
                    else if (name == "Alt")
                        modifier |= Gdk.ModifierType.Mod1Mask;
                    else if (name == "Del")
                        keyName = "Delete";
                    else
                        keyName = name;
                }
                try
                {
                    Gdk.Key accelKey = (Gdk.Key)Enum.Parse(typeof(Gdk.Key), keyName, false);
                    item.AddAccelerator("activate", accel, (uint)accelKey, modifier, AccelFlags.Visible);
                }
                catch
                {
                }
            }
            item.Activated += onClick;
            Popup.Append(item);
            Popup.ShowAll();
        }

        /// <summary>
        /// Handle loss of focus by removing the accelerators from the popup menu
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void FocusOutEvent(object o, FocusOutEventArgs args)
        {
            ((o as Widget).Toplevel as Gtk.Window).RemoveAccelGroup(accel);
        }

        /// <summary>
        /// Handle receiving focus by adding accelerators for the popup menu
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void FocusInEvent(object o, FocusInEventArgs args)
        {
            ((o as Widget).Toplevel as Gtk.Window).AddAccelGroup(accel);
        }


        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        public void AddContextOption(string menuItemText, System.EventHandler onClick, bool active)
        {
            CheckMenuItem item = new CheckMenuItem(menuItemText);
            item.DrawAsRadio = true;
            item.Active = active;
            item.Activated += onClick;
            Popup.Append(item);
            Popup.ShowAll();
        }

        /// <summary>
        /// Clear all presenter defined context items.
        /// </summary>
        public void ClearContextActions()
        {
            while (Popup.Children.Length > 3)
                Popup.Remove(Popup.Children[3]);
        }

        /// <summary>
        /// Loads an image from a supplied bitmap.
        /// </summary>
        /// <param name="bitmap">The image to display.</param>
        public void LoadImage(Bitmap bitmap)
        {
            imagePixbuf = ImageToPixbuf(bitmap);
            // We should do a better job of rescaling the image. Any ideas?
            double scaleFactor = Math.Min(250.0 / imagePixbuf.Height, 250.0 / imagePixbuf.Width);
            image1.Pixbuf = imagePixbuf.ScaleSimple((int)(imagePixbuf.Width * scaleFactor), (int)(imagePixbuf.Height * scaleFactor), Gdk.InterpType.Bilinear);
            image1.Visible = true;
            scrolledwindow1.HscrollbarPolicy = PolicyType.Never;
        }


        private static Gdk.Pixbuf ImageToPixbuf(System.Drawing.Image image)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Bmp);
                stream.Position = 0;
                Gdk.Pixbuf pixbuf = new Gdk.Pixbuf(stream);
                return pixbuf;
            }
        }
         
        /// <summary>
        /// Loads an image from a manifest resource.
        /// </summary>
        public void LoadImage()
        {
            System.Reflection.Assembly thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            System.IO.Stream file = thisExe.GetManifestResourceStream("ApsimNG.Resources.PresenterPictures." + ModelName + ".png");
            if (file == null)
               image1.Visible = false;
            else
            {
                imagePixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.PresenterPictures." + ModelName + ".png");
                // We should do a better job of rescaling the image. Any ideas?
                double scaleFactor = Math.Min(250.0 / imagePixbuf.Height, 250.0 / imagePixbuf.Width);
                image1.Pixbuf = imagePixbuf.ScaleSimple((int)(imagePixbuf.Width * scaleFactor), (int)(imagePixbuf.Height * scaleFactor), Gdk.InterpType.Bilinear);
                image1.Visible = true;
                scrolledwindow1.HscrollbarPolicy = PolicyType.Never;
            }
        }

        /// <summary>
        /// Returns true if the grid row is empty.
        /// </summary>
        /// <param name="rowIndex">The row index</param>
        /// <returns>True if the row is empty</returns>
        public bool RowIsEmpty(int rowIndex)
        {
            // What should we look at here? "DataSource" or "gridmodel"
            // They should be synchronized, but....
            // The Windows.Forms version looked at the grid data, so let's do the same here.
            TreeIter iter;
            if (gridmodel.IterNthChild(out iter, rowIndex))
            {
                for (int i = 0; i < gridmodel.NColumns; i++)
                {
                    string contents = gridmodel.GetValue(iter, i) as string;
                    if (!String.IsNullOrEmpty(contents))
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// End the user editing the cell.
        /// </summary>
        public void EndEdit()
        {
            /// TBI this.Grid.EndEdit();
        }

        /// <summary>Lock the left most number of columns.</summary>
        /// <param name="number"></param>
        public void LockLeftMostColumns(int number)
        {
            if (number == numberLockedCols)
                return;
            for (int i = 0; i < gridmodel.NColumns; i++)
            {
                if (fixedcolview.Columns.Length > i)
                   fixedcolview.Columns[i].Visible = i < number;
                if (gridview.Columns.Length > i)
                    gridview.Columns[i].Visible = i >= number;
            }
            if (number > 0)
            {
                if (numberLockedCols == 0)
                {
                    gridview.Vadjustment.ValueChanged += Gridview_Vadjustment_Changed;
                    gridview.Selection.Changed += Gridview_CursorChanged;
                    fixedcolview.Vadjustment.ValueChanged += Fixedcolview_Vadjustment_Changed1;
                    fixedcolview.Selection.Changed += Fixedcolview_CursorChanged;
                    Gridview_CursorChanged(this, EventArgs.Empty);
                    Gridview_Vadjustment_Changed(this, EventArgs.Empty);
                }
                fixedcolview.Model = gridmodel;
                fixedcolview.Visible = true;
            }
            else
            {
                gridview.Vadjustment.ValueChanged -= Gridview_Vadjustment_Changed;
                gridview.Selection.Changed -= Gridview_CursorChanged;
                fixedcolview.Vadjustment.ValueChanged -= Fixedcolview_Vadjustment_Changed1;
                fixedcolview.Selection.Changed -= Fixedcolview_CursorChanged;
                fixedcolview.Visible = false;
            }
            numberLockedCols = number;
        }

        /// <summary>Get screenshot of grid.</summary>
        public System.Drawing.Image GetScreenshot()
        {
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
        }

        /// <summary>
        /// User is about to edit a cell.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnCellBeginEdit(object sender, EditingStartedArgs e)
        {
            this.userEditingCell = true;
            IGridCell where = GetCurrentCell;
            if (where.RowIndex >= DataSource.Rows.Count)
            {
                for (int i = DataSource.Rows.Count; i <= where.RowIndex; i++)
                {
                    DataRow row = DataSource.NewRow();
                    DataSource.Rows.Add(row);
                }
            }
            this.valueBeforeEdit = this.DataSource.Rows[where.RowIndex][where.ColumnIndex];
        }

        /// <summary>
        /// User has finished editing a cell.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnCellValueChanged(object sender, EditedArgs e)
        {

            if (this.userEditingCell)
            {
                IGridCell where = GetCurrentCell;
                 
                object oldValue = this.valueBeforeEdit;
                
                this.userEditingCell = false;

                // Make sure our table has enough rows.
                string newtext = e.NewText;
                object newValue = oldValue;
                if (newtext == null)
                {
                    newValue = DBNull.Value;
                }

                Type dataType = oldValue.GetType();
                if (oldValue == DBNull.Value)
                    dataType = this.DataSource.Columns[where.ColumnIndex].DataType;
                if (dataType == typeof(string))
                    newValue = newtext;
                else if (dataType == typeof(double))
                {
                    double numval;
                    if (Double.TryParse(newtext, out numval))
                        newValue = numval;
                    else
                        newValue = Double.NaN;
                }
                else if (dataType == typeof(Single))
                {
                    Single numval;
                    if (Single.TryParse(newtext, out numval))
                        newValue = numval;
                    else
                        newValue = Single.NaN;
                }
                else if (dataType == typeof(int))
                {
                    int numval;
                    if (int.TryParse(newtext, out numval))
                        newValue = numval;
                    else
                        newValue = Single.NaN;
                }
                else if (dataType == typeof(DateTime))
                {
                    DateTime dateval;
                    if (DateTime.TryParse(newtext, out dateval))
                        newValue = dateval;
                }

                while (this.DataSource != null && where.RowIndex >= this.DataSource.Rows.Count)
                {
                    this.DataSource.Rows.Add(this.DataSource.NewRow());
                }

                // Put the new value into the table on the correct row.
                if (this.DataSource != null)
                {
                    try
                    {
                        this.DataSource.Rows[where.RowIndex][where.ColumnIndex] = newValue;
                    }
                    catch (Exception)
                    {
                    }
                }

                if (this.valueBeforeEdit != null && this.valueBeforeEdit.GetType() == typeof(string) && newValue == null)
                {
                    newValue = string.Empty;
                }

                if (this.CellsChanged != null && this.valueBeforeEdit != newValue)
                {
                    GridCellsChangedArgs args = new GridCellsChangedArgs();
                    args.ChangedCells = new List<IGridCell>();
                    args.ChangedCells.Add(this.GetCell(where.ColumnIndex, where.RowIndex));
                    this.CellsChanged(this, args);
                }
            }
        }

        /// <summary>
        /// Called when the window is resized to resize all grid controls.
        /// </summary>
        public void ResizeControls()
        {
            if (gridmodel.NColumns == 0)
                return;

            if (gridmodel.IterNChildren() == 0)
            {
                gridview.Visible = false;
            }
            else
                gridview.Visible = true;
        }

        /// <summary>
        /// Trap any grid data errors, usually as a result of cell values not being
        /// in combo boxes. We'll handle these elsewhere.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnDataError(object sender, /* TBI DataGridViewDataError */ EventArgs e)
        {
            /// TBI e.Cancel = true;
        }

        /// <summary>
        /// User has clicked a cell. 
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnCellMouseDown(object sender, /* TBI DataGridViewCellMouse */ EventArgs e)
        {
            /*
            if (e.RowIndex == -1)
            {
                if (this.ColumnHeaderClicked != null)
                {
                    GridHeaderClickedArgs args = new GridHeaderClickedArgs();
                    args.Column = this.GetColumn(e.ColumnIndex);
                    args.RightClick = e.Button == System.Windows.Forms.MouseButtons.Right;
                    this.ColumnHeaderClicked.Invoke(this, args);
                }
            }
            else if (this.Grid[e.ColumnIndex, e.RowIndex] is Utility.ColorPickerCell)
            {
                ColorDialog dlg = new ColorDialog();

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    this.userEditingCell = true;
                    this.valueBeforeEdit = this.Grid[e.ColumnIndex, e.RowIndex].Value;
                    this.Grid[e.ColumnIndex, e.RowIndex].Value = dlg.Color.ToArgb();
                }
            }
            */
        }

        /// <summary>
        /// We need to trap the EditingControlShowing event so that we can tweak all combo box
        /// cells to allow the user to edit the contents.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnEditingControlShowing(object sender, /* TBI DataGridViewEditingControlShowing */ EventArgs e)
        {
            /* TBI
            if (this.Grid.CurrentCell is DataGridViewComboBoxCell)
            {
                DataGridViewComboBoxEditingControl combo = (DataGridViewComboBoxEditingControl)this.Grid.EditingControl;
                combo.DropDownStyle = ComboBoxStyle.DropDown;
            }
            */
        }

        /// <summary>
        /// If the cell being validated is a combo cell then always make sure the cell value 
        /// is in the list of combo items.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnGridCellValidating(object sender, /* TBI DataGridViewCellValidating */ EventArgs e)
        {
            /* 
            if (this.Grid.CurrentCell is DataGridViewComboBoxCell)
            {
                DataGridViewComboBoxEditingControl combo = (DataGridViewComboBoxEditingControl)this.Grid.EditingControl;
                if (combo != null && !combo.Items.Contains(e.FormattedValue))
                {
                    combo.Items.Add(e.FormattedValue);
                }
            }
            */
        }

        /// <summary>
        /// Paste from clipboard into grid.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnPasteFromClipboard(object sender, EventArgs e)
        {
            {
                try
                {
                    /* TBI
                    string text = Clipboard.GetText();
                    string[] lines = text.Split('\n');
                    int rowIndex = this.Grid.CurrentCell.RowIndex;
                    int columnIndex = this.Grid.CurrentCell.ColumnIndex;
                    List<IGridCell> cellsChanged = new List<IGridCell>();
                    if (lines.Length > 0 && this.Grid.CurrentCell.IsInEditMode)
                    {
                        DataGridViewTextBoxEditingControl dText = (DataGridViewTextBoxEditingControl)Grid.EditingControl;
                        dText.Paste(text);
                        cellsChanged.Add(this.GetCell(columnIndex, rowIndex));
                    }
                    else
                    {
                        foreach (string line in lines)
                        {
                            if (rowIndex < this.Grid.RowCount && line.Length > 0)
                            {
                                string[] words = line.Split('\t');
                                for (int i = 0; i < words.GetLength(0); ++i)
                                {
                                    if (columnIndex + i < this.Grid.ColumnCount)
                                    {
                                        DataGridViewCell cell = this.Grid[columnIndex + i, rowIndex];
                                        if (!cell.ReadOnly)
                                        {
                                            if (cell.Value == null || cell.Value.ToString() != words[i])
                                            {
                                                // We are pasting a new value for this cell. Put the new
                                                // value into the cell.
                                                if (words[i] == string.Empty)
                                                {
                                                    cell.Value = null;
                                                }
                                                else
                                                {
                                                    cell.Value = Convert.ChangeType(words[i], this.DataSource.Columns[columnIndex + i].DataType);
                                                }

                                                // Make sure there are enough rows in the data source.
                                                while (this.DataSource.Rows.Count <= rowIndex)
                                                {
                                                    this.DataSource.Rows.Add(this.DataSource.NewRow());
                                                }

                                                // Put the new value into the data source.
                                                if (cell.Value == null)
                                                {
                                                    this.DataSource.Rows[rowIndex][columnIndex + i] = DBNull.Value;
                                                }
                                                else
                                                {
                                                    this.DataSource.Rows[rowIndex][columnIndex + i] = cell.Value;
                                                }

                                                // Put a cell into the cells changed member.
                                                cellsChanged.Add(this.GetCell(columnIndex + i, rowIndex));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                rowIndex++;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    // If some cells were changed then send out an event.
                    if (cellsChanged.Count > 0 && this.CellsChanged != null)
                    {
                        this.CellsChanged.Invoke(this, new GridCellsChangedArgs() { ChangedCells = cellsChanged });
                    }
                    */
                }
                catch (FormatException)
                {
                }
            }
        }


        /// <summary>
        /// Copy to clipboard
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnCopyToClipboard(object sender, EventArgs e)
        {
            /* TBI
            // this.Grid.EndEdit();
            DataObject content = new DataObject();
            if (this.Grid.SelectedCells.Count==1)
            {
                if (this.Grid.CurrentCell.IsInEditMode)
                {
                    if (this.Grid.EditingControl is System.Windows.Forms.TextBox)
                    {
                        string text = ((System.Windows.Forms.TextBox)this.Grid.EditingControl).SelectedText;
                        content.SetText(text);
                    }
                }
                else
                    content.SetText(this.Grid.CurrentCell.Value.ToString());
            }
            else
            content = this.Grid.GetClipboardContent();

            Clipboard.SetDataObject(content);
            */
        }

        /// <summary>
        /// Delete was clicked by the user.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnDeleteClick(object sender, EventArgs e)
        {
            List<IGridCell> cellsChanged = new List<IGridCell>();
            /* TBI
            if (this.Grid.CurrentCell.IsInEditMode)
            {
                DataGridViewTextBoxEditingControl dText = (DataGridViewTextBoxEditingControl)Grid.EditingControl;
                string newText;
                int savedSelectionStart = dText.SelectionStart;
                if (dText.SelectionLength == 0)
                    newText = dText.Text.Remove(dText.SelectionStart, 1);
                else
                    newText = dText.Text.Remove(dText.SelectionStart, dText.SelectionLength);
                dText.Text = newText;
                dText.SelectionStart = savedSelectionStart;
                cellsChanged.Add(this.GetCell(Grid.CurrentCell.ColumnIndex, Grid.CurrentCell.RowIndex));
            }
            else
            {
            foreach (DataGridViewCell cell in this.Grid.SelectedCells)
            {
                // Save change in data source
                if (cell.RowIndex < this.DataSource.Rows.Count)
                {
                    this.DataSource.Rows[cell.RowIndex][cell.ColumnIndex] = DBNull.Value;

                    // Delete cell in grid.
                    this.Grid[cell.ColumnIndex, cell.RowIndex].Value = null;

                    // Put a cell into the cells changed member.
                    cellsChanged.Add(this.GetCell(cell.ColumnIndex, cell.RowIndex));
                }
                }
            }*/

            // If some cells were changed then send out an event.
            if (cellsChanged.Count > 0 && this.CellsChanged != null)
            {
                this.CellsChanged.Invoke(this, new GridCellsChangedArgs() { ChangedCells = cellsChanged });
            }
        }

        private void GridView_Resize(object sender, EventArgs e)
        {
            ResizeControls();
        }

        /// <summary>
        /// User has clicked a cell.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DataGridViewCellEventArgs"/> instance containing the event data.</param>
        private void OnCellContentClick(object sender, /* TBI DataGridViewCell */ EventArgs e)
        {
            /* TBI
            IGridCell cell = this.GetCell(e.ColumnIndex, e.RowIndex);
            if (cell != null && cell.EditorType == EditorTypeEnum.Button)
            {
                GridCellsChangedArgs cellClicked = new GridCellsChangedArgs();
                cellClicked.ChangedCells = new List<IGridCell>();
                cellClicked.ChangedCells.Add(cell);
                ButtonClick(this, cellClicked);
            }
            */
        }

        /// <summary>
        /// This prevents the selection changing when the right mouse button is pressed.
        /// Normally, all we want is to display the popup menu, not change the selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [GLib.ConnectBefore] 
        private void OnButtonDown(object sender, ButtonPressEventArgs e)
        {
            if (e.Event.Button == 3)
            {
                if (this.ColumnHeaderClicked != null)
                {
                    GridHeaderClickedArgs args = new GridHeaderClickedArgs();
                    if (sender is TreeView)
                    {
                        int i = 0;
                        int xpos = (int)e.Event.X;
                        foreach (Widget child in (sender as TreeView).AllChildren)
                        {
                            if (xpos >= child.Allocation.Left && xpos <= child.Allocation.Right)
                                break;
                            i++;
                        }
                        args.Column = this.GetColumn(i);
                    }
                    args.RightClick = true;
                    this.ColumnHeaderClicked.Invoke(this, args);
                }
                Popup.Popup();
                e.RetVal = true;
            }
        }

        /// <summary>
        /// Gets the Label widget rendering the text in the Gtk Button which displays a column header
        /// This is pretty much a hack, but it works. However, it may break in future versions of Gtk.
        /// This assumes that (a) we can get access to the Button widgets via the grid's AllChildren
        /// iterator, and (b) the Button holds an HBox, which holds an Alignment as its first child,
        /// which in turn holds the Label widget
        /// </summary>
        /// <param name="colNo">Column number we are looking for</param>
        public Label GetColumnHeaderLabel(int colNo, TreeView view = null)
        {
            int i = 0;
            if (view == null)
                view = gridview;
            foreach (Widget widget in view.AllChildren)
            {
                if (widget.GetType() != (typeof(Gtk.Button)))
                    continue;
                else if (i++ == colNo)
                {
                    foreach (Widget child in ((Gtk.Button)widget).AllChildren)
                    {
                        if (child.GetType() != (typeof(Gtk.HBox)))
                            continue;
                        foreach (Widget grandChild in ((Gtk.HBox)child).AllChildren)
                        {
                            if (grandChild.GetType() != (typeof(Gtk.Alignment)))
                                continue;
                            foreach (Widget greatGrandChild in ((Gtk.Alignment)grandChild).AllChildren)
                            {
                                if (greatGrandChild.GetType() != (typeof(Gtk.Label)))
                                    continue;
                                else
                                    return greatGrandChild as Label;
                            }
                        }
                    }
                }
            }
            return null;
        }

    }
}
