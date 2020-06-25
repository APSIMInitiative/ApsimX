namespace UserInterface.Views
{
    using Gtk;
    using Pango;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    /// <summary>An interface for a list view</summary>
    public interface IListView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary>Get or sets the datasource for the view.</summary>
        DataTable DataSource { get; set; }

        /// <summary>Gets or sets the selected row in the data source.</summary>
        int[] SelectedRows { get; set; }

        /// <summary>Sets the render details for particular cells.</summary>
        List<CellRendererDescription> CellRenderDetails { get; set; }
    }

    /// <summary>Render details for a cell.</summary>
    public class CellRendererDescription
    {
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public bool StrikeThrough { get; set; }
        public bool Bold { get; set; }
        public bool Italics { get; set; }
        public System.Drawing.Color Colour { get; set; }
    }

    /// <summary>A list view.</summary>
    public class ListView : ViewBase, IListView
    {
        /// <summary>The main tree view control.</summary>
        private Gtk.TreeView tree;

        /// <summary>The data table used to populate the tree view.</summary>
        private DataTable table;

        /// <summary>The columns of the tree view.</summary>
        private List<TreeViewColumn> columns = new List<TreeViewColumn>();

        /// <summary>The cells of the tree view.</summary>
        private List<CellRendererText> cells = new List<CellRendererText>();

        /// <summary>The popup context menu.</summary>
        private Gtk.Menu contextMenu;

        /// <summary>Constructor</summary>
        public ListView(ViewBase owner, Gtk.TreeView treeView, Gtk.Menu menu = null) : base(owner)
        {
            tree = treeView;
            mainWidget = tree;
            tree.ButtonReleaseEvent += OnTreeClicked;
            mainWidget.Destroyed += OnMainWidgetDestroyed;
            contextMenu = menu;
        }

        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>Get or sets the datasource for the view.</summary>
        public DataTable DataSource
        {
            get
            {
                return table;
            }
            set
            {
                table = value;
                PopulateTreeView();
            }
        }

        /// <summary>Gets or sets the selected row in the data source.</summary>
        public int[] SelectedRows
        {
            get
            {
                var indexes = new List<int>();
                var selectedRows = tree.Selection.GetSelectedRows();
                foreach (var row in selectedRows)
                    indexes.Add(row.Indices[0]);
                return indexes.ToArray();
            }
            set
            {
                foreach (var rowIndex in value)
                    tree.Selection.SelectPath(new TreePath(new int[] { rowIndex }));
            }
        }

        /// <summary>Sets the render details for particular cells.</summary>
        public List<CellRendererDescription> CellRenderDetails { get; set; }

        /// <summary>The main widget has been destroyed.</summary>
        private void OnMainWidgetDestroyed(object sender, EventArgs e)
        {
            try
            {
                mainWidget.Destroyed -= OnMainWidgetDestroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Populate the tree view.</summary>
        private void PopulateTreeView()
        {
            var types = new Type[table.Columns.Count];
            columns = new List<TreeViewColumn>();
            cells = new List<CellRendererText>();

            // Clear all columns from tree.
            while (tree.Columns.Length > 0)
                tree.RemoveColumn(tree.Columns[0]);

            // initialise column headers            
            for (int i = 0; i < table.Columns.Count; i++)
            {
                types[i] = typeof(string);
                var cell = new CellRendererText();
                cells.Add(cell);
                columns.Add(new TreeViewColumn { Title = table.Columns[i].ColumnName, Resizable = true, Sizing = TreeViewColumnSizing.GrowOnly });
                columns[i].PackStart(cells[i], false);
                columns[i].AddAttribute(cells[i], "text", i);
                columns[i].AddNotification("width", OnColumnWidthChange);
                columns[i].SetCellDataFunc(cell, OnFormatColumn);
                tree.AppendColumn(columns[i]);
            }

            var store = new ListStore(types);
            tree.Model = store;
            tree.Selection.Mode = SelectionMode.Multiple;
            tree.RubberBanding = true;
            tree.CanFocus = true;

            // Populate with rows.
            store.Clear();
            foreach (DataRow row in table.Rows)
                store.AppendValues(row.ItemArray);
        }

        /// <summary>
        /// Invoked for every cell in grid.
        /// </summary>
        /// <param name="col">The column.</param>
        /// <param name="cell">The cell.</param>
        /// <param name="model">The tree model.</param>
        /// <param name="iter">The tree iterator.</param>
        public void OnFormatColumn(TreeViewColumn col, CellRenderer baseCell, TreeModel model, TreeIter iter)
        {
            try
            {
                TreePath path = tree.Model.GetPath(iter);
                int rowIndex = path.Indices[0];
                int colIndex = DataSource.Columns.IndexOf(col.Title);

                CellRendererDescription renderDetails = CellRenderDetails.Find(c => c.ColumnIndex == colIndex && c.RowIndex == rowIndex);
                if (renderDetails != null)
                {
                    var cell = baseCell as CellRendererText;
                    cell.Strikethrough = renderDetails.StrikeThrough;
                    if (renderDetails.Bold)
                        cell.Weight = (int) Pango.Weight.Bold;
                    if (renderDetails.Italics)
                        cell.Font = "Sans Italic";
                    cell.Foreground = renderDetails.Colour.Name; // ?
                }
                else if (baseCell is CellRendererText)
                {
                    var cell = baseCell as CellRendererText;
                    cell.Strikethrough = false;
                    cell.Weight = (int)Pango.Weight.Normal;
                    cell.Font = "Normal";
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>The grid has readjusted the column widths.</summary>
        private void OnColumnWidthChange(object sender, EventArgs e)
        {
            //try
            //{
            //    TreeViewColumn col = sender as TreeViewColumn;
            //    int index = columns.IndexOf(col);
            //    Application.Invoke(delegate
            //    {
            //    // if something is going wrong with column width, this is probably causing it                
            //    cells[index].Width = col.Width - 4;
            //        cells[index].Ellipsize = EllipsizeMode.End;
            //    });
            //}
            //catch (Exception err)
            //{
            //    ShowError(err);
            //}
        }

        /// <summary>
        /// Event handler for clicking on the TreeView. 
        /// Shows the context menu (if and only if the click is a right click).
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        [GLib.ConnectBefore]
        private void OnTreeClicked(object sender, ButtonReleaseEventArgs e)
        {
            try
            {
                if (e.Event.Button == 1) // left click
                    Changed?.Invoke(sender, new EventArgs());

                else if (e.Event.Button == 3) // right click
                {
                    TreePath path;
                    tree.GetPathAtPos((int)e.Event.X, (int)e.Event.Y, out path);

                    // By default, Gtk will un-select the selected rows when a normal (non-shift/ctrl) click is registered.
                    // Setting e.Retval to true will stop the default Gtk ButtonPress event handler from being called after 
                    // we return from this handler, which in turn means that the rows will not be deselected.
                    e.RetVal = tree.Selection.GetSelectedRows().Contains(path);
                    if (contextMenu != null)
                    {
                        contextMenu.ShowAll();
                        contextMenu.Popup();
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
