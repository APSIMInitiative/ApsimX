namespace UserInterface.Views
{
    using Classes;
    using EventArguments;
    using Gtk;
    using Interfaces;
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

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
        /// Flag to keep track of whether a cursor move was initiated internally.
        /// </summary>
        private bool selfCursorMove = false;

        /// <summary>
        /// A value storing the caret location when the user activates intellisense.
        /// </summary>
        private int caretLocation;

        /// <summary>
        /// The box container .
        /// </summary>
        private HBox hboxContainer = null;

        /// <summary>
        /// List of grid model names.
        /// </summary>
        private ListStore gridModel = new ListStore(typeof(string));

        /// <summary>
        /// The dictionary of cell renderers. Maps a cell renderer to a column index.
        /// </summary>
        private Dictionary<CellRenderer, int> colLookup = new Dictionary<CellRenderer, int>();

        /// <summary>
        /// Row index of the selected celll.
        /// </summary>
        private int selectedCellRowIndex = -1;

        /// <summary>
        /// Column index of the selected cell.
        /// </summary>
        private int selectedCellColumnIndex = -1;

        /// <summary>
        /// Index of the last row in the selected cells range.
        /// </summary>
        private int selectionRowMax = -1;

        /// <summary>
        /// Index of the last column in the selected cells range.
        /// </summary>
        private int selectionColMax = -1;

        /// <summary>
        /// The popup menu object.
        /// </summary>
        private Menu popupMenu = new Menu();

        /// <summary>
        /// The key accelerator group.
        /// </summary>
        private AccelGroup accel = new AccelGroup();

        /// <summary>
        /// The cell at the popup locations.
        /// </summary>
        private GridCell popupCell = null;

        /// <summary>
        /// The splitter between the fixed and non-fixed grids.
        /// </summary>
        private HPaned splitter = null;

        /// <summary>
        /// List of "category" row numbers - uneditable rows used as separators.
        /// </summary>
        private List<int> categoryRows = new List<int>();

        /// <summary>
        /// Dictionary for looking up the rendering attributes for each column.
        /// </summary>
        private Dictionary<int, ColRenderAttributes> colAttributes = new Dictionary<int, ColRenderAttributes>();

        /// <summary>
        /// Stores whether our grid is readonly. Internal value.
        /// </summary>
        private bool isReadOnly = false;

        /// <summary>
        /// Number of locked columns.
        /// </summary>
        private int numberLockedCols = 0;

        /// <summary>
        /// The edit control currently in use (if any).
        /// We keep track of this to facilitate handling "partial" edits (e.g., when the user moves to a different component.
        /// </summary>
        private CellEditable editControl = null;

        /// <summary>
        /// The tree path for the row currently being edited.
        /// </summary>
        private string editPath;

        /// <summary>
        /// The widget which sent the EditingStarted event.
        /// </summary>
        private object editSender;

        /// <summary>
        /// The fixed column treeview.
        /// </summary>
        private Gtk.TreeView fixedColView = null;

        /// <summary>
        /// The scrolled window object. This handles scrolling in the gridview.
        /// </summary>
        private ScrolledWindow scrollingWindow = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridView" /> class.
        /// </summary>
        /// <param name="owner">The owning view.</param>
        public GridView(ViewBase owner) : base(owner)
        {
            Builder builder = MasterView.BuilderFromResource("ApsimNG.Resources.Glade.GridView.glade");
            hboxContainer = (HBox)builder.GetObject("hbox1");
            scrollingWindow = (ScrolledWindow)builder.GetObject("scrolledwindow1");
            Grid = (Gtk.TreeView)builder.GetObject("gridview");
            fixedColView = (Gtk.TreeView)builder.GetObject("fixedcolview");
            splitter = (HPaned)builder.GetObject("hpaned1");
            _mainWidget = hboxContainer;
            Grid.Model = gridModel;
            fixedColView.Model = gridModel;
            fixedColView.Selection.Mode = SelectionMode.None;
            popupMenu.AttachToWidget(Grid, null);
            AddContextActionWithAccel("Copy", OnCopy, "Ctrl+C");
            AddContextActionWithAccel("Paste", OnPaste, "Ctrl+V");
            AddContextActionWithAccel("Cut", OnCut, "Ctrl+X");
            AddContextActionWithAccel("Delete", OnDelete, "Delete");
            Grid.ButtonPressEvent += OnButtonDown;
            Grid.Selection.Mode = SelectionMode.None;
            Grid.CursorChanged += OnMoveCursor;
            fixedColView.ButtonPressEvent += OnButtonDown;
            Grid.FocusInEvent += FocusInEvent;
            Grid.FocusOutEvent += FocusOutEvent;
            Grid.KeyPressEvent += GridviewKeyPressEvent;
            Grid.EnableSearch = false;
            Grid.ExposeEvent += GridviewExposed;
            fixedColView.FocusInEvent += FocusInEvent;
            fixedColView.FocusOutEvent += FocusOutEvent;
            fixedColView.EnableSearch = false;
            splitter.Child1.Hide();
            splitter.Child1.NoShowAll = true;
            _mainWidget.Destroyed += MainWidgetDestroyed;
        }

        /// <summary>
        /// Invoked when the user wants to copy a range of cells to the clipboard.
        /// </summary>
        public event EventHandler<GridCellActionArgs> CopyCells;

        /// <summary>
        /// Invoked when the user wants to paste data into a range of cells.
        /// </summary>
        public event EventHandler<GridCellPasteArgs> PasteCells;

        /// <summary>
        /// Invoked when the user wants to delete data from a range of cells.
        /// </summary>
        public event EventHandler<GridCellActionArgs> DeleteCells;

        /// <summary>
        /// This event is invoked when the values of 1 or more cells have changed.
        /// </summary>
        public event EventHandler<GridCellsChangedArgs> CellsChanged;

        /// <summary>
        /// Invoked when a grid cell header is clicked.
        /// </summary>
        public event EventHandler<GridHeaderClickedArgs> ColumnHeaderClicked;

        /// <summary>
        /// Occurs when user clicks a button on the cell.
        /// </summary>
        public event EventHandler<GridCellsChangedArgs> ButtonClick;

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.').
        /// </summary>
        public event EventHandler<NeedContextItemsArgs> ContextItemsNeeded;

        /// <summary>
        /// Gets or sets the treeview object which displays the data.
        /// </summary>
        public Gtk.TreeView Grid { get; set; } = null;

        /// <summary>
        /// List of buttons in the grid.
        /// </summary>
        public List<Tuple<int, int>> ButtonList { get; set; } = new List<Tuple<int, int>>();

        /// <summary>
        /// Dictionary of combobox lookups.
        /// </summary>
        public Dictionary<Tuple<int, int>, ListStore> ComboLookup { get; set; } = new Dictionary<Tuple<int, int>, ListStore>();

        /// <summary>
        /// Gets or sets the number of rows in grid.
        /// </summary>
        public int RowCount
        {
            get
            {
                return gridModel.IterNChildren();
            }
            set
            {
                // The main use of this will be to allow "empty" rows at the bottom of the grid to allow for
                // additional data to be entered (primarily soil profile stuff). 
                if (!CanGrow)
                    throw new Exception("Unable to modify number of rows in grid - this grid cannot change size.");
                if (value > RowCount)
                {
                    // Add new rows
                    for (int i = RowCount; i < value; i++)
                    {
                        gridModel.Append();
                        if (DataSource != null)
                            DataSource.Rows.Add(DataSource.NewRow());
                    }
                }
                else if (value < RowCount)
                {
                    // Remove existing rows. But let's check first to be sure they're empty
                    // TBI
                }

                // TBI this.Grid.RowCount = value;
            }
        }

        /// <summary>
        /// Gets the number of columns in the grid.
        /// </summary>
        public int ColumnCount
        {
            get
            {
                return DataSource.Columns.Count;
            }
        }

        /// <summary>
        /// Gets or sets the numeric grid format e.g. N3
        /// </summary>
        public string NumericFormat { get; set; } = "F2";

        /// <summary>
        /// Gets or sets a value indicating whether the grid is read only.
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
                        foreach (CellRenderer render in col.CellRenderers)
                            if (render is CellRendererText)
                                (render as CellRendererText).Editable = !value;
                }
                isReadOnly = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of rows in grid.
        /// Setting this when <see cref="CanGrow"/> is false will generate an exception.
        /// </summary>
        public bool CanGrow { get; set; } = true;

        /// <summary>
        /// Gets or sets the currently selected cell. Null if none selected.
        /// </summary>
        public IGridCell GetCurrentCell
        {
            get
            {
                TreePath path;
                TreeViewColumn col;
                Grid.GetCursor(out path, out col);
                if (path != null && col != null && col.Cells.Length > 0)
                {
                    int colNo, rowNo;
                    rowNo = path.Indices[0];
                    if (colLookup.TryGetValue(col.Cells[0], out colNo))
                        return GetCell(colNo, rowNo);
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    TreePath row = new TreePath(new int[1] { value.RowIndex });
                    Grid.SetCursor(row, Grid.GetColumn(value.ColumnIndex), false);
                }
            }
        }

        /// <summary>
        /// Gets or sets the data to use to populate the grid.
        /// </summary>
        public DataTable DataSource
        {
            get
            {
                return table;
            }
            set
            {
                table = value;
                LockLeftMostColumns(0);
                PopulateGrid();
            }
        }

        /// <summary>
        /// Sets the contents of a cell being display on a grid.
        /// </summary>
        /// <param name="col">The column.</param>
        /// <param name="cell">The cell.</param>
        /// <param name="model">The tree model.</param>
        /// <param name="iter">The tree iterator.</param>
        public void OnSetCellData(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
        {
            TreePath path = model.GetPath(iter);
            Gtk.TreeView view = col.TreeView as Gtk.TreeView;
            int rowNo = path.Indices[0];
            int colNo = -1;
            string text = string.Empty;
            CellRendererText textRenderer = cell as CellRendererText;
            if (colLookup.TryGetValue(cell, out colNo) && rowNo < DataSource.Rows.Count && colNo < DataSource.Columns.Count)
            {
                StateType cellState = CellIsSelected(rowNo, colNo) ? StateType.Selected : StateType.Normal;
                if (categoryRows.Contains(rowNo))
                {
                    textRenderer.ForegroundGdk = view.Style.Foreground(StateType.Normal);
                    Color separatorColour = Color.LightSteelBlue;
                    cell.CellBackgroundGdk = new Gdk.Color(separatorColour.R, separatorColour.G, separatorColour.B);
                    textRenderer.Editable = false;
                }
                else
                {
                    cell.CellBackgroundGdk = Grid.Style.Base(cellState);
                    textRenderer.ForegroundGdk = Grid.Style.Foreground(cellState);
                    textRenderer.Editable = true;
                }

                if (view == Grid)
                {
                    col.CellRenderers[1].Visible = false;
                    col.CellRenderers[2].Visible = false;
                    col.CellRenderers[3].Visible = false;
                }
                object dataVal = DataSource.Rows[rowNo][colNo];
                Type dataType = dataVal.GetType();
                if (dataType == typeof(DBNull))
                    text = string.Empty;
                else if (NumericFormat != null && ((dataType == typeof(float) && !float.IsNaN((float)dataVal)) ||
                    (dataType == typeof(double) && !double.IsNaN((double)dataVal))))
                    text = string.Format("{0:" + NumericFormat + "}", dataVal);
                else if (dataType == typeof(DateTime))
                    text = string.Format("{0:d}", dataVal);
                else if (view == Grid)
                {
                    // Currently not handling booleans and lists in the "fixed" column grid
                    if (dataType == typeof(bool))
                    {
                        CellRendererToggle toggleRend = col.CellRenderers[1] as CellRendererToggle;
                        if (toggleRend != null)
                        {
                            toggleRend.CellBackgroundGdk = Grid.Style.Base(cellState);
                            toggleRend.Active = (bool)dataVal;
                            toggleRend.Activatable = true;
                            cell.Visible = false;
                            col.CellRenderers[2].Visible = false;
                            toggleRend.Visible = true;
                            return;
                        }
                    }
                    else
                    {   // This assumes that combobox grid cells are based on the "string" type
                        Tuple<int, int> location = new Tuple<int, int>(rowNo, colNo);
                        ListStore store;
                        if (ComboLookup.TryGetValue(location, out store))
                        {
                            CellRendererCombo comboRend = col.CellRenderers[2] as CellRendererCombo;
                            if (comboRend != null)
                            {
                                comboRend.Model = store;
                                comboRend.TextColumn = 0;
                                comboRend.Editable = true;
                                comboRend.HasEntry = false;
                                cell.Visible = false;
                                col.CellRenderers[1].Visible = false;
                                comboRend.Visible = true;
                                comboRend.Text = AsString(dataVal);
                                return;
                            }
                        }
                        if (ButtonList.Contains(location))
                        {
                            CellRendererActiveButton buttonRend = col.CellRenderers[3] as CellRendererActiveButton;
                            if (buttonRend != null)
                            {
                                buttonRend.Visible = true;
                            }
                        }
                        text = AsString(dataVal);
                    }
                }
                else
                {
                    text = AsString(dataVal);
                }
            }

            // We have a "text" cell. Set the text, and other properties for the cell
            cell.Visible = true;
            textRenderer.Text = text;
        }

        /// <summary>
        /// Return a particular cell of the grid.
        /// </summary>
        /// <param name="columnIndex">The column index.</param>
        /// <param name="rowIndex">The row index.</param>
        /// <returns>The cell.</returns>
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
        /// Indicates that a row should be treated as a separator line.
        /// </summary>
        /// <param name="row">The row number.</param>
        /// <param name="isSep">Added as a separator if true; removed as a separator if false.</param>
        public void SetRowAsSeparator(int row, bool isSep = true)
        {
            bool present = categoryRows.Contains(row);
            if (isSep && !present)
                categoryRows.Add(row);
            else if (!isSep && present)
                categoryRows.Remove(row);
        }

        /// <summary>
        /// Sets whether a column is readonly.
        /// </summary>
        /// <param name="col">The column index.</param>
        /// <param name="isReadonly">True if the column should be readonly.</param>
        public void SetColAsReadonly(int col, bool isReadonly)
        {
            ColRenderAttributes colAttr;
            if (colAttributes.TryGetValue(col, out colAttr))
            {
                colAttr.ReadOnly = isReadonly;
            }
        }

        /// <summary>
        /// Returns whether a column is set as readonly.
        /// </summary>
        /// <param name="col">the column number.</param>
        /// <returns>true if readonly.</returns>
        public bool ColIsReadonly(int col)
        {
            ColRenderAttributes colAttr;
            if (colAttributes.TryGetValue(col, out colAttr))
                return colAttr.ReadOnly;
            else
                return false;
        }

        /// <summary>
        /// Sets the normal foreground colour of a column.
        /// </summary>
        /// <param name="col">the column number.</param>
        /// <param name="color">Gdk.Color to be used.</param>
        public void SetColForegroundColor(int col, Gdk.Color color)
        {
            ColRenderAttributes colAttr;
            if (colAttributes.TryGetValue(col, out colAttr))
            {
                colAttr.ForegroundColor = color;
            }
        }

        /// <summary>
        /// Returns the normal foreground colour of a column.
        /// </summary>
        /// <param name="col">the column number.</param>
        /// <returns>Gdk.Color to be used as the foreground colour.</returns>
        public Gdk.Color ColForegroundColor(int col)
        {
            ColRenderAttributes colAttr;
            if (colAttributes.TryGetValue(col, out colAttr))
                return colAttr.ForegroundColor;
            else
                return Grid.Style.Foreground(StateType.Normal);
        }

        /// <summary>
        /// Sets the normal background colour of a column.
        /// </summary>
        /// <param name="col">the column number.</param>
        /// <param name="color">Gdk.Color to be used.</param>
        public void SetColBackgroundColor(int col, Gdk.Color color)
        {
            ColRenderAttributes colAttr;
            if (colAttributes.TryGetValue(col, out colAttr))
            {
                colAttr.BackgroundColor = color;
            }
        }

        /// <summary>
        /// Returns the normal background colour of a column.
        /// </summary>
        /// <param name="col">the column number.</param>
        /// <returns>Gdk.Color to be used as the background colour.</returns>
        public Gdk.Color ColBackgroundColor(int col)
        {
            ColRenderAttributes colAttr;
            if (colAttributes.TryGetValue(col, out colAttr))
                return colAttr.BackgroundColor;
            else
                return Grid.Style.Base(StateType.Normal);
        }

        /// <summary>
        /// Add a separator (on context menu) on the series grid.
        /// </summary>
        public void AddContextSeparator()
        {
            popupMenu.Append(new SeparatorMenuItem());
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="itemName">The name of the item.</param>
        /// <param name="menuItemText">The text of the menu item - may include spaces or other "special" characters (if empty, the itemName is used).</param>
        /// <param name="onClick">The event handler to call when menu is selected.</param>
        /// <param name="active">Indicates whether the option is current selected.</param>
        public void AddContextOption(string itemName, string menuItemText, EventHandler onClick, bool active)
        {
            if (string.IsNullOrEmpty(menuItemText))
                menuItemText = itemName;
            CheckMenuItem item = new CheckMenuItem(menuItemText);
            item.Name = itemName;
            item.DrawAsRadio = true;
            item.Active = active;
            item.Activated += onClick;
            popupMenu.Append(item);
            popupMenu.ShowAll();
        }

        /// <summary>
        /// Clear all presenter defined context items.
        /// </summary>
        public void ClearContextActions()
        {
            while (popupMenu.Children.Length > 3)
                popupMenu.Remove(popupMenu.Children[3]);
        }

        /// <summary>
        /// Returns true if the grid row is empty.
        /// </summary>
        /// <param name="rowIndex">The row index.</param>
        /// <returns>True if the row is empty.</returns>
        public bool RowIsEmpty(int rowIndex)
        {
            // What should we look at here? "DataSource" or "gridModel"
            // They should be synchronized, but....
            // The Windows.Forms version looked at the grid data, so let's do the same here.
            TreeIter iter;
            if (gridModel.IterNthChild(out iter, rowIndex))
            {
                for (int i = 0; i < gridModel.NColumns; i++)
                {
                    string contents = gridModel.GetValue(iter, i) as string;
                    if (!string.IsNullOrEmpty(contents))
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
            if (userEditingCell)
            {
                userEditingCell = false;
                string text = string.Empty;
                string path = string.Empty;
                if (editControl is Entry)
                {
                    text = (editControl as Entry).Text;
                    path = editPath;
                }
                else if (editControl is ComboBox)
                {
                    text = (editControl as ComboBox).ActiveText;
                    path = editPath;
                }
                else if (GetCurrentCell != null)
                {
                    text = GetCurrentCell.Value.ToString();
                    path = GetCurrentCell.RowIndex.ToString();
                }
                else
                    throw new Exception("Unable to finish editing cell.");

                EditedArgs args = new EditedArgs();
                args.Args = new object[2];
                args.Args[0] = path; // Path
                args.Args[1] = text;     // NewText
                OnCellValueChanged(editSender, args);
            }
        }

        /// <summary>
        /// Lock the left most number of columns.
        /// </summary>
        /// <param name="number">The number of columns to be locked.</param>
        public void LockLeftMostColumns(int number)
        {
            // If we've already set this, or if the widgets haven't yet been mapped
            // (we can't determine widths until then), then just save the number of fixed
            // columns we want, so we can try again when the widgets appear on screen
            if ((fixedColView.Visible && number == numberLockedCols) || !Grid.IsMapped)
            {
                numberLockedCols = number;
                return;
            }
            for (int i = 0; i < gridModel.NColumns; i++)
            {
                if (fixedColView.Columns.Length > i)
                    fixedColView.Columns[i].Visible = i < number;
                if (Grid.Columns.Length > i)
                    Grid.Columns[i].Visible = i >= number;
            }
            if (number > 0)
            {
                if (!splitter.Child1.Visible)
                {
                    Grid.Vadjustment.ValueChanged += GridviewVadjustmentChanged;
                    Grid.Selection.Changed += GridviewCursorChanged;
                    fixedColView.Vadjustment.ValueChanged += FixedcolviewVadjustmentChanged;
                    fixedColView.Selection.Changed += FixedcolviewCursorChanged;
                    GridviewCursorChanged(this, EventArgs.Empty);
                    GridviewVadjustmentChanged(this, EventArgs.Empty);
                }
                fixedColView.Model = gridModel;
                fixedColView.Visible = true;
                splitter.Child1.NoShowAll = false;
                splitter.ShowAll();
                splitter.PositionSet = true;
                int splitterWidth = (int)splitter.StyleGetProperty("handle-size");
                if (splitter.Allocation.Width > 1)
                    splitter.Position = Math.Min(fixedColView.SizeRequest().Width + splitterWidth, splitter.Allocation.Width / 2);
                else
                    splitter.Position = fixedColView.SizeRequest().Width + splitterWidth;
            }
            else
            {
                Grid.Vadjustment.ValueChanged -= GridviewVadjustmentChanged;
                Grid.Selection.Changed -= GridviewCursorChanged;
                fixedColView.Vadjustment.ValueChanged -= FixedcolviewVadjustmentChanged;
                fixedColView.Selection.Changed -= FixedcolviewCursorChanged;
                fixedColView.Visible = false;
                splitter.Position = 0;
                splitter.Child1.HideAll();
            }
            numberLockedCols = number;
        }

        /// <summary>
        /// Gets the Gtk Button which displays a column header
        /// This assumes that we can get access to the Button widgets via the grid's AllChildren
        /// iterator.
        /// </summary>
        /// <param name="colNo">Column number we are looking for.</param>
        /// <param name="view">The treeview.</param>
        /// <returns>The button object.</returns>
        public Button GetColumnHeaderButton(int colNo, Gtk.TreeView view = null)
        {
            int i = 0;
            if (view == null)
                view = Grid;
            foreach (Widget widget in view.AllChildren)
            {
                if (widget.GetType() != typeof(Gtk.Button))
                    continue;
                else if (i++ == colNo)
                    return widget as Button;
            }
            return null;
        }

        /// <summary>
        /// Gets the Label widget rendering the text in the Gtk Button which displays a column header
        /// This is pretty much a hack, but it works. However, it may break in future versions of Gtk.
        /// This assumes that (a) we can get access to the Button widgets via the grid's AllChildren
        /// iterator, and (b) the Button holds an HBox, which holds an Alignment as its first child,
        /// which in turn holds the Label widget.
        /// </summary>
        /// <param name="colNo">Column number we are looking for.</param>
        /// <param name="view">The treeview.</param>
        /// <returns>A label object.</returns>
        public Label GetColumnHeaderLabel(int colNo, Gtk.TreeView view = null)
        {
            int i = 0;
            if (view == null)
                view = Grid;
            foreach (Widget widget in view.AllChildren)
            {
                if (widget.GetType() != typeof(Gtk.Button))
                    continue;
                else if (i++ == colNo)
                {
                    foreach (Widget child in ((Gtk.Button)widget).AllChildren)
                    {
                        if (child.GetType() != typeof(Gtk.HBox))
                            continue;
                        foreach (Widget grandChild in ((Gtk.HBox)child).AllChildren)
                        {
                            if (grandChild.GetType() != typeof(Gtk.Alignment))
                                continue;
                            foreach (Widget greatGrandChild in ((Gtk.Alignment)grandChild).AllChildren)
                            {
                                if (greatGrandChild.GetType() != typeof(Gtk.Label))
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

        /// <summary>
        /// Does some cleanup work on the Grid.
        /// </summary>
        public void Dispose()
        {
            if (splitter.Child1.Visible)
            {
                Grid.Vadjustment.ValueChanged -= GridviewVadjustmentChanged;
                Grid.Selection.Changed -= GridviewCursorChanged;
                fixedColView.Vadjustment.ValueChanged -= FixedcolviewVadjustmentChanged;
                fixedColView.Selection.Changed -= FixedcolviewCursorChanged;
            }
            Grid.ButtonPressEvent -= OnButtonDown;
            fixedColView.ButtonPressEvent -= OnButtonDown;
            Grid.FocusInEvent -= FocusInEvent;
            Grid.FocusOutEvent -= FocusOutEvent;
            Grid.KeyPressEvent -= GridviewKeyPressEvent;
            fixedColView.FocusInEvent -= FocusInEvent;
            fixedColView.FocusOutEvent -= FocusOutEvent;
            Grid.ExposeEvent -= GridviewExposed;

            // It's good practice to disconnect the event handlers, as it makes memory leaks
            // less likely. However, we may not "own" the event handlers, so how do we 
            // know what to disconnect?
            // We can do this via reflection. Here's how it currently can be done in Gtk#.
            // Windows.Forms would do it differently.
            // This may break if Gtk# changes the way they implement event handlers.
            foreach (Widget w in popupMenu)
            {
                if (w is MenuItem)
                {
                    PropertyInfo pi = w.GetType().GetProperty("AfterSignals", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (pi != null)
                    {
                        System.Collections.Hashtable handlers = (System.Collections.Hashtable)pi.GetValue(w);
                        if (handlers != null && handlers.ContainsKey("activate"))
                        {
                            EventHandler handler = (EventHandler)handlers["activate"];
                            (w as MenuItem).Activated -= handler;
                        }
                    }
                }
            }
            ClearGridColumns();
            gridModel.Dispose();
            popupMenu.Dispose();
            accel.Dispose();
            if (table != null)
                table.Dispose();
            _mainWidget.Destroyed -= MainWidgetDestroyed;
            _owner = null;
        }

        /// <summary>
        /// Column index of the left-most selected cell.
        /// </summary>
        private int FirstSelectedColumn()
        {
            return selectionColMax >= 0 ? Math.Min(selectionColMax, selectedCellColumnIndex) : selectedCellColumnIndex;
        }

        /// <summary>
        /// Column index of the right-most selected cell.
        /// </summary>
        private int LastSelectedColumn()
        {
            return selectionColMax >= 0 ? Math.Max(selectionColMax, selectedCellColumnIndex) : selectedCellColumnIndex;
        }

        /// <summary>
        /// Row index of the top-most selected cell.
        /// </summary>
        private int FirstSelectedRow()
        {
            return selectionRowMax >= 0 ? Math.Min(selectionRowMax, selectedCellRowIndex) : selectedCellRowIndex;
        }

        /// <summary>
        /// Row index of the bottom-most selected cell.
        /// </summary>
        private int LastSelectedRow()
        {
            return selectionRowMax >= 0 ? Math.Max(selectionRowMax, selectedCellRowIndex) : selectedCellRowIndex;
        }

        /// <summary>
        /// Checks if any cells are selected.
        /// </summary>
        private bool AnyCellIsSelected()
        {
            return selectedCellRowIndex >= 0 && selectedCellColumnIndex >= 0;
        }

        /// <summary>
        /// Checks if a cell is selected.
        /// </summary>
        /// <param name="rowNo">0-indexed row number.</param>
        /// <param name="colNo">0-indexed column number.</param>
        /// <returns>True if the cell is selected. False otherwise.</returns>
        private bool CellIsSelected(int rowNo, int colNo)
        {
            return rowNo >= FirstSelectedRow() && rowNo <= LastSelectedRow() && colNo >= FirstSelectedColumn() && colNo <= LastSelectedColumn();
        }

        /// <summary>
        /// Called when the cursor is moved.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnMoveCursor(object sender, EventArgs args)
        {
            try
            {
                UpdateSelectedCell();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Selects (highlights) the cell which has focus.
        /// </summary>
        private void UpdateSelectedCell()
        {
            while (GLib.MainContext.Iteration()) ;
            TreePath path;
            TreeViewColumn column;
            Grid.GetCursor(out path, out column);
            if (path == null || column == null)
                return;

            int rowIndex = path.Indices[0];
            int colIndex = Array.IndexOf(Grid.Columns, column);

            // If we've clicked on the bottom row of populated data, all cells 
            // below the current cell will also be selected. To overcome this,
            // we add a new row to the datasource. Not a great solution, but 
            // it works.
            if (rowIndex >= DataSource.Rows.Count - 1)
                DataSource.Rows.Add(DataSource.NewRow());
            
            selectedCellRowIndex = rowIndex;
            selectedCellColumnIndex = colIndex;
            selectionColMax = -1;
            selectionRowMax = -1;
            Grid.QueueDraw();
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

        /// <summary>
        /// Removes all grid columns, and cleans up any associated event handlers.
        /// </summary>
        private void ClearGridColumns()
        {
            colAttributes.Clear();
            while (Grid.Columns.Length > 0)
            {
                TreeViewColumn col = Grid.GetColumn(0);
                foreach (CellRenderer render in col.CellRenderers)
                {
                    if (render is CellRendererText)
                    {
                        CellRendererText textRender = render as CellRendererText;
                        textRender.EditingStarted -= OnCellBeginEdit;
                        textRender.EditingCanceled -= TextRenderEditingCanceled;
                        textRender.Edited -= OnCellValueChanged;
                        col.SetCellDataFunc(textRender, (CellLayoutDataFunc)null);
                    }
                    else if (render is CellRendererActiveButton)
                    {
                        (render as CellRendererActiveButton).Toggled -= PixbufRenderToggled;
                    }
                    else if (render is CellRendererToggle)
                    {
                        (render as CellRendererToggle).Toggled -= ToggleRenderToggled;
                        (render as CellRendererToggle).EditingStarted -= OnCellBeginEdit;
                    }
                    else if (render is CellRendererCombo)
                    {
                        (render as CellRendererCombo).Edited -= ComboRenderEdited;
                    }
                    render.Destroy();
                }
                Widget w = col.Widget;
                while (!(w is Button || w == null))
                {
                    w = w.Parent;
                }
                if (w != null)
                    w.ButtonPressEvent -= HeaderClicked;
                Grid.RemoveColumn(col);
            }
            while (fixedColView.Columns.Length > 0)
            {
                TreeViewColumn col = fixedColView.GetColumn(0);
                foreach (CellRenderer render in col.CellRenderers)
                    if (render is CellRendererText)
                    {
                        CellRendererText textRender = render as CellRendererText;
                        textRender.EditingStarted -= OnCellBeginEdit;
                        textRender.EditingCanceled -= TextRenderEditingCanceled;
                        textRender.Edited -= OnCellValueChanged;
                        col.SetCellDataFunc(textRender, (CellLayoutDataFunc)null);
                    }
                Widget w = col.Widget;
                while (!(w is Button || w == null))
                {
                    w = w.Parent;
                }
                if (w != null)
                    w.ButtonPressEvent -= HeaderClicked;
                fixedColView.RemoveColumn(col);
            }
        }

        /// <summary>
        /// Intercepts key press events
        /// The main reason for doing this is to allow the user to move to the "next" cell
        /// when editing, and either the tab or return key is pressed.
        /// </summary>
        /// <param name="o">The calling object.</param>
        /// <param name="args">The event arguments.</param>
        [GLib.ConnectBefore]
        private void GridviewKeyPressEvent(object o, KeyPressEventArgs args)
        {
            try
            {
                string keyName = Gdk.Keyval.Name(args.Event.KeyValue);
                IGridCell cell = GetCurrentCell;
                if (cell == null)
                    return;
                int rowIdx = cell.RowIndex;
                int colIdx = cell.ColumnIndex;

                if (keyName == "ISO_Left_Tab")
                    keyName = "Tab";
                bool shifted = (args.Event.State & Gdk.ModifierType.ShiftMask) != 0;
                if (keyName == "Return" || keyName == "Tab")
                {
                    int nextRow = rowIdx;
                    int numCols = DataSource != null ? DataSource.Columns.Count : 0;
                    int nextCol = colIdx;
                    if (shifted)
                    {
                        // Move backwards
                        do
                        {
                            if (keyName == "Tab")
                            {
                                // Move horizontally
                                if (--nextCol < 0)
                                {
                                    if (--nextRow < 0)
                                        nextRow = RowCount - 1;
                                    nextCol = numCols - 1;
                                }
                            }
                            else if (keyName == "Return")
                            {
                                // Move vertically
                                if (--nextRow < 0)
                                {
                                    if (--nextCol < 0)
                                        nextCol = numCols - 1;
                                    nextRow = RowCount - 1;
                                }
                            }
                        }
                        while (GetColumn(nextCol).ReadOnly || !(new GridCell(this, nextCol, nextRow).EditorType == EditorTypeEnum.TextBox) || categoryRows.Contains(nextRow));
                    }
                    else
                    {
                        do
                        {
                            if (keyName == "Tab")
                            {
                                // Move horizontally
                                if (++nextCol >= numCols)
                                {
                                    if (++nextRow >= RowCount)
                                        nextRow = 0;
                                    nextCol = 0;
                                }
                            }
                            else if (keyName == "Return")
                            {
                                // Move vertically
                                if (++nextRow >= RowCount)
                                {
                                    if (++nextCol >= numCols)
                                        nextCol = 0;
                                    nextRow = 0;
                                }
                            }
                        }
                        while (GetColumn(nextCol).ReadOnly || !(new GridCell(this, nextCol, nextRow).EditorType == EditorTypeEnum.TextBox) || categoryRows.Contains(nextRow));
                    }

                    EndEdit();
                    while (GLib.MainContext.Iteration())
                    {
                    }
                    if (nextRow != rowIdx || nextCol != colIdx)
                    {
                        Grid.SetCursor(new TreePath(new int[1] { nextRow }), Grid.GetColumn(nextCol), userEditingCell);
                        selectedCellRowIndex = nextRow;
                        selectedCellColumnIndex = nextCol;
                        selectionRowMax = -1;
                        selectionColMax = -1;
                    }
                    else
                        Grid.SetCursor(new TreePath(new int[1] { nextRow }), Grid.GetColumn(nextCol), false);

                    while (GLib.MainContext.Iteration()) ;
                    args.RetVal = true;
                }
                else if (!userEditingCell && !GetColumn(colIdx).ReadOnly && IsPrintableChar(args.Event.Key))
                {
                    // Initiate cell editing when user starts typing.
                    Grid.SetCursor(new TreePath(new int[1] { rowIdx }), Grid.GetColumn(colIdx), true);
                    if (new GridCell(this, colIdx, rowIdx).EditorType == EditorTypeEnum.TextBox)
                    {
                        Gdk.EventHelper.Put(args.Event);
                        userEditingCell = true;
                    }
                    args.RetVal = true;
                }
                else if ((char)Gdk.Keyval.ToUnicode(args.Event.KeyValue) == '.')
                {
                    if (ContextItemsNeeded == null)
                        return;

                    NeedContextItemsArgs e = new NeedContextItemsArgs
                    {
                        Coordinates = GetAbsoluteCellPosition(GetCurrentCell.ColumnIndex, GetCurrentCell.RowIndex + 1)
                    };

                    if (editControl is Entry)
                    {
                        Entry editable = editControl as Entry;
                        e.Code = editable.Text;
                        e.Offset = editable.Position;

                        // The cursor position must be calculated before we insert the period.
                        caretLocation = editable.Position;

                        // Due to the intellisense popup (briefly) taking focus, the current cell will usually go out of edit mode
                        // before the period is inserted by the Gtk event handler. Therefore, we insert it manually now, and stop
                        // this signal from propagating further.
                        // editable.Text += ".";
                        editable.InsertText(".", ref caretLocation);
                        editable.Position = caretLocation;
                    }
                    else
                    {
                        // Last resort - if this code ever runs, something has gone wrong.
                        e.Code = GetCurrentCell.Value.ToString();
                        e.Offset = e.Code.Length;
                        caretLocation = 0;
                    }

                    ContextItemsNeeded.Invoke(this, e);

                    // Stop the Gtk signal from propagating any further.
                    args.RetVal = true;
                }
                else if (shifted)
                {
                    bool isArrowKey = args.Event.Key == Gdk.Key.Up || args.Event.Key == Gdk.Key.Down || args.Event.Key == Gdk.Key.Left || args.Event.Key == Gdk.Key.Right;
                    if (isArrowKey)
                    {
                        selectionRowMax = selectionRowMax < 0 ? selectedCellRowIndex : selectionRowMax;
                        selectionColMax = selectionColMax < 0 ? selectedCellColumnIndex : selectionColMax;
                        args.RetVal = true;
                    }
                    if (args.Event.Key == Gdk.Key.Up)
                    {
                        selectionRowMax -= 1;
                    }
                    else if (args.Event.Key == Gdk.Key.Down)
                    {
                        selectionRowMax += 1;
                    }
                    else if (args.Event.Key == Gdk.Key.Left)
                    {
                        selectionColMax -= 1;
                    }
                    else if (args.Event.Key == Gdk.Key.Right)
                    {
                        selectionColMax += 1;
                    }
                    if (isArrowKey)
                        Grid.QueueDraw();
                }
                else if (args.Event.Key == Gdk.Key.Delete)
                {
                    if (DeleteCells == null)
                        throw new Exception("Unable to perform the delete operation - this grid is not owned by a grid presenter! 😠");
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Tests if a <see cref="Gdk.Key"/> is a printable character (e.g. 'a', '3', '#').
        /// </summary>
        /// <param name="chr">Character to be tested.</param>
        /// <returns>True if printable.</returns>
        private bool IsPrintableChar(Gdk.Key chr)
        {
            string keyName = char.ConvertFromUtf32((int)Gdk.Keyval.ToUnicode((uint)chr));
            char c;
            return char.TryParse(keyName, out c) && !char.IsControl(c);
        }
        
        /// <summary>
        /// Calcualtes the number of selected cells.
        /// </summary>
        /// <returns>The number of selected cells.</returns>
        private int NumSelectedCells()
        {
            if (selectionColMax < 0 || selectionRowMax < 0 || selectedCellRowIndex < 0 || selectedCellColumnIndex < 0)
                return 0;
            return (LastSelectedRow() - FirstSelectedRow() + 1) * (LastSelectedColumn() - FirstSelectedColumn() + 1);
        }

        /// <summary>
        /// Calculates the size of a given cell.
        /// Results are returned in a tuple, where Item1 is the width and Item2 is the height of the cell.
        /// </summary>
        /// <param name="col">Column number of the cell.</param>
        /// <param name="row">Row number of the cell.</param>
        /// <returns>The cell size.</returns>
        private Tuple<int, int> GetCellSize(int col, int row)
        {
            int cellHeight, offsetX, offsetY, cellWidth;
            Gdk.Rectangle rectangle = new Gdk.Rectangle();
            TreeViewColumn column = Grid.GetColumn(col);

            // Getting dimensions from TreeViewColumn
            column.CellGetSize(rectangle, out offsetX, out offsetY, out cellWidth, out cellHeight);

            // And now get padding from CellRenderer
            CellRenderer renderer = column.CellRenderers[row];
            cellHeight += (int)renderer.Ypad;
            return new Tuple<int, int>(column.Width, cellHeight);
        }

        /// <summary>
        /// Calculates the XY coordinates of a given cell relative to the origin of the TreeView.
        /// Results are returned in a tuple, where Item1 is the x-coord and Item2 is the y-coord.
        /// </summary>
        /// <param name="col">Column number of the cell.</param>
        /// <param name="row">Row number of the cell.</param>
        /// <returns>The cell position.</returns>
        private Tuple<int, int> GetCellPosition(int col, int row)
        {
            int x = 0;

            for (int i = 0; i < col; i++)
            {
                Tuple<int, int> cellSize = GetCellSize(i, 0);
                x += cellSize.Item1;
            }

            // Rows are uniform in height, so we just get the height of the first cell in the table, 
            // then multiply by the number of rows.
            int y = GetCellSize(0, 0).Item2 * row;

            return new Tuple<int, int>(x, y);
        }

        /// <summary>
        /// Calculates the absolute coordinates of the top-left corner of a given cell on the screen. 
        /// Results are returned in a tuple, where Item1 is the x-coordinate and Item2 is the y-coordinate.
        /// </summary>
        /// <param name="col">Column of the cell.</param>
        /// <param name="row">Row of the cell.</param>
        /// <returns>The absolute cell position.</returns>
        private Tuple<int, int> GetAbsoluteCellPosition(int col, int row)
        {
            int frameX, frameY, containerX, containerY;
            MasterView.MainWindow.GetOrigin(out frameX, out frameY);
            Grid.GdkWindow.GetOrigin(out containerX, out containerY);
            Tuple<int, int> relCoordinates = GetCellPosition(col, row + 1);
            return new Tuple<int, int>(relCoordinates.Item1 + containerX, relCoordinates.Item2 + containerY);
        }

        /// <summary>
        /// Ensure that we save any changes made when the editing control loses focus
        /// Note that we need to handle loss of the editing control's focus, not that
        /// of the gridview overall.
        /// </summary>
        /// <param name="o">The calling object.</param>
        /// <param name="args">The event arguments.</param>
        [GLib.ConnectBefore]
        private void GridViewCellFocusOutEvent(object o, FocusOutEventArgs args)
        {
            try
            {
                EndEdit();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Repsonds to selection changes in the "fixed" columns area by
        /// selecting corresponding rows in the main grid.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event arguments.</param>
        private void FixedcolviewCursorChanged(object sender, EventArgs e)
        {
            try
            {
                if (!selfCursorMove)
                {
                    selfCursorMove = true;
                    TreeSelection fixedSel = fixedColView.Selection;
                    TreePath[] selPaths = fixedSel.GetSelectedRows();

                    TreeSelection gridSel = Grid.Selection;
                    gridSel.UnselectAll();
                    foreach (TreePath path in selPaths)
                        gridSel.SelectPath(path);
                    selfCursorMove = false;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Repsonds to selection changes in the main grid by
        /// selecting corresponding rows in the "fixed columns" grid.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event arguments.</param>
        private void GridviewCursorChanged(object sender, EventArgs e)
        {
            try
            {
                if (fixedColView.Visible && !selfCursorMove)
                {
                    selfCursorMove = true;
                    TreeSelection gridSel = Grid.Selection;
                    TreePath[] selPaths = gridSel.GetSelectedRows();

                    TreeSelection fixedSel = fixedColView.Selection;
                    fixedSel.UnselectAll();
                    foreach (TreePath path in selPaths)
                        fixedSel.SelectPath(path);
                    selfCursorMove = false;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

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
                MasterView.MainWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
            ClearGridColumns();
            fixedColView.Visible = false;
            colLookup.Clear();

            // Begin by creating a new ListStore with the appropriate number of
            // columns. Use the string column type for everything.
            int numCols = DataSource != null ? DataSource.Columns.Count : 0;
            Type[] colTypes = new Type[numCols];
            for (int i = 0; i < numCols; i++)
                colTypes[i] = typeof(string);
            gridModel = new ListStore(colTypes);
            Grid.ModifyBase(StateType.Active, fixedColView.Style.Base(StateType.Selected));
            Grid.ModifyText(StateType.Active, fixedColView.Style.Text(StateType.Selected));
            fixedColView.ModifyBase(StateType.Active, Grid.Style.Base(StateType.Selected));
            fixedColView.ModifyText(StateType.Active, Grid.Style.Text(StateType.Selected));
            
            // Now set up the grid columns
            for (int i = 0; i < numCols; i++)
            {
                ColRenderAttributes attrib = new ColRenderAttributes();
                attrib.ForegroundColor = Grid.Style.Foreground(StateType.Normal);
                attrib.BackgroundColor = Grid.Style.Base(StateType.Normal);
                colAttributes.Add(i, attrib);

                // Design plan: include renderers for text, toggles and combos, but hide all but one of them
                CellRendererText textRender = new CellRendererText();
                CellRendererToggle toggleRender = new CellRendererToggle();
                toggleRender.Visible = false;
                toggleRender.Toggled += ToggleRenderToggled;
                toggleRender.Xalign = 0f;
                toggleRender.EditingCanceled += (sender, e) =>
                {
                    userEditingCell = false;
                };
                CellRendererCombo comboRender = new CellRendererDropDown();
                comboRender.EditingStarted += OnCellBeginEdit;
                comboRender.Edited += ComboRenderEdited;
                comboRender.Visible = false;
                comboRender.EditingStarted += ComboRenderEditing;
                CellRendererActiveButton pixbufRender = new CellRendererActiveButton();
                pixbufRender.Pixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.MenuImages.Save.png");
                pixbufRender.Toggled += PixbufRenderToggled;

                colLookup.Add(textRender, i);

                textRender.FixedHeightFromFont = 1; // 1 line high
                textRender.Editable = !isReadOnly;
                textRender.EditingStarted += OnCellBeginEdit;
                textRender.EditingCanceled += TextRenderEditingCanceled;
                textRender.Edited += OnCellValueChanged;

                TreeViewColumn column = new TreeViewColumn();
                column.Title = DataSource.Columns[i].Caption;
                column.PackStart(textRender, true);     // 0
                column.PackStart(toggleRender, true);   // 1
                column.PackStart(comboRender, true);    // 2
                column.PackStart(pixbufRender, false);  // 3

                column.Sizing = TreeViewColumnSizing.Autosize;

                // column.FixedWidth = 100;
                column.Resizable = true;
                column.SetCellDataFunc(textRender, OnSetCellData);
                Grid.AppendColumn(column);

                // Gtk Treeview doesn't support "frozen" columns, so we fake it by creating a second, identical, TreeView to display
                // the columns we want frozen
                // For now, these frozen columns will be treated as read-only text
                TreeViewColumn fixedColumn = new TreeViewColumn(DataSource.Columns[i].ColumnName, textRender, "text", i);
                fixedColumn.Sizing = TreeViewColumnSizing.Autosize;
                fixedColumn.Resizable = true;
                fixedColumn.SetCellDataFunc(textRender, OnSetCellData);
                fixedColumn.Alignment = 0.5f; // For centered alignment of the column header
                fixedColumn.Visible = false;
                fixedColView.AppendColumn(fixedColumn);
            }
            /*
            if (!isPropertyMode)
            {
                // Add an empty column at the end; auto-sizing will give this any "leftover" space
                TreeViewColumn fillColumn = new TreeViewColumn();
                Grid.AppendColumn(fillColumn);
                fillColumn.Sizing = TreeViewColumnSizing.Autosize;
            }
            */
            int numRows = DataSource != null ? DataSource.Rows.Count : 0;

            Grid.Model = null;
            fixedColView.Model = null;
            for (int row = 0; row <= numRows; row++)
            {
                // We could store data into the grid model, but we don't.
                // Instead, we retrieve the data from our datastore when the OnSetCellData function is called
                gridModel.Append();

                // DataRow dataRow = this.DataSource.Rows[row];
                // gridmodel.AppendValues(dataRow.ItemArray);
            }
            Grid.Model = gridModel;

            SetColumnHeaders(Grid);
            SetColumnHeaders(fixedColView);

            Grid.EnableSearch = false;
            //// gridview.SearchColumn = 0;
            fixedColView.EnableSearch = false;
            //// fixedcolview.SearchColumn = 0;

            UpdateControls();

            if (MasterView.MainWindow != null)
                MasterView.MainWindow.Cursor = null;
        }

        /// <summary>
        /// Event handler for when the user clicks on a column header.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        [GLib.ConnectBefore]
        private void HeaderClicked(object sender, ButtonPressEventArgs e)
        {
            try
            {
                if (e.Event.Button == 1)
                {
                    int columnNumber = GetColNoFromButton(sender as Button);
                    if ((e.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask && selectedCellColumnIndex >= 0)
                    {
                        selectedCellRowIndex = 0;

                        selectionColMax = columnNumber;
                        selectionRowMax = RowCount;
                    }
                    else
                    {
                        selectedCellColumnIndex = columnNumber;
                        selectedCellRowIndex = 0;

                        selectionRowMax = RowCount;
                        selectionColMax = columnNumber;
                    }
                    Grid.QueueDraw();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Get the column number of the column associated with a button.
        /// This is a bit of a hack but it works.
        /// </summary>
        /// <param name="btn">The button control.</param>
        /// <returns>The column number.</returns>
        private int GetColNoFromButton(Button btn)
        {
            int colNo = 0;
            Gtk.TreeView view = btn.Parent as Gtk.TreeView;
            if (view == null)
                return -1;
            foreach (Widget child in view.AllChildren)
            {
                if (child is Button)
                {
                    if (child == btn)
                        break;
                    else
                        colNo++;
                }
            }
            return colNo;
        }

        /// <summary>
        /// Modify the settings of all column headers
        /// We apply center-justification to all the column headers, just for the heck of it
        /// Note that "justification" here refers to justification of wrapped lines, not 
        /// justification of the header as a whole, which is handled with column.Alignment
        /// We create new Labels here, and use markup to make them bold, since other approaches 
        /// don't seem to work consistently.
        /// </summary>
        /// <param name="view">The treeview for which headings are to be modified.</param>
        private void SetColumnHeaders(Gtk.TreeView view)
        {
            int numCols = DataSource != null ? DataSource.Columns.Count : 0;
            for (int i = 0; i < numCols; i++)
            {
                Label newLabel = new Label();
                view.Columns[i].Widget = newLabel;
                newLabel.Wrap = true;
                newLabel.Justify = Justification.Center;
                /*
                if (i == 1 && isPropertyMode)  // Add a tiny bit of extra space when left-aligned
                    (newLabel.Parent as Alignment).LeftPadding = 2;
                */
                newLabel.UseMarkup = true;
                newLabel.Markup = "<b>" + System.Security.SecurityElement.Escape(Grid.Columns[i].Title) + "</b>";
                if (DataSource.Columns[i].Caption != DataSource.Columns[i].ColumnName)
                    newLabel.Parent.Parent.Parent.TooltipText = DataSource.Columns[i].ColumnName;
                newLabel.Show();

                // By default, it's difficult to trap a right-click event on a TreeViewColumn (on the header of a column).
                // TreeViewColumn responds to a click event, but this is not terribly useful, as we have no way of knowing 
                // what type of click triggered the event. To get around this, we respond to the ButtonPressEvent of the 
                // tree's internal button. The button is several parent-levels above the tree's widget (newLabel). 
                // See https://bugzilla.gnome.org/show_bug.cgi?id=141937. This is a bit of a hack, but it works (for now). 
                view.Columns[i].Clickable = true;
                Widget w = view.Columns[i].Widget;
                while (!(w is Button || w == null))
                {
                    w = w.Parent;
                }
                if (w != null)
                    w.ButtonPressEvent += HeaderClicked;
            }
        }

        /// <summary>
        /// Clean up "stuff" when the editing control is closed.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event arguments.</param>
        private void TextRenderEditingCanceled(object sender, EventArgs e)
        {
            try
            {
                userEditingCell = false;
                (editControl as Widget).KeyPressEvent -= GridviewKeyPressEvent;
                (editControl as Widget).FocusOutEvent -= GridViewCellFocusOutEvent;
                editControl = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handle vertical scrolling changes to keep the gridview and fixedcolview at the same scrolled position.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event arguments.</param>
        private void FixedcolviewVadjustmentChanged(object sender, EventArgs e)
        {
            try
            {
                Grid.Vadjustment.Value = fixedColView.Vadjustment.Value;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handle vertical scrolling changes to keep the gridview and fixedcolview at the same scrolled position.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event arguments.</param>
        private void GridviewVadjustmentChanged(object sender, EventArgs e)
        {
            try
            {
                fixedColView.Vadjustment.Value = Grid.Vadjustment.Value;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Inserts text into the current cell at the cursor position.
        /// </summary>
        /// <param name="text">Text to be inserted.</param>
        public void InsertText(string text)
        {
            try
            {
                if (GetCurrentCell == null)
                    return;

                string beforeCaret = GetCurrentCell.Value.ToString().Substring(0, caretLocation);
                string afterCaret = GetCurrentCell.Value.ToString().Substring(caretLocation);

                GetCurrentCell.Value = beforeCaret + text + afterCaret;
                Grid.SetCursor(new TreePath(new int[1] { GetCurrentCell.RowIndex }), Grid.GetColumn(GetCurrentCell.ColumnIndex), true);
                (editControl as Entry).Position = (GetCurrentCell.Value as string).Length;
                while (GLib.MainContext.Iteration())
                {
                }
                valueBeforeEdit = string.Empty;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item.</param>
        /// <param name="onClick">The event handler to call when menu is selected.</param>
        /// <param name="shortcut">The shortcut keys.</param>
        private void AddContextActionWithAccel(string menuItemText, EventHandler onClick, string shortcut)
        {
            ImageMenuItem item = new ImageMenuItem(menuItemText);
            if (!string.IsNullOrEmpty(shortcut))
            {
                string keyName = string.Empty;
                Gdk.ModifierType modifier = Gdk.ModifierType.None;
                string[] keyNames = shortcut.Split(new char[] { '+' });
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
            popupMenu.Append(item);
            popupMenu.ShowAll();
        }

        /// <summary>
        /// Handle loss of focus by removing the accelerators from the popup menu.
        /// </summary>
        /// <param name="o">The calling object.</param>
        /// <param name="args">The event arguments.</param>
        private void FocusOutEvent(object o, FocusOutEventArgs args)
        {
            try
            {
                ((o as Widget).Toplevel as Window).RemoveAccelGroup(accel);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handle receiving focus by adding accelerators for the popup menu.
        /// </summary>
        /// <param name="o">The calling object.</param>
        /// <param name="args">The event arguments.</param>
        private void FocusInEvent(object o, FocusInEventArgs args)
        {
            try
            {
                ((o as Widget).Toplevel as Window).AddAccelGroup(accel);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// User is about to edit a cell.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCellBeginEdit(object sender, EditingStartedArgs e)
        {
            try
            {
                userEditingCell = true;
                editPath = e.Path;
                editControl = e.Editable;
                (editControl as Widget).KeyPressEvent += GridviewKeyPressEvent;
                (editControl as Widget).FocusOutEvent += GridViewCellFocusOutEvent;
                editSender = sender;
                IGridCell where = GetCurrentCell;
                if (where.RowIndex >= DataSource.Rows.Count)
                {
                    for (int i = DataSource.Rows.Count; i <= where.RowIndex; i++)
                    {
                        DataRow row = DataSource.NewRow();
                        DataSource.Rows.Add(row);
                    }
                }
                valueBeforeEdit = DataSource.Rows[where.RowIndex][where.ColumnIndex];
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handle the toggled event.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="r">The event arguments.</param>
        private void ToggleRenderToggled(object sender, ToggledArgs r)
        {
            try
            {
                int colNo = Grid.Columns.ToList().IndexOf(Grid.Columns.FirstOrDefault(c => c.Cells.Contains(sender as CellRenderer)));
                int rowNo = Int32.Parse(r.Path);
                IGridCell where = colNo >= 0 && rowNo >= 0 ? new GridCell(this, colNo, rowNo) : GetCurrentCell;
                while (DataSource != null && where.RowIndex >= DataSource.Rows.Count)
                {
                    DataSource.Rows.Add(DataSource.NewRow());
                }
                object value = DataSource.Rows[where.RowIndex][where.ColumnIndex];
                if (value != null && value != DBNull.Value)
                {
                    DataSource.Rows[where.RowIndex][where.ColumnIndex] = !(bool)DataSource.Rows[where.RowIndex][where.ColumnIndex];
                    if (CellsChanged != null)
                    {
                        GridCellsChangedArgs args = new GridCellsChangedArgs();
                        args.ChangedCells = new List<IGridCell>();
                        args.ChangedCells.Add(GetCell(where.ColumnIndex, where.RowIndex));
                        CellsChanged(this, args);
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
            finally
            {
                userEditingCell = false;
            }
        }

        /// <summary>
        /// Handle the editing event.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event arguments.</param>
        private void ComboRenderEditing(object sender, EditingStartedArgs e)
        {
            try
            {
                (e.Editable as ComboBox).Changed += (o, _) =>
                {
                    IGridCell currentCell = GetCurrentCell;
                    if (currentCell != null)
                        UpdateCellText(currentCell, (o as ComboBox).ActiveText);
                };
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handle the edited event.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event arguments.</param>
        private void ComboRenderEdited(object sender, EditedArgs e)
        {
            try
            {
                UpdateCellText(GetCurrentCell, e.NewText);
                userEditingCell = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Update the text in the cell
        /// </summary>
        /// <param name="where">The cell.</param>
        /// <param name="newText">The new text.</param>
        private void UpdateCellText(IGridCell where, string newText)
        {
            while (DataSource != null && where.RowIndex >= DataSource.Rows.Count)
            {
                DataSource.Rows.Add(DataSource.NewRow());
            }

            // Put the new value into the table on the correct row.
            if (DataSource != null)
            {
                string oldtext = AsString(DataSource.Rows[where.RowIndex][where.ColumnIndex]);
                if (oldtext != newText && newText != null)
                {
                    try
                    {
                        DataSource.Rows[where.RowIndex][where.ColumnIndex] = newText;
                    }
                    catch (Exception)
                    {
                    }

                    if (CellsChanged != null)
                    {
                        GridCellsChangedArgs args = new GridCellsChangedArgs();
                        args.ChangedCells = new List<IGridCell>();
                        args.ChangedCells.Add(GetCell(where.ColumnIndex, where.RowIndex));
                        CellsChanged(this, args);
                    }
                }
            }
        }

        /// <summary>
        /// User has finished editing a cell.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCellValueChanged(object sender, EditedArgs e)
        {
            try
            {
                if (userEditingCell)
                    userEditingCell = false;
                IGridCell where = GetCurrentCell;
                if (where == null)
                    return;
                object oldValue = valueBeforeEdit;

                // Make sure our table has enough rows.
                string newtext = e.NewText;
                object newValue = oldValue;
                bool isInvalid = false;
                if (newtext == null)
                {
                    newValue = DBNull.Value;
                }

                Type dataType = oldValue.GetType();
                if (oldValue == DBNull.Value)
                {
                    if (string.IsNullOrEmpty(newtext))
                        return; // If the old value was null, and we've nothing meaningfull to add, pack up and go home
                    dataType = DataSource.Columns[where.ColumnIndex].DataType;
                }
                if (dataType == typeof(string))
                    newValue = newtext;
                else if (dataType == typeof(double))
                {
                    double numval;
                    if (double.TryParse(newtext, out numval))
                        newValue = numval;
                    else
                    {
                        newValue = double.NaN;
                        isInvalid = true;
                    }
                }
                else if (dataType == typeof(float))
                {
                    float numval;
                    if (float.TryParse(newtext, out numval))
                        newValue = numval;
                    else
                    {
                        newValue = float.NaN;
                        isInvalid = true;
                    }
                }
                else if (dataType == typeof(int))
                {
                    int numval;
                    if (int.TryParse(newtext, out numval))
                        newValue = numval;
                    else
                    {
                        newValue = 0;
                        isInvalid = true;
                    }
                }
                else if (dataType == typeof(DateTime))
                {
                    DateTime dateval;
                    if (!DateTime.TryParse(newtext, out dateval))
                        isInvalid = true;
                    newValue = dateval;
                }

                while (DataSource != null && where.RowIndex >= DataSource.Rows.Count)
                {
                    DataSource.Rows.Add(DataSource.NewRow());
                }

                // Put the new value into the table on the correct row.
                if (DataSource != null)
                {
                    try
                    {
                        DataSource.Rows[where.RowIndex][where.ColumnIndex] = newValue;
                    }
                    catch (Exception)
                    {
                    }
                }

                if (valueBeforeEdit != null && valueBeforeEdit.GetType() == typeof(string) && newValue == null)
                {
                    newValue = string.Empty;
                }

                if (CellsChanged != null && valueBeforeEdit.ToString() != newValue.ToString())
                {
                    GridCellsChangedArgs args = new GridCellsChangedArgs();
                    args.ChangedCells = new List<IGridCell>();
                    args.ChangedCells.Add(GetCell(where.ColumnIndex, where.RowIndex));
                    args.InvalidValue = isInvalid;
                    CellsChanged(this, args);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called to handle any needed changes when the model in changed.
        /// </summary>
        private void UpdateControls()
        {
            if (gridModel.NColumns > 0)
            {
                if (gridModel.IterNChildren() == 0)
                    Grid.Sensitive = false;
                else
                    Grid.Sensitive = true;
            }
            Grid.Show();
        }

        private void OnCut(object sender, EventArgs e)
        {
            try
            {
                OnCopy(this, new EventArgs());
                OnDelete(this, new EventArgs());
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Paste from clipboard into grid.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnPaste(object sender, EventArgs e)
        {
            try
            {
                List<IGridCell> cellsChanged = new List<IGridCell>();
                if (userEditingCell && editControl != null)
                {
                    (editControl as Entry).PasteClipboard();
                    cellsChanged.Add(popupCell);
                }
                else if (PasteCells != null)
                {
                    Clipboard cb = MainWidget.GetClipboard(Gdk.Selection.Clipboard);
                    string text = cb.WaitForText();
                    if (text != null)
                    {
                        string[] lines = text.Split('\n');
                        int numCellsToPaste = 0;
                        lines.ToList().ForEach(line => line.Split('\t').ToList().ForEach(cell => { if (!string.IsNullOrEmpty(cell)) numCellsToPaste++; }));
                        int numReadOnlyColumns = 0;
                        for (int i = FirstSelectedColumn(); i < ColumnCount; i++)
                            if (GetColumn(i).ReadOnly)
                                numReadOnlyColumns++;

                        int numColumnsToPaste = lines[0].Split('\t').Count();
                        int numColumnsAvailable = ColumnCount - FirstSelectedColumn() - numReadOnlyColumns;
                        if (numColumnsToPaste > numColumnsAvailable)
                            throw new Exception(string.Format("Unable to paste {0} column{1} of data into {2} non-readonly column{3}.", numColumnsToPaste, numColumnsToPaste == 1 ? string.Empty : "s", numColumnsAvailable, numColumnsAvailable == 1 ? string.Empty : "s"));
                        int numberSelectedCells = NumSelectedCells();
                        if (numCellsToPaste > numberSelectedCells && numberSelectedCells > 1 && (ResponseType)MasterView.ShowMsgDialog("There's already data here. Do you want to replace it?", "APSIM Next Generation", MessageType.Question, ButtonsType.YesNo) != Gtk.ResponseType.Yes)
                        {
                            // The number of selected cells is less than the number of cells that the user is attempting to paste.
                            // In this scenario, we ask for confirmation before proceeding with the paste operation.
                            return;
                        }
                        GridCell firstCell = new GridCell(this, FirstSelectedColumn(), FirstSelectedRow());
                        GridCell lastCell = new GridCell(this, LastSelectedColumn(), LastSelectedRow());
                        PasteCells.Invoke(this, new GridCellPasteArgs { StartCell = firstCell, EndCell = lastCell, Grid = this, Text = text });
                    }
                }
                else
                {
                    throw new Exception("Unable to paste cells - this view is not handled by a grid presenter.");
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Copies selected data to the clipboard.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCopy(object sender, EventArgs e)
        {
            try
            {
                if (userEditingCell && editControl != null)
                {
                    (editControl as Entry).CopyClipboard();
                }
                else if (CopyCells != null)
                {
                    GridCell firstCell = new GridCell(this, FirstSelectedColumn(), FirstSelectedRow());
                    GridCell lastCell = new GridCell(this, LastSelectedColumn(), LastSelectedRow());
                    CopyCells.Invoke(this, new GridCellActionArgs { StartCell = firstCell, EndCell = lastCell, Grid = this });
                }
                else
                {
                    throw new Exception("Unable to copy cells - the view is not handled by a grid presenter.");
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
            
        }

        /// <summary>
        /// Delete context menu option was clicked by the user.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDelete(object sender, EventArgs e)
        {
            try
            {
                List<IGridCell> cellsChanged = new List<IGridCell>();
                if (userEditingCell && editControl != null)
                {
                    (editControl as Entry).DeleteSelection();
                    cellsChanged.Add(popupCell);
                }
                else if (DeleteCells != null)
                {
                    GridCell firstCell = new GridCell(this, FirstSelectedColumn(), FirstSelectedRow());
                    GridCell lastCell = new GridCell(this, LastSelectedColumn(), LastSelectedRow());
                    DeleteCells.Invoke(this, new GridCellActionArgs { StartCell = firstCell, EndCell = lastCell, Grid = this });
                }
                else if (AnyCellIsSelected())
                    throw new Exception("Unable to delete cells - the view is not handled by a grid presenter.");
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Refreshes the grid.
        /// </summary>
        public void Refresh()
        {
            Grid.QueueDraw();
            fixedColView?.QueueDraw();
        }

        /// <summary>
        /// Refreshes the grid and updates the model.
        /// </summary>
        /// <param name="args"></param>
        public void Refresh(GridCellsChangedArgs args)
        {
            CellsChanged?.Invoke(this, args);
            Refresh();
        }

        /// <summary>
        /// User has clicked a "button".
        /// </summary>
        /// <param name="o">The calling object.</param>
        /// <param name="args">The event arguments.</param>
        private void PixbufRenderToggled(object o, ToggledArgs args)
        {
            try
            {
                IGridCell cell = GetCurrentCell;
                if (cell != null && cell.EditorType == EditorTypeEnum.Button)
                {
                    GridCellsChangedArgs cellClicked = new GridCellsChangedArgs();
                    cellClicked.ChangedCells = new List<IGridCell>();
                    cellClicked.ChangedCells.Add(cell);
                    ButtonClick(this, cellClicked);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Unselects any currently selected cells and selects a new range of cells.
        /// Passing in a null or empty list of cells will deselect all cells.
        /// </summary>
        /// <param name="cells">Cells to be selected.</param>
        public void SelectCells(List<IGridCell> cells)
        {
            try
            {
                if (cells == null || !cells.Any())
                {
                    selectedCellRowIndex = -1;
                    selectedCellColumnIndex = -1;
                    selectionRowMax = -1;
                    selectionColMax = -1;
                }
                else
                {
                    selectedCellRowIndex = cells.Min(cell => cell.RowIndex);
                    selectedCellColumnIndex = cells.Min(cell => cell.ColumnIndex);
                    selectionRowMax = cells.Max(cell => cell.RowIndex);
                    selectionColMax = cells.Max(cell => cell.ColumnIndex);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// This prevents the selection changing when the right mouse button is pressed.
        /// Normally, all we want is to display the popup menu, not change the selection.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event arguments.</param>
        [GLib.ConnectBefore]
        private void OnButtonDown(object sender, ButtonPressEventArgs e)
        {
            try
            {
                Gtk.TreeView view = sender is Gtk.TreeView ? sender as Gtk.TreeView : Grid;
                TreePath path;
                TreeViewColumn column;
                view.GetPathAtPos((int)e.Event.X, (int)e.Event.Y, out path, out column);
                if (e.Event.Button == 1)
                {
                    if (path != null && column != null)
                    {
                        try
                        {
                            if ((e.Event.State & Gdk.ModifierType.ShiftMask) != 0)
                            {
                                selectionColMax = Array.IndexOf(view.Columns, column);
                                selectionRowMax = path.Indices[0];
                                e.RetVal = true;
                                Refresh();
                            }
                            else
                            {
                                int newlySelectedColumnIndex = Array.IndexOf(view.Columns, column);
                                int newlySelectedRowIndex = path.Indices[0];
                                // If the user has clicked on a selected cell, or if they have double clicked on any cell, we start editing the cell.
                                if (!userEditingCell && ((newlySelectedRowIndex == selectedCellRowIndex && newlySelectedColumnIndex == selectedCellColumnIndex) || e.Event.Type == Gdk.EventType.TwoButtonPress))
                                {
                                    EditSelectedCell();
                                    e.RetVal = true;
                                }
                                else
                                {
                                    selectedCellColumnIndex = newlySelectedColumnIndex;
                                    selectedCellRowIndex = newlySelectedRowIndex;
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            ShowError(err);
                        }
                    }
                }
                else if (e.Event.Button == 3)
                {
                    if (ColumnHeaderClicked != null)
                    {
                        GridHeaderClickedArgs args = new GridHeaderClickedArgs();
                        if (sender is Gtk.TreeView)
                        {
                            int rowIdx = path.Indices[0];
                            int xpos = (int)e.Event.X;
                            int colIdx = 0;
                            foreach (Widget child in (sender as Gtk.TreeView).AllChildren)
                            {
                                if (child.GetType() != typeof(Gtk.Button))
                                    continue;
                                if (xpos >= child.Allocation.Left && xpos <= child.Allocation.Right)
                                    break;
                                colIdx++;
                            }
                            args.Column = GetColumn(colIdx);
                            popupCell = new GridCell(this, colIdx, rowIdx);
                        }
                        args.RightClick = true;
                        ColumnHeaderClicked.Invoke(this, args);
                    }
                    if (AnyCellIsSelected())
                        popupMenu.Popup();
                    e.RetVal = true;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Initiates an edit operation on the selected cell.
        /// </summary>
        private void EditSelectedCell()
        {
            if ( !(GetColumn(selectedCellColumnIndex).ReadOnly || categoryRows.Contains(selectedCellRowIndex)) )
            {
                userEditingCell = true;
                Grid.SetCursor(new TreePath(new int[1] { selectedCellRowIndex }), Grid.Columns[selectedCellColumnIndex], true);
            }
        }

        /// <summary>
        /// Called when the sender is first exposed on screen
        /// We may not have been able to set the fixed columns earlier,
        /// but we should be able to now. Once we've done so, we no longer
        /// need to handle this event.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void GridviewExposed(object sender, ExposeEventArgs e)
        {
            try
            {
                if (numberLockedCols > 0)
                    LockLeftMostColumns(numberLockedCols);
                Grid.ExposeEvent -= GridviewExposed;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Returns the string representation of an object. For most objects,
        /// this will be the same as "ToString()", but for Crops, it will give
        /// the crop name.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <returns>The value as a string.</returns>
        private string AsString(object obj)
        {
            string result;
            if (obj is IPlant)
                result = (obj as IModel).Name;
            else
                result = obj.ToString();
            return result;
        }
    }
}
