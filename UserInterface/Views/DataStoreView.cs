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
    public delegate void TableSelectedDelegate(string SimulationName, string TableName);

    interface IDataStoreView
    {
        void PopulateTables(string[] Simulations, string[] TableNames);
        void PopulateData(DataTable Data);

        bool AutoCreate { get; set; }
        
        event TableSelectedDelegate OnTableSelected;
        event EventHandler AutoCreateChanged;
        event EventHandler CreateNowClicked;
    }


    public partial class DataStoreView : UserControl, IDataStoreView
    {

        public event TableSelectedDelegate OnTableSelected;
        public event EventHandler AutoCreateChanged;
        public event EventHandler CreateNowClicked;

        /// <summary>
        /// constructor
        /// </summary>
        public DataStoreView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// populate the tables list in the view.
        /// </summary>
        public void PopulateTables(string[] Simulations, string[] TableNames)
        {
            foreach (string SimulationName in Simulations)
            {
                ListViewGroup Group = TableList.Groups.Add(SimulationName, SimulationName);

                foreach (string TableName in TableNames)
                {
                    ListViewItem NewItem = new ListViewItem();
                    NewItem.Text = TableName;
                    NewItem.Group = Group;
                    TableList.Items.Add(NewItem);
                }

            }
            TableList.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            Grid.ReadOnly = true;
        }

        /// <summary>
        /// Populate the grid with the specified data.
        /// </summary>
        public void PopulateData(DataTable Data)
        {
            Grid.DataSource = Data;
            if (Data != null)
            {
                Grid.Columns[0].Visible = false;
                FormatGrid();
            }
        }

        /// <summary>
        /// Format the grid.
        /// </summary>
        private void FormatGrid()
        {
            DataTable Data = Grid.DataSource as DataTable;
            for (int i = 0; i < Data.Columns.Count; i++)
            {
                if (Data.Columns[i].DataType == typeof(float) || Data.Columns[i].DataType == typeof(double))
                    Grid.Columns[i].DefaultCellStyle.Format = "N3";
            }

            foreach (DataGridViewColumn Col in Grid.Columns)
                Col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        /// <summary>
        /// User has selected a simulation/table pair.
        /// </summary>
        private void OnTableSelectedInGrid(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected && OnTableSelected != null)
                OnTableSelected.Invoke(e.Item.Group.Name, e.Item.Text);
        }

        /// <summary>
        /// Get/Set auto create checkbox state
        /// </summary>
        public bool AutoCreate
        {
            get
            {
                return AutoCreateCheckBox.Checked;
            }
            set
            {
                AutoCreateCheckBox.Checked = value;
            }
        }

        /// <summary>
        /// The user has changed the auto check state.
        /// </summary>
        private void OnAutoCreateCheckBoxChanged(object sender, EventArgs e)
        {
            if (AutoCreateChanged != null)
                AutoCreateChanged(this, e);
        }

        /// <summary>
        /// The user has clicked the create now button.
        /// </summary>
        private void OnCreateButtonClick(object sender, EventArgs e)
        {
            if (CreateNowClicked != null)
                CreateNowClicked(this, e);
        }
        
    }
}
