using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using System;

namespace UserInterface.Views
{
    public delegate void DatasetDelegate(string TableName);
    public delegate void CellDelegate(int Col, int Row, string NewContents);
    public delegate void ColumnClickedDelegate(string ColumnHeader);
    interface ISeriesView
    {
        /// <summary>
        /// Invoked when the dataset is changed.
        /// </summary>
        event DatasetDelegate DatasetChanged;

        /// <summary>
        /// User has changed the selected cell in the data grid.
        /// </summary>
        event ColumnClickedDelegate DataColumnClicked;

        /// <summary>
        /// User has changed a series cell.
        /// </summary>
        event CellDelegate SeriesCellChanged;

        /// <summary>
        /// Set the data source to use in the series grid at the top.
        /// </summary>
        DataTable Series { get; set; }
         
        /// <summary>
        /// Set the data source to use in the data grid at the bottom
        /// </summary>
        DataTable Data { get; set; }

        /// <summary>
        /// Set the datasets.
        /// </summary>
        string[] Datasets { get; set; }

        /// <summary>
        /// Return the current selected series cell.
        /// </summary>
        Point CurrentSeriesCellSelected { get; set; }

        /// <summary>
        /// Return the current data set.
        /// </summary>
        string CurrentDataset { get; }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        void AddSeriesContextAction(string ButtonText, System.EventHandler OnClick);

    }


    public partial class SeriesView : UserControl, ISeriesView
    {
        /// <summary>
        /// Invoked when the dataset is changed.
        /// </summary>
        public event DatasetDelegate DatasetChanged;

        /// <summary>
        /// User has changed the selected cell in the data grid.
        /// </summary>
        public event ColumnClickedDelegate DataColumnClicked;

        /// <summary>
        /// User has changed a series cell.
        /// </summary>
        public event CellDelegate SeriesCellChanged;

        /// <summary>
        /// constructor.
        /// </summary>
        public SeriesView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set the data source to use in the series grid at the top.
        /// </summary>
        public DataTable Series
        {
            get
            {
                return SeriesGrid.DataSource as DataTable;
            }
            set
            {
                SeriesGrid.DataSource = value;
                foreach (DataGridViewColumn C in SeriesGrid.Columns)
                    C.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        /// <summary>
        /// Set the data source to use in the data grid at the bottom
        /// </summary>
        public DataTable Data
        {
            get
            {
                return DataGrid.DataSource as DataTable;
            }
            set
            {
                DataGrid.DataSource = value;
                foreach (DataGridViewColumn C in DataGrid.Columns)
                    C.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        /// <summary>
        /// Set the datasets.
        /// </summary>
        public string[] Datasets
        {
            get
            {
                List<string> Items = new List<string>();
                foreach (string S in DataCombo.Items)
                    Items.Add(S);
                return Items.ToArray();
            }
            set
            {
                DataCombo.Items.Clear();
                foreach (string S in value)
                    DataCombo.Items.Add(S);
                if (DataCombo.Items.Count > 0)
                    DataCombo.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Return the current selected series cell.
        /// </summary>
        public Point CurrentSeriesCellSelected
        {
            get
            {
                if (SeriesGrid.SelectedCells.Count == 1)
                    return new Point(SeriesGrid.SelectedCells[0].ColumnIndex, SeriesGrid.SelectedCells[0].RowIndex);
                else
                    throw new Exception("No cell selected in Series Grid");
            }
            set
            {
                SeriesGrid.ClearSelection();
                SeriesGrid[value.X, value.Y].Selected = true;
            }
        }

        /// <summary>
        /// Return the current data set.
        /// </summary>
        public string CurrentDataset { get { return DataCombo.Text; } }

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        public void AddSeriesContextAction(string ButtonText, System.EventHandler OnClick)
        {
            ToolStripItem Item = PopupMenu.Items.Add(ButtonText);
            Item.Click += OnClick;
        }

        /// <summary>
        /// User has changed the dataset.
        /// </summary>
        private void OnDataComboChanged(object sender, System.EventArgs e)
        {
            if (DatasetChanged != null)
                DatasetChanged(DataCombo.Text);
        }

        /// <summary>
        /// User has changed the selected cell in the data grid.
        /// </summary>
        private void DataGrid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (DataColumnClicked != null)
                DataColumnClicked(DataGrid.Columns[e.ColumnIndex].HeaderText);
        }

        /// <summary>
        /// User has finished editing a cell.
        /// </summary>
        private void OnCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (SeriesCellChanged != null)
                SeriesCellChanged(e.ColumnIndex, e.RowIndex, SeriesGrid[e.ColumnIndex, e.RowIndex].Value.ToString());
        }

    }
}
