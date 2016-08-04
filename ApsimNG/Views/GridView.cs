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

        [Widget]
        public TreeView gridview = null;
        [Widget]
        private HBox hbox1 = null;
        [Widget]
        private Gtk.Image image1 = null;

        private Gdk.Pixbuf imagePixbuf;

        private ListStore gridmodel = new ListStore(typeof(string));
        private Dictionary<CellRenderer, int> colLookup = new Dictionary<CellRenderer, int>();
        private Menu Popup = new Menu();


        /// <summary>
        /// Initializes a new instance of the <see cref="GridView" /> class.
        /// </summary>
        public GridView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.GridView.glade", "hbox1");
            gxml.Autoconnect(this);
            _mainWidget = hbox1;
            gridview.Model = gridmodel;
            Popup.AttachToWidget(gridview, null);
            AddContextAction("Copy", OnCopyToClipboard);
            AddContextAction("Paste", OnPasteFromClipboard);
            AddContextAction("Delete", OnDeleteClick);
            gridview.ButtonReleaseEvent += OnButtonUp;
            image1.Visible = false;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            gridview.ButtonReleaseEvent -= OnButtonUp;
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
        }

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
                this.PopulateGrid();
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
        public void AddContextAction(string menuItemText, System.EventHandler onClick)
        {
            ImageMenuItem item = new ImageMenuItem(menuItemText);
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
            /// TBI foreach (DataGridViewColumn column in this.Grid.Columns)
            {
                /// TBI if (this.Grid[column.Index, rowIndex].Value != null)
                {
                    return false;
                }
            }
            /// TBI return true;
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
            /// TBI this.Grid.Columns[number - 1].Frozen = true;
        }

        /// <summary>Get screenshot of grid.</summary>
        /// THIS CODE HAS NOT BEEN TESTED.
        public System.Drawing.Image GetScreenshot()
        {
            // Create a Bitmap and draw the DataGridView on it.
            int width;
            int height;
            Gdk.Window gridWindow = gridview.GdkWindow;
            gridWindow.GetSize(out width, out height);
            Gdk.Pixbuf screenshot = Gdk.Pixbuf.FromDrawable(gridWindow, gridWindow.Colormap, 0, 0, 0, 0, width, height);
            byte[] buffer = screenshot.SaveToBuffer("png");
            MemoryStream stream = new MemoryStream(buffer);
            System.Drawing.Bitmap bitmap = new Bitmap(stream);
            return bitmap;
        }

        /// <summary>
        /// Populate the grid from the DataSource.
        /// </summary>
        private void PopulateGrid()
        {
            ClearGridColumns();
            colLookup.Clear();
            // Begin by creating a new ListStore with the appropriate number of
            // columns. Use the string column type for everything.
            int nCols = DataSource != null ? this.DataSource.Columns.Count : 0;
            Type[] colTypes = new Type[nCols];
            for (int i = 0; i < nCols; i++)
                colTypes[i] = typeof(string);
            gridmodel = new ListStore(colTypes);

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

                TreeViewColumn column = new TreeViewColumn(this.DataSource.Columns[i].ColumnName, textRender, "text", i);
                gridview.AppendColumn(column);
                column.Sizing = TreeViewColumnSizing.Autosize;
                column.Resizable = true;
                column.SetCellDataFunc(textRender, OnSetCellData);
            }

            int nRows = DataSource != null ? this.DataSource.Rows.Count : 0;
            for (int row = 0; row < nRows; row++)
            {
                string[] cells = new string[this.DataSource.Columns.Count];
                // Put everything into the ListStore as a string, but set the actual text
                // on the fly using the SetCellDataFunc callback
                for (int col = 0; col < this.DataSource.Columns.Count; col++)
                    cells[col] = this.DataSource.Rows[row][col].ToString();
                gridmodel.AppendValues(cells);
            }
            gridview.Model = gridmodel;
            gridview.Selection.Mode = SelectionMode.Multiple;
            gridview.ShowAll();
        }

        public void OnSetCellData(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
        {
            TreePath path = model.GetPath(iter);
            int rowNo = path.Indices[0];
            int colNo;
            if (colLookup.TryGetValue(cell, out colNo) && rowNo < this.DataSource.Rows.Count)
            {
                object dataVal = this.DataSource.Rows[rowNo][colNo];
                Type dataType = dataVal.GetType(); //  DataSource.Columns[colNo].DataType;
                string text;
                if ((dataType == typeof(float) && !float.IsNaN((float)dataVal)) ||
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

        private void OnButtonUp(object sender, ButtonReleaseEventArgs e)
        {
            if (e.Event.Button == 3)
                Popup.Popup();
        }

    }
}
