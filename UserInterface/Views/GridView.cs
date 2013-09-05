using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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
        /// Set the editor for the specified cell
        /// </summary>
        void SetCellEditor(int Col, int Row, Type type);

        /// <summary>
        /// Set the specified column to auto size.
        /// </summary>
        void SetColumnAutoSize(int Col);

        /// <summary>
        /// Set the column to readonly.
        /// </summary>
        void SetColumnReadOnly(int Col, bool IsReadOnly);
    }

    public partial class GridView : UserControl, IGridView
    {
        private object OldValue;

        /// <summary>
        /// This event is invoked when the value of a cell is changed.
        /// </summary>
        public event GridCellValueChanged CellValueChanged;

        /// <summary>
        /// Specified the data to use to populate the grid.
        /// </summary>
        private DataTable Data;

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
                PopulateGrid(Data);
            }
        }

        /// <summary>
        /// Populate the grid from the data in the specified table.
        /// </summary>
        private void PopulateGrid(DataTable Data)
        {
            Grid.DataSource = Data;
            foreach (DataGridViewColumn Column in Grid.Columns)
                Column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        /// <summary>
        /// Set the editor for the specified cell
        /// </summary>
        public void SetCellEditor(int Col, int Row, Type type)
        {
            if (type == typeof(DateTime))
                Grid[Col, Row] = new Utility.DataGridViewCalendarCell.CalendarCell();
        }

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
            object NewValue = Grid[e.ColumnIndex, e.RowIndex].Value;
            if (CellValueChanged != null && OldValue != NewValue)
                CellValueChanged(e.ColumnIndex, e.RowIndex, OldValue, NewValue);
        }


    }
}
