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
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Classes;
    ////using DataGridViewAutoFilter;
    using EventArguments;
    using Gtk;
    using Interfaces;
    using Models.Core;

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
        /// A value indicating whether auto filter (whatever that is) is turned on.
        /// We don't currently use this in the Gtk GUI.
        /// </summary>
        private bool isAutoFilterOn = false;

        /// <summary>
        /// The default numeric format
        /// </summary>
        private string defaultNumericFormat = "F2";

        /// <summary>
        /// Flag to keep track of whether a cursor move was initiated internally
        /// </summary>
        private bool selfCursorMove = false;

        /// <summary>
        /// A value storing the caret location when the user activates intellisense.
        /// </summary>
        private int caretLocation;

        /// <summary>
        /// The box container 
        /// </summary>
        private HBox hbox1 = null;

        /// <summary>
        /// The image object
        /// </summary>
        private Gtk.Image image1 = null;

        /// <summary>
        /// The image pixbuf object
        /// </summary>
        private Gdk.Pixbuf imagePixbuf;

        /// <summary>
        /// List of grid model names
        /// </summary>
        private ListStore gridmodel = new ListStore(typeof(string));

        /// <summary>
        /// The dictionary of cell renderers
        /// </summary>
        private Dictionary<CellRenderer, int> colLookup = new Dictionary<CellRenderer, int>();

        /// <summary>
        /// Dictionary of combobox lookups 
        /// </summary>
        public Dictionary<Tuple<int, int>, ListStore> ComboLookup = new Dictionary<Tuple<int, int>, ListStore>();

        /// <summary>
        /// List of buttons
        /// </summary>
        public List<Tuple<int, int>> ButtonList = new List<Tuple<int, int>>();

        /// <summary>
        /// The popup menu object
        /// </summary>
        private Menu popupMenu = new Menu();

        /// <summary>
        /// The key accelerator group
        /// </summary>
        private AccelGroup accel = new AccelGroup();

        /// <summary>
        /// The cell at the popup locations
        /// </summary>
        private GridCell popupCell = null;

        /// <summary>
        /// The intellisense object
        /// </summary>
        private IntellisenseView intellisense;

        /// <summary>
        /// List of active column indexes
        /// </summary>
        private List<int> activeCol = new List<int>();

        /// <summary>
        /// Is used as a property editor
        /// </summary>
        private bool isPropertyMode = false;

        /// <summary>
        /// Stores whether our grid is readonly. Internal value.
        /// </summary>
        private bool isReadOnly = false;

        /// <summary>
        /// Number of locked columns
        /// </summary>
        private int numberLockedCols = 0;

        /// <summary>
        /// The edit control currently in use (if any).
        /// We keep track of this to facilitate handling "partial" edits (e.g., when the user moves to a different component
        /// </summary>
        private CellEditable editControl = null;

        /// <summary>
        /// The tree path for the row currently being edited
        /// </summary>
        private string editPath;

        /// <summary>
        /// The widget which sent the EditingStarted event
        /// </summary>
        private object editSender;

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

        /// <summary>Invoked when the editor needs context items (after user presses '.')</summary>
        public event EventHandler<NeedContextItemsArgs> ContextItemsNeeded;

        /// <summary>
        /// Invoked when the columns need to be reset to their default colours.
        /// If this event handler is null, the default colours are assumed to be white.
        /// </summary>
        public event EventHandler<EventArgs> FormatColumns;

        /// <summary>
        /// Gets or sets the scrolled window object
        /// </summary>
        public ScrolledWindow ScrolledWindow1 { get; set; } = null;

        /// <summary>
        /// Gets or sets the treeview object
        /// </summary>
        public TreeView Gridview { get; set; } = null;

        /// <summary>
        /// Gets or sets the fixed column treeview
        /// </summary>
        public TreeView FixedColview { get; set; } = null;

        /// <summary>
        /// Gets or sets the name of the associated model.
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
                if (value > RowCount)
                {
                    // Add new rows
                    for (int i = RowCount; i < value; i++)
                        gridmodel.Append(); // Will this suffice?
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
                    foreach (TreeViewColumn col in Gridview.Columns)
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
                Gridview.GetCursor(out path, out col);
                if (path != null && col != null && col.Cells.Length > 0)
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
                    Gridview.SetCursor(row, Gridview.GetColumn(value.ColumnIndex), false);
                }
            }
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
        /// Initializes a new instance of the <see cref="GridView" /> class.
        /// </summary>
        /// <param name="owner">The owning view</param>
        public GridView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.GridView.glade");
            hbox1 = (HBox)builder.GetObject("hbox1");
            ScrolledWindow1 = (ScrolledWindow)builder.GetObject("scrolledwindow1");
            Gridview = (TreeView)builder.GetObject("gridview");
            FixedColview = (TreeView)builder.GetObject("fixedcolview");
            image1 = (Gtk.Image)builder.GetObject("image1");
            _mainWidget = hbox1;
            Gridview.Model = gridmodel;
            Gridview.Selection.Mode = SelectionMode.Multiple;
            FixedColview.Model = gridmodel;
            FixedColview.Selection.Mode = SelectionMode.Multiple;
            popupMenu.AttachToWidget(Gridview, null);
            AddContextActionWithAccel("Copy", OnCopyToClipboard, "Ctrl+C");
            AddContextActionWithAccel("Paste", OnPasteFromClipboard, "Ctrl+V");
            AddContextActionWithAccel("Delete", OnDeleteClick, "Delete");
            Gridview.ButtonPressEvent += OnButtonDown;
            FixedColview.ButtonPressEvent += OnButtonDown;
            Gridview.FocusInEvent += FocusInEvent;
            Gridview.FocusOutEvent += FocusOutEvent;
            Gridview.KeyPressEvent += Gridview_KeyPressEvent;
            Gridview.EnableSearch = false;
            FixedColview.FocusInEvent += FocusInEvent;
            FixedColview.FocusOutEvent += FocusOutEvent;
            FixedColview.EnableSearch = false;
            image1.Pixbuf = null;
            image1.Visible = false;
            _mainWidget.Destroyed += _mainWidget_Destroyed;

            intellisense = new IntellisenseView();
            intellisense.ContextItemsNeeded += ContextItemsNeeded;
            intellisense.ItemSelected += InsertCompletionText;
        }

        /// <summary>
        /// Convert the image
        /// </summary>
        /// <param name="image">The image object to convert</param>
        /// <returns>A Pixbuf type object</returns>
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
        /// Inserts the currently selected text in the intellisense popup into the active grid cell, puts the cell in edit mode, then hides the intellisense popup.
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">The event arguments</param>
        private void InsertCompletionText(object sender, IntellisenseItemSelectedArgs e)
        {
            try
            {
                string beforeCaret = GetCurrentCell.Value.ToString().Substring(0, caretLocation + 1);
                string afterCaret = GetCurrentCell.Value.ToString().Substring(caretLocation + 1);

                GetCurrentCell.Value = beforeCaret + e.ItemSelected + afterCaret;
                Gridview.SetCursor(new TreePath(new int[1] { GetCurrentCell.RowIndex }), Gridview.GetColumn(GetCurrentCell.ColumnIndex), true);
                (editControl as Entry).Position = (GetCurrentCell.Value as string).Length;
                while (GLib.MainContext.Iteration())
                {
                }
                valueBeforeEdit = string.Empty;
            }
            catch (Exception ex)
            {
                var mainView = GetMainViewReference(this);
                if (mainView != null)
                    mainView.ShowMessage(ex.ToString(), Simulation.ErrorLevel.Error);
            }
        }

        /// <summary>
        /// Does cleanup when the main widget is destroyed
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            if (numberLockedCols > 0)
            {
                Gridview.Vadjustment.ValueChanged -= Gridview_Vadjustment_Changed;
                Gridview.Selection.Changed -= Gridview_CursorChanged;
                FixedColview.Vadjustment.ValueChanged -= Fixedcolview_Vadjustment_Changed1;
                FixedColview.Selection.Changed -= Fixedcolview_CursorChanged;
            }
            Gridview.ButtonPressEvent -= OnButtonDown;
            FixedColview.ButtonPressEvent -= OnButtonDown;
            Gridview.FocusInEvent -= FocusInEvent;
            Gridview.FocusOutEvent -= FocusOutEvent;
            Gridview.KeyPressEvent -= Gridview_KeyPressEvent;
            FixedColview.FocusInEvent -= FocusInEvent;
            FixedColview.FocusOutEvent -= FocusOutEvent;

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
            gridmodel.Dispose();
            popupMenu.Dispose();
            accel.Dispose();
            if (imagePixbuf != null)
                imagePixbuf.Dispose();
            if (image1 != null)
                image1.Dispose();
            if (table != null)
                table.Dispose();
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            _owner = null;
        }

        /// <summary>
        /// Removes all grid columns, and cleans up any associated event handlers
        /// </summary>
        private void ClearGridColumns()
        {
            while (Gridview.Columns.Length > 0)
            {
                TreeViewColumn col = Gridview.GetColumn(0);
                foreach (CellRenderer render in col.CellRenderers)
                {
                    if (render is CellRendererText)
                    {
                        CellRendererText textRender = render as CellRendererText;
                        textRender.EditingStarted -= OnCellBeginEdit;
                        textRender.EditingCanceled -= TextRender_EditingCanceled;
                        textRender.Edited -= OnCellValueChanged;
                        col.SetCellDataFunc(textRender, (CellLayoutDataFunc)null);
                    }
                    else if (render is CellRendererActiveButton)
                    {
                        (render as CellRendererActiveButton).Toggled -= PixbufRender_Toggled;
                    }
                    else if (render is CellRendererToggle)
                    {
                        (render as CellRendererToggle).Toggled -= ToggleRender_Toggled;
                    }
                    else if (render is CellRendererCombo)
                    {
                        (render as CellRendererCombo).Edited -= ComboRender_Edited;
                    }
                    render.Destroy();
                }
                Gridview.RemoveColumn(Gridview.GetColumn(0));
            }
            while (FixedColview.Columns.Length > 0)
            {
                TreeViewColumn col = FixedColview.GetColumn(0);
                foreach (CellRenderer render in col.CellRenderers)
                    if (render is CellRendererText)
                    {
                        CellRendererText textRender = render as CellRendererText;
                        textRender.EditingStarted -= OnCellBeginEdit;
                        textRender.EditingCanceled -= TextRender_EditingCanceled;
                        textRender.Edited -= OnCellValueChanged;
                        col.SetCellDataFunc(textRender, (CellLayoutDataFunc)null);
                    }
                FixedColview.RemoveColumn(FixedColview.GetColumn(0));
            }
        }

        /// <summary>
        /// Intercepts key press events
        /// The main reason for doing this is to allow the user to move to the "next" cell
        /// when editing, and either the tab or return key is pressed.
        /// </summary>
        /// <param name="o">The calling object</param>
        /// <param name="args">The event arguments</param>
        [GLib.ConnectBefore]
        private void Gridview_KeyPressEvent(object o, KeyPressEventArgs args)
        {
            string keyName = Gdk.Keyval.Name(args.Event.KeyValue);
            IGridCell cell = GetCurrentCell;
            if (cell == null)
                return;
            int rowIdx = cell.RowIndex;
            int colIdx = cell.ColumnIndex;

            if (keyName == "ISO_Left_Tab")
                keyName = "Tab";
            if (keyName == "Return" || keyName == "Tab")
            {
                if (userEditingCell)
                {
                    bool shifted = (args.Event.State & Gdk.ModifierType.ShiftMask) != 0;
                    int nextRow = rowIdx;
                    int numCols = DataSource != null ? this.DataSource.Columns.Count : 0;
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
                        while (this.GetColumn(nextCol).ReadOnly || !(new GridCell(this, nextCol, nextRow).EditorType == EditorTypeEnum.TextBox));
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
                        while (this.GetColumn(nextCol).ReadOnly || !(new GridCell(this, nextCol, nextRow).EditorType == EditorTypeEnum.TextBox));
                    }

                    EndEdit();
                    while (GLib.MainContext.Iteration())
                    {
                    }
                    if (nextRow != rowIdx || nextCol != colIdx)
                        Gridview.SetCursor(new TreePath(new int[1] { nextRow }), Gridview.GetColumn(nextCol), true);
                    args.RetVal = true;
                }
            }
            else if (!userEditingCell && !GetColumn(colIdx).ReadOnly && (activeCol == null || activeCol.Count < 1) && IsPrintableChar(args.Event.Key))
            {
                // Initiate cell editing when user starts typing.
                Gridview.SetCursor(new TreePath(new int[1] { rowIdx }), Gridview.GetColumn(colIdx), true);
                Gdk.EventHelper.Put(args.Event);
                userEditingCell = true;
                args.RetVal = true;
            }
            else if ((char)Gdk.Keyval.ToUnicode(args.Event.KeyValue) == '.')
            {
                ShowCompletionWindow();
            }
        }

        /// <summary>
        /// Tests if a <see cref="Gdk.Key"/> is a printable character (e.g. 'a', '3', '#').
        /// </summary>
        /// <param name="chr">Character to be tested.</param>
        /// <returns>True if printable</returns>
        private bool IsPrintableChar(Gdk.Key chr)
        {
            string keyName = char.ConvertFromUtf32((int)Gdk.Keyval.ToUnicode((uint)chr));
            char c;
            return char.TryParse(keyName, out c) && !char.IsControl(c);
        }

        /// <summary>
        /// Show the completion window
        /// </summary>
        private void ShowCompletionWindow()
        {
            if (ContextItemsNeeded == null)
                return;
            try
            {
                caretLocation = (editControl as Entry).Position;
                string cellContents = (editControl as Entry).Text;
                string contentsToCursor = cellContents.Substring(0, caretLocation);
                string contentsAfterCursor = cellContents.Substring(caretLocation);
                string node = contentsToCursor.Substring(contentsToCursor.LastIndexOf(',') + 1);

                intellisense.ContextItemsNeeded += this.ContextItemsNeeded;
                if (!intellisense.GenerateAutoCompletionOptions(node))
                    return;

                GetCurrentCell.Value = contentsToCursor + "." + contentsAfterCursor;
                Gridview.SetCursor(new TreePath(new int[1] { GetCurrentCell.RowIndex }), Gridview.Columns[GetCurrentCell.ColumnIndex], true);
                (editControl as Entry).Position = (GetCurrentCell.Value as string).Length;

                // Get the coordinates of the cell 1 row beneath the current one - we don't want the popup to cover the cell we're working on
                Tuple<int, int> coordinates = GetAbsoluteCellPosition(GetCurrentCell.ColumnIndex, GetCurrentCell.RowIndex + 1);
                intellisense.MainWindow = MainWidget.Toplevel as Window;
                intellisense.SmartShowAtCoordinates(coordinates.Item1, coordinates.Item2);

                while (Gtk.Application.EventsPending())
                    Gtk.Application.RunIteration();
            }
            catch (Exception e)
            {
                var mainView = GetMainViewReference(this);
                if (mainView != null)
                    mainView.ShowMessage(e.ToString(), Simulation.ErrorLevel.Error);
            }
        }

        /// <summary>
        /// Finds a reference to the main view, so that error messages can be displayed.
        /// </summary>
        /// <param name="obj">The view object</param>
        /// <returns>Reference to the main view</returns>
        private MainView GetMainViewReference(ViewBase obj)
        {
            if (obj is MainView)
                return obj as MainView;
            if (obj.Owner == null)
                return null;
            return GetMainViewReference(obj.Owner);
        }

        /// <summary>
        /// Calculates the size of a given cell.
        /// Results are returned in a tuple, where Item1 is the width and Item2 is the height of the cell.
        /// </summary>
        /// <param name="col">Column number of the cell.</param>
        /// <param name="row">Row number of the cell.</param>
        /// <returns>The cell size</returns>
        private Tuple<int, int> GetCellSize(int col, int row)
        {
            int cellHeight, offsetX, offsetY, cellWidth;
            Gdk.Rectangle rectangle = new Gdk.Rectangle();
            TreeViewColumn column = Gridview.GetColumn(col);

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
        /// <returns>The cell position</returns>
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
        /// <returns>The absolute cell position</returns>
        private Tuple<int, int> GetAbsoluteCellPosition(int col, int row)
        {
            int frameX, frameY, containerX, containerY;
            mainWindow.GetOrigin(out frameX, out frameY);
            Gridview.GdkWindow.GetOrigin(out containerX, out containerY);
            Tuple<int, int> relCoordinates = GetCellPosition(col, row + 1);
            return new Tuple<int, int>(relCoordinates.Item1 + containerX, relCoordinates.Item2 + containerY);
        }

        /// <summary>
        /// Ensure that we save any changes made when the editing control loses focus
        /// Note that we need to handle loss of the editing control's focus, not that
        /// of the gridview overall
        /// </summary>
        /// <param name="o">The calling object</param>
        /// <param name="args">The event arguments</param>
        [GLib.ConnectBefore]
        private void GridViewCell_FocusOutEvent(object o, FocusOutEventArgs args)
        {
            EndEdit();
        }

        /// <summary>
        /// Repsonds to selection changes in the "fixed" columns area by
        /// selecting corresponding rows in the main grid
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void Fixedcolview_CursorChanged(object sender, EventArgs e)
        {
            if (!selfCursorMove)
            {
                selfCursorMove = true;
                TreeSelection fixedSel = FixedColview.Selection;
                TreePath[] selPaths = fixedSel.GetSelectedRows();

                TreeSelection gridSel = Gridview.Selection;
                gridSel.UnselectAll();
                foreach (TreePath path in selPaths)
                    gridSel.SelectPath(path);
                selfCursorMove = false;
            }
        }

        /// <summary>
        /// Repsonds to selection changes in the main grid by
        /// selecting corresponding rows in the "fixed columns" grid
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void Gridview_CursorChanged(object sender, EventArgs e)
        {
            if (FixedColview.Visible && !selfCursorMove)
            {
                selfCursorMove = true;
                TreeSelection gridSel = Gridview.Selection;
                TreePath[] selPaths = gridSel.GetSelectedRows();

                TreeSelection fixedSel = FixedColview.Selection;
                fixedSel.UnselectAll();
                foreach (TreePath path in selPaths)
                    fixedSel.SelectPath(path);
                selfCursorMove = false;
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
            if (mainWindow != null)
                mainWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
            ClearGridColumns();
            FixedColview.Visible = false;
            colLookup.Clear();

            // Begin by creating a new ListStore with the appropriate number of
            // columns. Use the string column type for everything.
            int numCols = DataSource != null ? this.DataSource.Columns.Count : 0;
            Type[] colTypes = new Type[numCols];
            for (int i = 0; i < numCols; i++)
                colTypes[i] = typeof(string);
            gridmodel = new ListStore(colTypes);
            Gridview.ModifyBase(StateType.Active, FixedColview.Style.Base(StateType.Selected));
            Gridview.ModifyText(StateType.Active, FixedColview.Style.Text(StateType.Selected));
            FixedColview.ModifyBase(StateType.Active, Gridview.Style.Base(StateType.Selected));
            FixedColview.ModifyText(StateType.Active, Gridview.Style.Text(StateType.Selected));

            image1.Visible = false;

            // Now set up the grid columns
            for (int i = 0; i < numCols; i++)
            {
                // Design plan: include renderers for text, toggles and combos, but hide all but one of them
                CellRendererText textRender = new Gtk.CellRendererText();
                CellRendererToggle toggleRender = new Gtk.CellRendererToggle();
                toggleRender.Visible = false;
                toggleRender.Toggled += ToggleRender_Toggled;
                toggleRender.Xalign = ((i == 1) && isPropertyMode) ? 0.0f : 0.5f; // Left of center align, as appropriate
                CellRendererCombo comboRender = new Gtk.CellRendererCombo();
                comboRender.Edited += ComboRender_Edited;
                comboRender.Xalign = ((i == 1) && isPropertyMode) ? 0.0f : 1.0f; // Left or right align, as appropriate
                comboRender.Visible = false;
                comboRender.EditingStarted += ComboRender_Editing;
                CellRendererActiveButton pixbufRender = new CellRendererActiveButton();
                pixbufRender.Pixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.MenuImages.Save.png");
                pixbufRender.Toggled += PixbufRender_Toggled;

                colLookup.Add(textRender, i);

                textRender.FixedHeightFromFont = 1; // 1 line high
                textRender.Editable = !isReadOnly;
                textRender.EditingStarted += OnCellBeginEdit;
                textRender.EditingCanceled += TextRender_EditingCanceled;
                textRender.Edited += OnCellValueChanged;
                textRender.Xalign = ((i == 0) || ((i == 1) && isPropertyMode)) ? 0.0f : 1.0f; // For right alignment of text cell contents; left align the first column

                TreeViewColumn column = new TreeViewColumn();
                column.Title = this.DataSource.Columns[i].Caption;
                column.PackStart(textRender, true);     // 0
                column.PackStart(toggleRender, true);   // 1
                column.PackStart(comboRender, true);    // 2
                column.PackStart(pixbufRender, false);  // 3

                column.Sizing = TreeViewColumnSizing.Autosize;

                // column.FixedWidth = 100;
                column.Resizable = true;
                column.SetCellDataFunc(textRender, OnSetCellData);
                if (i == 1 && isPropertyMode)
                    column.Alignment = 0.0f;
                else
                    column.Alignment = 0.5f; // For centered alignment of the column header
                Gridview.AppendColumn(column);

                // Gtk Treeview doesn't support "frozen" columns, so we fake it by creating a second, identical, TreeView to display
                // the columns we want frozen
                // For now, these frozen columns will be treated as read-only text
                TreeViewColumn fixedColumn = new TreeViewColumn(this.DataSource.Columns[i].ColumnName, textRender, "text", i);
                fixedColumn.Sizing = TreeViewColumnSizing.Autosize;
                fixedColumn.Resizable = true;
                fixedColumn.SetCellDataFunc(textRender, OnSetCellData);
                fixedColumn.Alignment = 0.5f; // For centered alignment of the column header
                fixedColumn.Visible = false;
                FixedColview.AppendColumn(fixedColumn);
            }

            if (!isPropertyMode)
            {
                // Add an empty column at the end; auto-sizing will give this any "leftover" space
                TreeViewColumn fillColumn = new TreeViewColumn();
                Gridview.AppendColumn(fillColumn);
                fillColumn.Sizing = TreeViewColumnSizing.Autosize;
            }

            int numRows = DataSource != null ? this.DataSource.Rows.Count : 0;

            Gridview.Model = null;
            FixedColview.Model = null;
            for (int row = 0; row < numRows; row++)
            {
                // We could store data into the grid model, but we don't.
                // Instead, we retrieve the data from our datastore when the OnSetCellData function is called
                gridmodel.Append();

                // DataRow dataRow = this.DataSource.Rows[row];
                // gridmodel.AppendValues(dataRow.ItemArray);
            }
            Gridview.Model = gridmodel;

            SetColumnHeaders(Gridview);
            SetColumnHeaders(FixedColview);

            Gridview.EnableSearch = false;
            //// gridview.SearchColumn = 0;
            FixedColview.EnableSearch = false;
            //// fixedcolview.SearchColumn = 0;

            Gridview.Show();

            if (mainWindow != null)
                mainWindow.Cursor = null;
        }

        /// <summary>
        /// Event handler for when the user clicks on a column header.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        [GLib.ConnectBefore]
        private void HeaderClicked(object sender, ButtonPressEventArgs e)
        {
            if (activeCol == null)
                activeCol = new List<int>();
            if (e.Event.Button == 1)
            {
                Gridview.Selection.UnselectAll();
                int colNo = GetColNoFromButton(sender as Button);

                if (e.Event.State == Gdk.ModifierType.ShiftMask && activeCol.Count > 0)
                {
                    int closestColumn = activeCol.Aggregate((a, b) => Math.Abs(a - colNo) < Math.Abs(b - colNo) ? a : b);
                    int lowerBound = Math.Min(colNo, closestColumn);
                    int n = Math.Max(colNo, closestColumn) - lowerBound + 1;
                    foreach (int columnIndex in Enumerable.Range(lowerBound, n))
                    {
                        if (!activeCol.Contains(columnIndex))
                            activeCol.Add(columnIndex);
                    }
                }
                else if (e.Event.State == Gdk.ModifierType.ControlMask)
                {
                    if (activeCol.Contains(colNo))
                        activeCol.Remove(colNo);
                    else
                        activeCol.Add(colNo);
                }
                else
                {
                    activeCol = new List<int> { colNo };
                }
                HighlightColumns((sender as Button).Parent as TreeView);
            }
            if (e.Event.Button == 3 && activeCol.Count >= 1)
            {
                Menu headerMenu = new Menu();
                MenuItem copy = new MenuItem("Copy");
                copy.Activated += CopyColumn;
                copy.ShowAll();
                headerMenu.Append(copy);

                MenuItem paste = new MenuItem("Paste");
                paste.Activated += PasteColumn;
                paste.ShowAll();
                headerMenu.Append(paste);

                headerMenu.Popup();
            }
        }

        /// <summary>
        /// Get the column number of the column associated with a button.
        /// This is a bit of a hack but it works.
        /// </summary>
        /// <param name="btn">The button control</param>
        /// <returns>The column number</returns>
        private int GetColNoFromButton(Button btn)
        {
            int colNo = 0;
            TreeView view = btn.Parent as TreeView;
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
        /// Highlights all selected columns and un-highlights all other columns.
        /// </summary>
        private void HighlightColumns(TreeView view)
        {
            // Reset all columns to default colour.
            FormatColumns?.Invoke(this, new EventArgs());

            Gdk.Color backgroundColour, foregroundColour;
            foreach (int n in activeCol)
            {
                try
                {
                    // Attempt to get default highlighting colour from the user's selected theme.
                    backgroundColour = Gridview.Style.Base(StateType.Selected);
                }
                catch
                {
                    // Last resort - use the default highlighting colour in the theme packaged with Apsim.
                    backgroundColour = new Gdk.Color(0, 120, 215);
                }
                foregroundColour = new Gdk.Color(255, 255, 255);

                Gridview.Columns[n].Cells.OfType<CellRendererText>().ToList().ForEach(cell =>
                {
                    cell.BackgroundGdk = backgroundColour;
                    cell.ForegroundGdk = foregroundColour;
                });
            }
            if (FormatColumns == null)
            {
                foreach (int i in Enumerable.Range(0, Gridview.Columns.Length).Where(n => !activeCol.Contains(n)))
                {
                    backgroundColour = Gridview.Style.Base(StateType.Normal);
                    foregroundColour = new Gdk.Color(0, 0, 0);
                    if (i < Gridview.Columns.Length && i >= 0)
                    {
                        Gridview.Columns[i].Cells.OfType<CellRendererText>().ToList().ForEach(cell =>
                        {
                            cell.BackgroundGdk = backgroundColour;
                            cell.ForegroundGdk = foregroundColour;
                        });
                    }
                }
            }

            Gridview.QueueDraw();
            if (view != null)
                view.QueueDraw();
        }

        /// <summary>
        /// Calculates the index of the column which a given x-coordinate falls indside.
        /// </summary>
        /// <param name="x">x-coordinate relative to the <see cref="Gridview"/>.</param>
        /// <returns>The column number</returns>
        private int GetColumn(double x)
        {
            int width = 0;
            int i;
            for (i = 0; i < Gridview.Columns.Length; i++)
            {
                width += Gridview.Columns[i].Width;
                if (width > x)
                    break;
            }
            return i;
        }

        /// <summary>
        /// Copies data from all selected columns to the clipboard.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void CopyColumn(object sender, EventArgs e)
        {
            if (activeCol.Count < 1)
                return;
            activeCol.Sort();
            StringBuilder buffer = new StringBuilder();
            for (int i = 0; i < DataSource.Rows.Count; i++)
            {
                buffer.Append(AsString(DataSource.Rows[i][activeCol[0]]));
                for (int j = 1; j < activeCol.Count; j++)
                {
                    buffer.Append('\t');
                    buffer.Append(AsString(DataSource.Rows[i][activeCol[j]]));
                }
                buffer.Append('\n');
            }
            Clipboard cb = MainWidget.GetClipboard(Gdk.Selection.Clipboard);
            cb.Text = buffer.ToString();
        }

        /// <summary>
        /// Pastes data from the clipboard into the selected column(s).
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void PasteColumn(object sender, EventArgs e)
        {
            if (activeCol == null || activeCol.Count < 1)
                return;

            List<IGridCell> cellsChanged = new List<IGridCell>();
            activeCol.Sort();
            Clipboard cb = MainWidget.GetClipboard(Gdk.Selection.Clipboard);
            string text = cb.WaitForText();
            if (text != null)
            {
                string[] lines = text.Split(Environment.NewLine.ToCharArray());
                string[] firstLine = lines[0].Split('\t');

                // The indices of the columns of the gridview which we will paste the data into.
                List<int> columnIndices = new List<int>();

                if (firstLine.Length == activeCol.Count)
                {
                    columnIndices = activeCol;
                }
                else if (Gridview.Columns.Length - activeCol.Aggregate((x, y) => Math.Min(x, y)) - 1 > firstLine.Length)
                {
                    // Paste into contiguous columns starting at earliest selected column.
                    columnIndices = Enumerable.Range(activeCol.Aggregate((x, y) => Math.Min(x, y)), firstLine.Length).ToList();
                }
                else if (firstLine.Length < Gridview.Columns.Length - 1)
                {
                    // Paste into contiguous columns starting from the first column in the gridview.
                    columnIndices = Enumerable.Range(0, firstLine.Length).ToList();
                }
                else
                {
                    throw new Exception("Unable to paste " + firstLine.Length + " columns of data into a grid with " + Gridview.Columns.Length + " columns.");
                }

                for (int i = 0; i < lines.Length; i++)
                {
                    if (i < this.RowCount && lines[i].Length > 0)
                    {
                        string[] words = lines[i].Split('\t');

                        for (int j = 0; j < columnIndices.Count; j++)
                        {
                            // Make sure there are enough rows in the data source.
                            while (this.DataSource.Rows.Count <= i)
                            {
                                this.DataSource.Rows.Add(this.DataSource.NewRow());
                            }

                            IGridCell cell = this.GetCell(columnIndices[j], i);
                            IGridColumn column = this.GetColumn(columnIndices[j]);
                            if (!column.ReadOnly)
                            {
                                try
                                {
                                    if (cell.Value == null || AsString(cell.Value) != words[j])
                                    {
                                        // We are pasting a new value for this cell. Put the new
                                        // value into the cell.
                                        if (words[j] == string.Empty)
                                        {
                                            cell.Value = DBNull.Value;
                                        }
                                        else
                                        {
                                            cell.Value = Convert.ChangeType(words[j], this.DataSource.Columns[j].DataType);
                                        }

                                        // Put a cell into the cells changed member.
                                        cellsChanged.Add(this.GetCell(columnIndices[j], i));
                                    }
                                }
                                catch (FormatException)
                                {
                                }
                            }
                        }
                    }
                }
            }

            // If some cells were changed then send out an event.
            if (cellsChanged.Count > 0 && this.CellsChanged != null)
            {
                FixedColview.QueueDraw();
                Gridview.QueueDraw();
                this.CellsChanged?.Invoke(this, new GridCellsChangedArgs() { ChangedCells = cellsChanged });
            }
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
        private void SetColumnHeaders(TreeView view)
        {
            int numCols = DataSource != null ? this.DataSource.Columns.Count : 0;
            for (int i = 0; i < numCols; i++)
            {
                Label newLabel = new Label();
                view.Columns[i].Widget = newLabel;
                newLabel.Wrap = true;
                newLabel.Justify = Justification.Center;
                if (i == 1 && isPropertyMode)  // Add a tiny bit of extra space when left-aligned
                    (newLabel.Parent as Alignment).LeftPadding = 2;
                newLabel.UseMarkup = true;
                newLabel.Markup = "<b>" + System.Security.SecurityElement.Escape(Gridview.Columns[i].Title) + "</b>";
                if (this.DataSource.Columns[i].Caption != this.DataSource.Columns[i].ColumnName)
                    newLabel.Parent.Parent.Parent.TooltipText = this.DataSource.Columns[i].ColumnName;
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
        /// Clean up "stuff" when the editing control is closed
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void TextRender_EditingCanceled(object sender, EventArgs e)
        {
            this.userEditingCell = false;
            (this.editControl as Widget).KeyPressEvent -= Gridview_KeyPressEvent;
            (this.editControl as Widget).FocusOutEvent -= GridViewCell_FocusOutEvent;
            this.editControl = null;
        }

        /// <summary>
        /// Handle vertical scrolling changes to keep the gridview and fixedcolview at the same scrolled position
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void Fixedcolview_Vadjustment_Changed1(object sender, EventArgs e)
        {
            Gridview.Vadjustment.Value = FixedColview.Vadjustment.Value;
        }

        /// <summary>
        /// Handle vertical scrolling changes to keep the gridview and fixedcolview at the same scrolled position
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void Gridview_Vadjustment_Changed(object sender, EventArgs e)
        {
            FixedColview.Vadjustment.Value = Gridview.Vadjustment.Value;
        }

        /// <summary>
        /// Sets the contents of a cell being display on a grid
        /// </summary>
        /// <param name="col">The column</param>
        /// <param name="cell">The cell</param>
        /// <param name="model">The tree model</param>
        /// <param name="iter">The tree iterator</param>
        public void OnSetCellData(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
        {
            TreePath path = model.GetPath(iter);
            TreeView view = col.TreeView as TreeView;
            int rowNo = path.Indices[0];
            int colNo;
            string text = string.Empty;
            if (colLookup.TryGetValue(cell, out colNo) && rowNo < this.DataSource.Rows.Count && colNo < this.DataSource.Columns.Count)
            {
                if (view == Gridview)
                {
                    col.CellRenderers[1].Visible = false;
                    col.CellRenderers[2].Visible = false;
                    col.CellRenderers[3].Visible = false;
                }
                object dataVal = this.DataSource.Rows[rowNo][colNo];
                Type dataType = dataVal.GetType();
                if (dataType == typeof(DBNull))
                    text = string.Empty;
                else if (NumericFormat != null && ((dataType == typeof(float) && !float.IsNaN((float)dataVal)) ||
                    (dataType == typeof(double) && !double.IsNaN((double)dataVal))))
                    text = string.Format("{0:" + NumericFormat + "}", dataVal);
                else if (dataType == typeof(DateTime))
                    text = string.Format("{0:d}", dataVal);
                else if (view == Gridview)
                {
                    // Currently not handling booleans and lists in the "fixed" column grid
                    if (dataType == typeof(bool))
                    {
                        CellRendererToggle toggleRend = col.CellRenderers[1] as CellRendererToggle;
                        if (toggleRend != null)
                        {
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
            cell.Visible = true;
            (cell as CellRendererText).Text = text;
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
        /// Returns the string representation of an object. For most objects,
        /// this will be the same as "ToString()", but for Crops, it will give
        /// the crop name
        /// </summary>
        /// <param name="obj">The object to convert</param>
        /// <returns>The value as a string</returns>
        private string AsString(object obj)
        {
            string result;
            if (obj is ICrop)
                result = (obj as IModel).Name;
            else
                result = obj.ToString();
            return result;
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
        /// Add a separator (on context menu) on the series grid.
        /// </summary>
        public void AddContextSeparator()
        {
            popupMenu.Append(new SeparatorMenuItem());
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
            popupMenu.Append(item);
            popupMenu.ShowAll();
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        /// <param name="shortcut">The shortcut keys</param>
        public void AddContextActionWithAccel(string menuItemText, System.EventHandler onClick, string shortcut)
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
        /// Handle loss of focus by removing the accelerators from the popup menu
        /// </summary>
        /// <param name="o">The calling object</param>
        /// <param name="args">The event arguments</param>
        private void FocusOutEvent(object o, FocusOutEventArgs args)
        {
            ((o as Widget).Toplevel as Gtk.Window).RemoveAccelGroup(accel);
        }

        /// <summary>
        /// Handle receiving focus by adding accelerators for the popup menu
        /// </summary>
        /// <param name="o">The calling object</param>
        /// <param name="args">The event arguments</param>
        private void FocusInEvent(object o, FocusInEventArgs args)
        {
            ((o as Widget).Toplevel as Gtk.Window).AddAccelGroup(accel);
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="itemName">The name of the item</param>
        /// <param name="menuItemText">The text of the menu item - may include spaces or other "special" characters (if empty, the itemName is used)</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        /// <param name="active">Indicates whether the option is current selected</param>
        public void AddContextOption(string itemName, string menuItemText, System.EventHandler onClick, bool active)
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
            ScrolledWindow1.HscrollbarPolicy = PolicyType.Never;
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
                ScrolledWindow1.HscrollbarPolicy = PolicyType.Never;
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
                // NB - this assumes that the editing control is a Gtk.Entry control
                // This may change in future versions of Gtk
                if (editControl != null && editControl is Entry)
                {
                    string text = (editControl as Entry).Text;
                    EditedArgs args = new EditedArgs();
                    args.Args = new object[2];
                    args.Args[0] = editPath; // Path
                    args.Args[1] = text;     // NewText
                    OnCellValueChanged(editSender, args);
                }
                else // A fallback procedure
                    ViewBase.SendKeyEvent(Gridview, Gdk.Key.Return);
            }
        }

        /// <summary>
        /// Lock the left most number of columns.
        /// </summary>
        /// <param name="number">The number of columns</param>
        public void LockLeftMostColumns(int number)
        {
            if (number == numberLockedCols || !Gridview.IsMapped)
                return;
            for (int i = 0; i < gridmodel.NColumns; i++)
            {
                if (FixedColview.Columns.Length > i)
                    FixedColview.Columns[i].Visible = i < number;
                if (Gridview.Columns.Length > i)
                    Gridview.Columns[i].Visible = i >= number;
            }
            if (number > 0)
            {
                if (numberLockedCols == 0)
                {
                    Gridview.Vadjustment.ValueChanged += Gridview_Vadjustment_Changed;
                    Gridview.Selection.Changed += Gridview_CursorChanged;
                    FixedColview.Vadjustment.ValueChanged += Fixedcolview_Vadjustment_Changed1;
                    FixedColview.Selection.Changed += Fixedcolview_CursorChanged;
                    Gridview_CursorChanged(this, EventArgs.Empty);
                    Gridview_Vadjustment_Changed(this, EventArgs.Empty);
                }
                FixedColview.Model = gridmodel;
                FixedColview.Visible = true;
            }
            else
            {
                Gridview.Vadjustment.ValueChanged -= Gridview_Vadjustment_Changed;
                Gridview.Selection.Changed -= Gridview_CursorChanged;
                FixedColview.Vadjustment.ValueChanged -= Fixedcolview_Vadjustment_Changed1;
                FixedColview.Selection.Changed -= Fixedcolview_CursorChanged;
                FixedColview.Visible = false;
            }
            numberLockedCols = number;
        }

        /// <summary>
        /// Get screenshot of grid.
        /// </summary>
        /// <returns>A bitmap object</returns>
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
            this.editPath = e.Path;
            this.editControl = e.Editable;
            (this.editControl as Widget).KeyPressEvent += Gridview_KeyPressEvent;
            (this.editControl as Widget).FocusOutEvent += GridViewCell_FocusOutEvent;
            this.editSender = sender;
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
            Type dataType = this.valueBeforeEdit.GetType();
            if (dataType == typeof(DateTime))
            {
                Window dialog = new Window("Select date")
                {
                    TransientFor = Gridview.Toplevel as Window,
                    SkipPagerHint = true,
                    SkipTaskbarHint = true,
                    KeepBelow = true
                };
                dialog.KeyPressEvent += new KeyPressEventHandler((o, eArgs) =>
                {
                    if (eArgs.Event.Key == Gdk.Key.Escape)
                        dialog.Destroy();
                    else if (eArgs.Event.Key == Gdk.Key.Return)
                        GLib.Signal.Emit(o as Calendar, "day-selected-double-click");
                });
                dialog.SetPosition(WindowPosition.None);
                VBox topArea = new VBox();
                topArea.PackStart(new HBox());
                Calendar calendar = new Calendar();
                calendar.DisplayOptions = CalendarDisplayOptions.ShowHeading |
                                     CalendarDisplayOptions.ShowDayNames |
                                     CalendarDisplayOptions.ShowWeekNumbers;
                calendar.Date = (DateTime)this.valueBeforeEdit;

                topArea.PackStart(calendar, true, true, 0);
                dialog.Add(topArea);

                dialog.ShowAll();
                calendar.DaySelectedDoubleClick += (_, __) =>
                {
                    // What SHOULD we do here? For now, assume that if the user modified the date in the calendar dialog,
                    // the resulting date is what they want. Otherwise, keep the text-editing (Entry) widget active, and
                    // let the user enter a value manually.
                    if (calendar.Date != (DateTime)this.valueBeforeEdit)
                    {
                        DateTime date = calendar.GetDate();
                        this.DataSource.Rows[where.RowIndex][where.ColumnIndex] = date;
                        CellRendererText render = sender as CellRendererText;
                        if (render != null)
                        {
                            render.Text = string.Format("{0:d}", date);
                            if (e.Editable is Entry)
                            {
                                (e.Editable as Entry).Text = render.Text;
                                (e.Editable as Entry).Destroy();
                                this.userEditingCell = false;
                                if (this.CellsChanged != null)
                                {
                                    GridCellsChangedArgs args = new GridCellsChangedArgs();
                                    args.ChangedCells = new List<IGridCell>();
                                    args.ChangedCells.Add(this.GetCell(where.ColumnIndex, where.RowIndex));
                                    this.CellsChanged(this, args);
                                }
                            }
                        }
                    }
                    dialog.Destroy();
                    Gridview.QueueDraw();
                };

                // The new dialog will have focus by default. Switch focus back to the main window so that
                // the Entry widget will be immediately editable.
                (Gridview.Toplevel as Window).Present();

                // When the calendar dialog first appears, the Entry widget will briefly lose focus. We don't want
                // the custom focus out event handler below to fire when this occurs, so we process all events in 
                // the events queue before attaching the new event handler.
                while (GLib.MainContext.Iteration())
                {
                }
                (e.Editable as Widget).FocusOutEvent += (_, __) =>
                {
                    // Process all events in the events queue, so we can accurately test if the
                    // calendar has the focus.
                    while (GLib.MainContext.Iteration())
                    {
                    }

                    // If the user clicks on the calendar dialog, the entry widget will lose focus.
                    // Normally we want to remove the calendar dialog when the entry loses focus, but the user might
                    // be a bit annoyed if the calendar disappears when they click on it.
                    if (!(dialog.HasFocus || calendar.HasFocus))
                        dialog.Destroy();
                };
                userEditingCell = true;
            }
        }

        /// <summary>
        /// Handle the toggled event
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="r">The event arguments</param>
        private void ToggleRender_Toggled(object sender, ToggledArgs r)
        {
            IGridCell where = GetCurrentCell;
            while (this.DataSource != null && where.RowIndex >= this.DataSource.Rows.Count)
            {
                this.DataSource.Rows.Add(this.DataSource.NewRow());
            }
            this.DataSource.Rows[where.RowIndex][where.ColumnIndex] = !(bool)this.DataSource.Rows[where.RowIndex][where.ColumnIndex];
            if (this.CellsChanged != null)
            {
                GridCellsChangedArgs args = new GridCellsChangedArgs();
                args.ChangedCells = new List<IGridCell>();
                args.ChangedCells.Add(this.GetCell(where.ColumnIndex, where.RowIndex));
                this.CellsChanged(this, args);
            }
        }

        /// <summary>
        /// Handle the editing event
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void ComboRender_Editing(object sender, EditingStartedArgs e)
        {
            (e.Editable as ComboBox).Changed += (o, _) =>
            {
                UpdateCellText(GetCurrentCell, (o as ComboBox).ActiveText);
            };
        }

        /// <summary>
        /// Handle the edited event
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void ComboRender_Edited(object sender, EditedArgs e)
        {
            UpdateCellText(GetCurrentCell, e.NewText);
        }

        /// <summary>
        /// Update the text in the cell
        /// </summary>
        /// <param name="where">The cell</param>
        /// <param name="newText">The new text</param>
        private void UpdateCellText(IGridCell where, string newText)
        {
            while (this.DataSource != null && where.RowIndex >= this.DataSource.Rows.Count)
            {
                this.DataSource.Rows.Add(this.DataSource.NewRow());
            }

            // Put the new value into the table on the correct row.
            if (this.DataSource != null)
            {
                string oldtext = AsString(this.DataSource.Rows[where.RowIndex][where.ColumnIndex]);
                if (oldtext != newText && newText != null)
                {
                    try
                    {
                        this.DataSource.Rows[where.RowIndex][where.ColumnIndex] = newText;
                    }
                    catch (Exception)
                    {
                    }

                    if (this.CellsChanged != null)
                    {
                        GridCellsChangedArgs args = new GridCellsChangedArgs();
                        args.ChangedCells = new List<IGridCell>();
                        args.ChangedCells.Add(this.GetCell(where.ColumnIndex, where.RowIndex));
                        this.CellsChanged(this, args);
                    }
                }
            }
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
                if (where == null)
                    return;

                object oldValue = this.valueBeforeEdit;

                this.userEditingCell = false;

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
                    dataType = this.DataSource.Columns[where.ColumnIndex].DataType;
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

                if (this.CellsChanged != null && this.valueBeforeEdit.ToString() != newValue.ToString())
                {
                    GridCellsChangedArgs args = new GridCellsChangedArgs();
                    args.ChangedCells = new List<IGridCell>();
                    args.ChangedCells.Add(this.GetCell(where.ColumnIndex, where.RowIndex));
                    args.invalidValue = isInvalid;
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
                Gridview.Visible = false;
            }
            else
                Gridview.Visible = true;
        }

        /// <summary>
        /// Trap any grid data errors, usually as a result of cell values not being
        /// in combo boxes. We'll handle these elsewhere.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnDataError(object sender, /* TBI DataGridViewDataError */ EventArgs e)
        {
            // TBI e.Cancel = true;
        }

        /// <summary>
        /// User has clicked a cell. 
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnCellMouseDown(object sender, /* TBI DataGridViewCellMouse */ EventArgs e)
        {
            // Probably not needed in the Gtk implementation
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
            // Probably not needed in the Gtk implementation
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
            // Probably not needed in the Gtk implementation
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
                List<IGridCell> cellsChanged = new List<IGridCell>();
                int rowIndex = popupCell.RowIndex;
                int columnIndex = popupCell.ColumnIndex;
                if (this.userEditingCell && this.editControl != null)
                {
                    (editControl as Entry).PasteClipboard();
                    cellsChanged.Add(popupCell);
                }
                else
                {
                    Clipboard cb = MainWidget.GetClipboard(Gdk.Selection.Clipboard);
                    string text = cb.WaitForText();
                    if (text != null)
                    {
                        string[] lines = text.Split('\n');
                        foreach (string line in lines)
                        {
                            if (rowIndex < this.RowCount && line.Length > 0)
                            {
                                string[] words = line.Split('\t');
                                for (int i = 0; i < words.GetLength(0); ++i)
                                {
                                    if (columnIndex + i < this.DataSource.Columns.Count)
                                    {
                                        // Make sure there are enough rows in the data source.
                                        while (this.DataSource.Rows.Count <= rowIndex)
                                        {
                                            this.DataSource.Rows.Add(this.DataSource.NewRow());
                                        }

                                        IGridCell cell = this.GetCell(columnIndex + i, rowIndex);
                                        IGridColumn column = this.GetColumn(columnIndex + i);
                                        if (!column.ReadOnly)
                                        {
                                            try
                                            {
                                                if (cell.Value == null || AsString(cell.Value) != words[i])
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

                                                    // Put a cell into the cells changed member.
                                                    cellsChanged.Add(this.GetCell(columnIndex + i, rowIndex));
                                                }
                                            }
                                            catch (FormatException)
                                            {
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
                }

                // If some cells were changed then send out an event.
                if (cellsChanged.Count > 0 && this.CellsChanged != null)
                {
                    FixedColview.QueueDraw();
                    Gridview.QueueDraw();
                    this.CellsChanged.Invoke(this, new GridCellsChangedArgs() { ChangedCells = cellsChanged });
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
            if (this.userEditingCell && this.editControl != null)
            {
                (editControl as Entry).CopyClipboard();
            }
            else
            {
                TreeSelection selection = Gridview.Selection;
                if (selection.CountSelectedRows() > 0)
                {
                    StringBuilder buffer = new StringBuilder();
                    int numCols = DataSource != null ? this.DataSource.Columns.Count : 0;
                    TreePath[] selRows = selection.GetSelectedRows();
                    foreach (TreePath row in selRows)
                    {
                        int rowIdx = row.Indices[0];
                        for (int colIdx = 0; colIdx < numCols; colIdx++)
                        {
                            object dataVal = this.DataSource.Rows[rowIdx][colIdx];
                            buffer.Append(AsString(dataVal));
                            if (colIdx == numCols - 1)
                                buffer.Append('\n');
                            else
                                buffer.Append('\t');
                        }
                    }
                    Clipboard cb = MainWidget.GetClipboard(Gdk.Selection.Clipboard);
                    cb.Text = buffer.ToString();
                }
            }
        }

        /// <summary>
        /// Delete was clicked by the user.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnDeleteClick(object sender, EventArgs e)
        {
            List<IGridCell> cellsChanged = new List<IGridCell>();
            if (this.userEditingCell && this.editControl != null)
            {
                (editControl as Entry).DeleteSelection();
                cellsChanged.Add(popupCell);
            }
            else
            {
                TreeSelection selection = Gridview.Selection;
                if (selection.CountSelectedRows() > 0)
                {
                    int numCols = DataSource != null ? this.DataSource.Columns.Count : 0;
                    TreePath[] selRows = selection.GetSelectedRows();
                    foreach (TreePath row in selRows)
                    {
                        int rowIdx = row.Indices[0];
                        for (int colIdx = 0; colIdx < numCols; colIdx++)
                        {
                            if (!this.GetColumn(colIdx).ReadOnly)
                            {
                                DataSource.Rows[rowIdx][colIdx] = DBNull.Value;
                                cellsChanged.Add(this.GetCell(colIdx, rowIdx));
                            }
                        }
                    }
                }
            }

            // If some cells were changed then send out an event.
            if (cellsChanged.Count > 0 && this.CellsChanged != null)
            {
                FixedColview.QueueDraw();
                Gridview.QueueDraw();
                this.CellsChanged.Invoke(this, new GridCellsChangedArgs() { ChangedCells = cellsChanged });
            }
        }

        /// <summary>
        /// Handle the resize event
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void GridView_Resize(object sender, EventArgs e)
        {
            ResizeControls();
        }

        /// <summary>
        /// User has clicked a "button".
        /// </summary>
        /// <param name="o">The calling object</param>
        /// <param name="args">The event arguments</param>
        private void PixbufRender_Toggled(object o, ToggledArgs args)
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

        /// <summary>
        /// This prevents the selection changing when the right mouse button is pressed.
        /// Normally, all we want is to display the popup menu, not change the selection.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [GLib.ConnectBefore]
        private void OnButtonDown(object sender, ButtonPressEventArgs e)
        {
            activeCol = new List<int>();
            TreeView view = sender is TreeView ? sender as TreeView : Gridview;
            HighlightColumns(view);
            if (e.Event.Button == 3)
            {
                if (this.ColumnHeaderClicked != null)
                {
                    GridHeaderClickedArgs args = new GridHeaderClickedArgs();
                    if (sender is TreeView)
                    {
                        TreePath path;
                        TreeViewColumn column;
                        Gridview.GetPathAtPos((int)e.Event.X, (int)e.Event.Y, out path, out column);
                        int rowIdx = path.Indices[0];
                        int xpos = (int)e.Event.X;
                        int colIdx = 0;
                        foreach (Widget child in (sender as TreeView).AllChildren)
                        {
                            if (child.GetType() != typeof(Gtk.Button))
                                continue;
                            if (xpos >= child.Allocation.Left && xpos <= child.Allocation.Right)
                                break;
                            colIdx++;
                        }
                        args.Column = this.GetColumn(colIdx);
                        popupCell = new GridCell(this, colIdx, rowIdx);
                    }
                    args.RightClick = true;
                    this.ColumnHeaderClicked.Invoke(this, args);
                }
                popupMenu.Popup();
                e.RetVal = true;
            }
        }

        /// <summary>
        /// Gets the Gtk Button which displays a column header
        /// This assumes that we can get access to the Button widgets via the grid's AllChildren
        /// iterator.
        /// </summary>
        /// <param name="colNo">Column number we are looking for</param>
        /// <param name="view">The treeview</param>
        /// <returns>The button object</returns>
        public Button GetColumnHeaderButton(int colNo, TreeView view = null)
        {
            int i = 0;
            if (view == null)
                view = Gridview;
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
        /// which in turn holds the Label widget
        /// </summary>
        /// <param name="colNo">Column number we are looking for</param>
        /// <param name="view">The treeview</param>
        /// <returns>A label object</returns>
        public Label GetColumnHeaderLabel(int colNo, TreeView view = null)
        {
            int i = 0;
            if (view == null)
                view = Gridview;
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
    }

    /// <summary>
    /// We want to have a "button" we can press within a grid cell. We could use a Gtk CellRendererPixbuf for this, 
    /// but that doesn't provide an easy way to detect a button press, so instead we can use a "toggle", but 
    /// override the Render function to simply display our Pixbuf
    /// </summary>
    public class CellRendererActiveButton : CellRendererToggle
    {
        /// <summary>
        /// Gets or sets the pixbuf object
        /// </summary>
        public Gdk.Pixbuf Pixbuf { get; set; }

        /// <summary>
        /// Render the cell in the window
        /// </summary>
        /// <param name="window">The owning window</param>
        /// <param name="widget">The widget</param>
        /// <param name="background_area">Background area</param>
        /// <param name="cell_area">The cell area</param>
        /// <param name="expose_area">Expose the area</param>
        /// <param name="flags">Render flags</param>
        protected override void Render(Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
        {
            Gdk.GC gc = new Gdk.GC(window);
            window.DrawPixbuf(gc, Pixbuf, 0, 0, cell_area.X, cell_area.Y, Pixbuf.Width, Pixbuf.Height, Gdk.RgbDither.Normal, 0, 0);
        }
    }
}
