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
    
    public interface IGridView
    {
        /// <summary>
        /// This event is invoked when the value of a cell is changed.
        /// </summary>
        event GridCellValueChanged CellValueChanged;

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
        /// Get or set the number of rows in grid.
        /// </summary>
        int RowCount { get; set; }

        /// <summary>
        /// Set the column to readonly.
        /// </summary>
        void SetColumnReadOnly(int Col, bool IsReadOnly);

        /// <summary>
        /// Get or set the readonly status of the grid.
        /// </summary>
        bool ReadOnly { get; set; }

    }

    public partial class GridView : UserControl, IGridView
    {
        private object OldValue;
        private DataTable Data;

        /// <summary>
        /// This event is invoked when the value of a cell is changed.
        /// </summary>
        public event GridCellValueChanged CellValueChanged;

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
                    Grid.Columns[Col].SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                // Populate the grid cells with new rows.
                Grid.RowCount = DataSource.Rows.Count;
                for (int Row = 0; Row < DataSource.Rows.Count; Row++)
                {
                    for (int Col = 0; Col < DataSource.Columns.Count; Col++)
                        Grid[Col, Row].Value = DataSource.Rows[Row][Col];
                }

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
        }

        /// <summary>
        /// Set the column to readonly.
        /// </summary>
        public void SetColumnReadOnly(int Col, bool IsReadOnly)
        {
            Grid.Columns[Col].ReadOnly = IsReadOnly;
            Grid.Columns[Col].DefaultCellStyle.BackColor = Color.LightGray;
            if (Grid.CurrentCell.ColumnIndex == Col)
            {
                // Move the current cell out of the readonly column.
                Grid.CurrentCell = Grid.Rows[Grid.CurrentCell.RowIndex].Cells[Grid.CurrentCell.ColumnIndex + 1];
            }
        }

        /// <summary>
        /// User is about to edit a cell.
        /// </summary>
        private void OnCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            OldValue = Grid[e.ColumnIndex, e.RowIndex].Value;
        }

        /// <summary>
        /// User has finished editing a cell.
        /// </summary>
        private void OnCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // Make sure our table has enough rows.
            object NewValue = Grid[e.ColumnIndex, e.RowIndex].Value;
            while (DataSource != null && e.RowIndex >= DataSource.Rows.Count)
                DataSource.Rows.Add(DataSource.NewRow());

            // Put the new value into the table on the correct row.
            DataSource.Rows[e.RowIndex][e.ColumnIndex] = NewValue;

            if (CellValueChanged != null && OldValue != NewValue)
                CellValueChanged(e.ColumnIndex, e.RowIndex, OldValue, NewValue);
        }

        /// <summary>
        /// Trap any grid data errors, usually as a result of cell values not being
        /// in combo boxes. We'll handle these elsewhere.
        /// </summary>
        private void Grid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

    }

    public class GetComboItemsForCellArgs : EventArgs
    {
        public int Col;
        public int Row;
        public List<string> Items = new List<string>();
    }

}
