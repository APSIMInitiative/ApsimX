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
    public delegate void TableSelectedDelegate(string TableName);

    interface IDataStoreView
    {
        void PopulateTables(string[] TableNames);

        /// <summary>
        /// Provide access the the main grid
        /// </summary>
        IGridView Grid { get; }
      
        event TableSelectedDelegate OnTableSelected;
        event EventHandler CreateNowClicked;
    }


    public partial class DataStoreView : UserControl, IDataStoreView
    {

        public event TableSelectedDelegate OnTableSelected;
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
        public void PopulateTables(string[] TableNames)
        {
            TableList.Items.Clear();
            bool isFirstItem = true;
            foreach (string TableName in TableNames)
            {
                if (TableName != "Messages")
                {
                    ListViewItem NewItem = new ListViewItem();
                    NewItem.Text = TableName;
                    TableList.Items.Add(NewItem);
                    NewItem.Selected = isFirstItem;
                    isFirstItem = false;
                }
            }

        }

        /// <summary>
        /// Provide access the the main grid
        /// </summary>
        public IGridView Grid { get { return GridView; } }

        /// <summary>
        /// User has selected a simulation/table pair.
        /// </summary>
        private void OnTableSelectedInGrid(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected && OnTableSelected != null)
                OnTableSelected.Invoke(e.Item.Text);
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
