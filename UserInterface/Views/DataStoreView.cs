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
        /// Invoked when the create now button is clicked.
        /// </summary>
        public event EventHandler CreateNowClicked;

        /// <summary>
        /// Invoked when the run child models button is clicked.
        /// </summary>
        public event EventHandler RunChildModelsClicked;

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
                bool isFirstItem = true;
                foreach (string tableName in value)
                {
                    ListViewItem newItem = new ListViewItem();
                    newItem.Text = tableName;
                    this.listBox1.Items.Add(newItem);
                    newItem.Selected = isFirstItem;
                    isFirstItem = false;
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
        /// Gets or sets the list of simulation names.
        /// </summary>
        public string[] SimulationNames
        {
            get
            {
                string[] items = new string[this.listView2.Items.Count];
                for (int i = 0; i < this.listView2.Items.Count; i++)
                {
                    items[i] = this.listView2.Items[i].Text;
                }

                return items;
            }

            set
            {
                this.listView2.Items.Clear();
                foreach (string tableName in value)
                {
                    ListViewItem newItem = new ListViewItem();
                    newItem.Text = tableName;
                    this.listView2.Items.Add(newItem);
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected simulation name.
        /// </summary>
        public string SelectedSimulationName
        {
            get
            {
                if (this.listView2.SelectedItems.Count > 0)
                {
                    return this.listView2.SelectedItems[0].Text;
                }

                return null;
            }

            set
            {
                foreach (ListViewItem listItem in this.listView2.Items)
                {
                    if (listItem.Text == value)
                    {
                        listItem.Selected = true;
                    }
                }
            }
        }

        /// <summary>
        /// Show the summary content.
        /// </summary>
        /// <param name="content">The html content to show.</param>
        public void ShowSummaryContent(string content)
        {
            this.htmlView1.MemoText = content;
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
        /// The user has clicked the create now button.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnCreateButtonClick(object sender, EventArgs e)
        {
            if (this.CreateNowClicked != null)
            {
                this.CreateNowClicked(this, e);
            }
        }

        /// <summary>
        /// The user has clicked the run child models button.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnRunChildModelsClick(object sender, EventArgs e)
        {
            if (this.RunChildModelsClicked != null)
            {
                this.RunChildModelsClicked.Invoke(this, e);
            }
        }
    }
}
