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
        event TableSelectedDelegate OnTableSelected;
    }


    public partial class DataStoreView : UserControl, IDataStoreView
    {

        public event TableSelectedDelegate OnTableSelected;

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
            if (Data != null)
            {
                Grid.DataSource = Data;
                Grid.Columns[0].Visible = false;
            }
            else
            {
                Grid.ColumnCount = 0;
                Grid.RowCount = 1;
            }
        }

        /// <summary>
        /// User has selected a simulation/table pair.
        /// </summary>
        private void OnTableSelectedInGrid(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected && OnTableSelected != null)
                OnTableSelected.Invoke(e.Item.Group.Name, e.Item.Text);
        }

    }
}
