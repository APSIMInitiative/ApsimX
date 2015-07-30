// -----------------------------------------------------------------------
// <copyright file="DataStoreView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Windows.Forms;
    using Interfaces;

    /// <summary>
    /// A data store view
    /// </summary>
    public partial class DataStoreView : UserControl, IDataStoreView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataStoreView" /> class.
        /// </summary>
        public DataStoreView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when a table is selected.
        /// </summary>
        public event EventHandler OnTableSelected;

        /// <summary>
        /// Invoked when a table is selected.
        /// </summary>
        public event EventHandler OnSimulationSelected;

        /// <summary>
        /// Invoked when the auto export checkbox is clicked.
        /// </summary>
        public event EventHandler AutoExportClicked;

        /// <summary>
        /// Gets or sets the list of tables.
        /// </summary>
        public string[] TableNames
        {
            get
            {
                string[] items = new string[this.listBox1.Items.Count];
                for (int i = 0; i < this.listBox1.Items.Count; i++)
                {
                    items[i] = this.listBox1.Items[i].Text;
                }

                return items;
            }

            set
            {
                this.listBox1.Items.Clear();
                foreach (string tableName in value)
                {
                    ListViewItem newItem = new ListViewItem();
                    newItem.Text = tableName;
                    this.listBox1.Items.Add(newItem);
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently selected table name.
        /// </summary>
        public string SelectedTableName
        {
            get
            {
                if (this.listBox1.SelectedItems.Count > 0)
                {
                    return this.listBox1.SelectedItems[0].Text;
                }

                return null;
            }

            set
            {
                foreach (ListViewItem listItem in this.listBox1.Items)
                {
                    if (listItem.Text == value)
                    {
                        listItem.Selected = true;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the main data grid.
        /// </summary>
        public Interfaces.IGridView Grid 
        { 
            get
            {
                return this.gridView;        
            }
        }

        /// <summary>
        /// Gets or sets the autoexport option
        /// </summary>
        public bool AutoExport
        {
            get
            {
                return this.checkBox1.Checked;
            }

            set
            {
                this.checkBox1.Checked = value;
            }
        }

        /// <summary>
        /// User has selected a simulation/table pair.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnTableSelectedInGrid(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected && this.OnTableSelected != null)
            {
                this.OnTableSelected.Invoke(sender, new EventArgs());
            }
        }

        /// <summary>
        /// User has selected a simulation/table pair.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnSimulationSelectedInView(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected && this.OnSimulationSelected != null)
            {
                this.OnSimulationSelected.Invoke(sender, new EventArgs());
            }
        }

        /// <summary>
        /// The auto export checkbox has been clicked.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnAutoExportCheckedChanged(object sender, EventArgs e)
        {
            if (this.AutoExportClicked != null)
            {
                this.AutoExportClicked(this, e);
            }
        }
    }
}
