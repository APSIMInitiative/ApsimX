using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace UserInterface.Views
{

    public delegate void GridCellValueChanged(int Col, int Row, object OldValue, object NewValue);
    public delegate void GridCellHeaderClickedDelegate(string HeaderText);
    
    public interface IGridView
    {
        /// <summary>
        /// This event is invoked when the value of a cell is changed.
        /// </summary>
        event GridCellValueChanged CellValueChanged;

        /// <summary>
        /// Invoked when a grid cell header is clicked.
        /// </summary>
        event GridCellHeaderClickedDelegate ColumnHeaderClicked;

        /// <summary>
        /// Specified the data to use to populate the grid.
        /// </summary>
        DataTable DataSource { get; set; }

        /// <summary>
        /// Set the editor for the specified cell using the specified Obj to determine type
        /// </summary>
        void SetCellEditor(int Col, int Row, object Obj);

        /// <summary>
        /// Set the specified column to auto size.
        /// </summary>
        void SetColumnAutoSize(int Col);

        /// <summary>
        /// Set the specified column's alignment (left/right)
        /// </summary>
        void SetColumnAlignment(int Col, bool LeftAlign);

        /// <summary>
        /// Get or set the number of rows in grid.
        /// </summary>
        int RowCount { get; set; }

        /// <summary>
        /// Set the column to readonly.
        /// </summary>
        void SetColumnReadOnly(int Col, bool IsReadOnly);

        /// <summary>
        /// Set the column format.
        /// </summary>
        void SetColumnFormat(int Col, string Format, 
                             Color? BackgroundColour = null,
                             Color? ForegroundColour = null,
                             bool ReadOnly = false,
                             string[] ToolTips = null);

        /// <summary>
        /// Set the numeric grid format e.g. N3
        /// </summary>
        void SetNumericFormat(string Format);

        /// <summary>
        /// Get or set the readonly status of the grid.
        /// </summary>
        bool ReadOnly { get; set; }

        /// <summary>
        /// Return the address of the current cell in the grid.
        /// </summary>
        Point CurrentCell { get; }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        void AddContextAction(string ButtonText, System.EventHandler OnClick);

        /// <summary>
        /// Return a cell's tooltip.
        /// </summary>
        string GetToolTipForCell(int Col, int Row);

        /// <summary>
        /// Set a cell's tooltip.
        /// </summary>
        void SetToolTipForCell(int Col, int Row, string TipText);

        /// <summary>
        /// Set the value of the specified cell.
        /// </summary>
        void SetCellValue(int Col, int Row, object Value);

        /// <summary>
        /// Set the value of the specified cell.
        /// </summary>
        object GetCellValue(int Col, int Row);

    }

    public partial class GridView : UserControl, IGridView
    {
        private object ValueBeforeEdit;
        private DataTable Data;

        /// <summary>
        /// This event is invoked when the value of a cell is changed.
        /// </summary>
        public event GridCellValueChanged CellValueChanged;

        /// <summary>
        /// Invoked when a grid cell header is clicked.
        /// </summary>
        public event GridCellHeaderClickedDelegate ColumnHeaderClicked;

        /// <summary>
        /// Constructor
        /// </summary>
        public GridView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// DataSource property.
        /// </summary>
        public DataTable DataSource
        {
            get
            {
                return Data;
            }
            set
            {
                Data = value;
                PopulateGrid();
            }
        }

        /// <summary>
        /// Populate the grid from the DataSource.
        /// </summary>
        private void PopulateGrid()
        {
            if (DataSource != null)
            {
                // Under MONO for LINUX, when Grid.EditMode = DataGridViewEditMode.EditOnEnter
                // then the populating code below will cause the grid to go into edit mode.
                // For now turn off edit mode temporarily.
                Grid.EditMode = DataGridViewEditMode.EditProgrammatically;

                // The populating code below will cause Grid.CellValueChanged to be invoked
                // Turn this event off temporarily.
                Grid.CellValueChanged -= OnCellValueChanged;

                // Make sure we have the right number of columns.
                Grid.ColumnCount = Math.Max(DataSource.Columns.Count, 1);

                // Populate the grid headers.
                for (int Col = 0; Col < DataSource.Columns.Count; Col++)
                {
                    Grid.Columns[Col].HeaderText = DataSource.Columns[Col].ColumnName;
                    Grid.Columns[Col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    Grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    Grid.Columns[Col].SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                // Populate the grid cells with new rows.
                Grid.RowCount = DataSource.Rows.Count;
                for (int Row = 0; Row < DataSource.Rows.Count; Row++)
                {
                    for (int Col = 0; Col < DataSource.Columns.Count; Col++)
                        Grid[Col, Row].Value = DataSource.Rows[Row][Col];
                }

                foreach (DataGridViewColumn Col in Grid.Columns)
                    Col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                // Reinstate Grid.CellValueChanged event.
                Grid.CellValueChanged += OnCellValueChanged;

                // Reinstate our desired edit mode.
                Grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            }
            else
                Grid.Columns.Clear();
        }

        /// <summary>
        /// Set the editor for the specified cell using the specified Obj to determine type
        /// </summary>
        public void SetCellEditor(int Col, int Row, object Obj)
        {
            if (Obj != null)
            {
                object Value = Grid[Col, Row].Value;

                if (Obj is DateTime)
                    Grid[Col, Row] = new Utility.DataGridViewCalendarCell.CalendarCell();
                else if (Obj is bool)
                    Grid[Col, Row] = new DataGridViewCheckBoxCell();
                else if (Obj is Color)
                {
                    Utility.ColorPickerCell Button = new Utility.ColorPickerCell();
                    Grid[Col, Row] = Button;
                    Grid.Columns[Col].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    Grid.Columns[Col].Width = Grid.Rows[Row].Height;  // Make the cell square
                }
                else if (Obj.GetType().IsEnum)
                {
                    List<string> Items = new List<string>();
                    foreach (object e in Obj.GetType().GetEnumValues())
                        Items.Add(e.ToString());

                    SetCellEditor(Col, Row, Items.ToArray());
                }
                else if (Obj is string[])
                {
                    DataGridViewComboBoxCell Combo;
                    if (Grid[Col, Row] is DataGridViewComboBoxCell)
                        Combo = Grid[Col, Row] as DataGridViewComboBoxCell;
                    else
                        Combo = new DataGridViewComboBoxCell();
                    
                    Combo.Items.Clear();
                    foreach (string St in Obj as string[])
                        if (St != null)
                            Combo.Items.Add(St);
                    Combo.Value = Grid[Col, Row].Value;

                    Combo.FlatStyle = FlatStyle.Flat;
                    Combo.ToolTipText = Grid[Col, Row].ToolTipText;
                    if (!(Grid[Col, Row] is DataGridViewComboBoxCell))
                    {
                        // Normally you set a cell editor like this:
                        //    Grid[Col, Row] = Combo;
                        // But this doesn't work on MONO OSX. The two lines
                        // below seem to work ok though.
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT ||
                            Environment.OSVersion.Platform == PlatformID.Win32Windows)
                            Grid[Col, Row] = Combo;
                        else
                        {
                            Grid.Rows[Row].Cells.RemoveAt(Col);
                            Grid.Rows[Row].Cells.Insert(Col, Combo);
                        }
                    }
                }

                Grid[Col, Row].Value = Value;
            }
        }

        /// <summary>
        /// Set the specified column's alignment (left/right)
        /// </summary>
        public void SetColumnAlignment(int Col, bool LeftAlign)
        {
            if (LeftAlign)
                Grid.Columns[Col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            else
                Grid.Columns[Col].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        /// <summary>
        /// Set the numeric grid format e.g. N3
        /// </summary>
        public void SetNumericFormat(string Format)
        {
            for (int Col = 0; Col < DataSource.Columns.Count; Col++)
            {
                if (DataSource.Columns[Col].DataType == typeof(float) ||
                    DataSource.Columns[Col].DataType == typeof(double))
                    Grid.Columns[Col].DefaultCellStyle.Format = Format;
            }
        }

        /// <summary>
        /// Get or set the number of rows in grid.
        /// </summary>
        public int  RowCount
        {
            get
            {
                return Grid.RowCount;
            }
            set
            {
                Grid.RowCount = value;
            }
        }

        /// <summary>
        /// Get or set the readonly status of the grid.
        /// </summary>
        public bool ReadOnly { get { return Grid.ReadOnly; } set { Grid.ReadOnly = value; } }

        /// <summary>
        /// Set the specified column to auto size.
        /// </summary>
        public void SetColumnAutoSize(int Col)
        {
          Grid.Columns[Col].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
          //avoid auto so the columns can be resized. 20 pixels also allows for datepickers
          int w = Grid.Columns[Col].Width + 20;
          Grid.Columns[Col].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
          Grid.Columns[Col].Width = w;

        }

        /// <summary>
        /// Set the column to readonly.
        /// </summary>
        public void SetColumnReadOnly(int Col, bool IsReadOnly)
        {
            Grid.Columns[Col].ReadOnly = IsReadOnly;
            Grid.Columns[Col].DefaultCellStyle.BackColor = Color.LightGray;
            if (Grid.CurrentCell != null && Grid.CurrentCell.ColumnIndex == Col)
            {
                // Move the current cell out of the readonly column.
                Grid.CurrentCell = Grid.Rows[Grid.CurrentCell.RowIndex].Cells[Grid.CurrentCell.ColumnIndex + 1];
            }
        }

        /// <summary>
        /// Set the column format.
        /// </summary>
        public void SetColumnFormat(int Col, string Format,
                                    Color? BackgroundColour = null,
                                    Color? ForegroundColour = null,
                                    bool ReadOnly = false,
                                    string[] ToolTips = null)
        {
            if (Col < Grid.Columns.Count)
            {
                if (Format != null)
                    Grid.Columns[Col].DefaultCellStyle.Format = Format;
                if (ForegroundColour.HasValue)
                    Grid.Columns[Col].DefaultCellStyle.ForeColor = ForegroundColour.Value;
                if (BackgroundColour.HasValue)
                    Grid.Columns[Col].DefaultCellStyle.BackColor = BackgroundColour.Value;
                Grid.Columns[Col].ReadOnly = ReadOnly;
                if (ToolTips != null)
                {
                    for (int Row = 0; Row < Grid.RowCount; Row++)
                        Grid.Rows[Row].Cells[Col].ToolTipText = ToolTips[Row];
                }
            }
        }

        /// <summary>
        /// Return the address of the current cell in the grid.
        /// </summary>
        public Point CurrentCell
        {
            get
            {
                return new Point(Grid.CurrentCell.ColumnIndex, Grid.CurrentCell.RowIndex);
            }
        }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        public void AddContextAction(string ButtonText, System.EventHandler OnClick)
        {
            ToolStripItem Item = PopupMenu.Items.Add(ButtonText);
            Item.Click += OnClick;
        }

        /// <summary>
        /// Return a cell's tooltip.
        /// </summary>
        public string GetToolTipForCell(int Col, int Row)
        {
            return Grid[Col, Row].ToolTipText;
        }

        /// <summary>
        /// Set a cell's tooltip.
        /// </summary>
        public void SetToolTipForCell(int Col, int Row, string TipText)
        {
            Grid[Col, Row].ToolTipText = TipText;
            Grid.ShowCellToolTips = true;
        }

        /// <summary>
        /// Set the value of the specified cell.
        /// </summary>
        public void SetCellValue(int Col, int Row, object Value)
        {
            Grid[Col, Row].Value = Value;
        }

        /// <summary>
        /// Set the value of the specified cell.
        /// </summary>
        public object GetCellValue(int Col, int Row)
        {
            return Grid[Col, Row].Value;
        }

        /// <summary>
        /// User is about to edit a cell.
        /// </summary>
        public void OnCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            ValueBeforeEdit = Grid[e.ColumnIndex, e.RowIndex].Value;
        }

        /// <summary>
        /// User has finished editing a cell.
        /// </summary>
        private void OnCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (ValueBeforeEdit != null)
            {
                object OldValue = ValueBeforeEdit;
                ValueBeforeEdit = null;

                // Make sure our table has enough rows.
                object NewValue = Grid[e.ColumnIndex, e.RowIndex].Value;
                while (DataSource != null && e.RowIndex >= DataSource.Rows.Count)
                    DataSource.Rows.Add(DataSource.NewRow());

                // Put the new value into the table on the correct row.
                DataSource.Rows[e.RowIndex][e.ColumnIndex] = NewValue;

                if (CellValueChanged != null && ValueBeforeEdit != NewValue)

                    CellValueChanged(e.ColumnIndex, e.RowIndex, OldValue, NewValue);
            }
        }

        /// <summary>
        /// Trap any grid data errors, usually as a result of cell values not being
        /// in combo boxes. We'll handle these elsewhere.
        /// </summary>
        private void OnDataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        /// <summary>
        /// User has clicked a cell. Check for a click on the header of a column and
        /// the click on a colour cell.
        /// </summary>
        private void OnCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
            {
                if (ColumnHeaderClicked != null)
                    ColumnHeaderClicked.Invoke(Grid.Columns[e.ColumnIndex].HeaderText);
            }
            else if (Grid[e.ColumnIndex, e.RowIndex] is Utility.ColorPickerCell)
            {
                ColorDialog dlg = new ColorDialog();

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    ValueBeforeEdit = Grid[e.ColumnIndex, e.RowIndex].Value;
                    Grid[e.ColumnIndex, e.RowIndex].Value = dlg.Color;
                }
            }
        }

        /// <summary>
        /// We need to trap the EditingControlShowing event so that we can tweak all combo box
        /// cells to allow the user to edit the contents.
        /// </summary>
        private void OnEditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (Grid.CurrentCell is DataGridViewComboBoxCell)
            {
                DataGridViewComboBoxEditingControl Combo = (DataGridViewComboBoxEditingControl) Grid.EditingControl;
                Combo.DropDownStyle = ComboBoxStyle.DropDown;
            }
        }

        private void Grid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (Grid.CurrentCell is DataGridViewComboBoxCell)
            {
                DataGridViewComboBoxEditingControl Combo = (DataGridViewComboBoxEditingControl) Grid.EditingControl;
                if (Combo != null && !Combo.Items.Contains(e.FormattedValue))
                    Combo.Items.Add(e.FormattedValue);
            }
        }

    }

    public class GetComboItemsForCellArgs : EventArgs
    {
        public int Col;
        public int Row;
        public List<string> Items = new List<string>();
    }

}
