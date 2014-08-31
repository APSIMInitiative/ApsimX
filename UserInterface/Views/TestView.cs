using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    public partial class TestView : UserControl, ITestView
    {
        /// <summary>
        /// The table name has changed.
        /// </summary>
        public event EventHandler TableNameChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestView" /> class.
        /// </summary>
        public TestView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the list of tables.
        /// </summary>
        public string[] TableNames
        {
            get
            {
                string[] items = new string[this.comboBox1.Items.Count];
                for (int i = 0; i < this.comboBox1.Items.Count; i++)
                {
                    items[i] = this.comboBox1.Items[i].ToString();
                }

                return items;
            }

            set
            {
                this.comboBox1.Items.Clear();
                foreach (string tableName in value)
                {
                    this.comboBox1.Items.Add(tableName);
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        public string TableName
        {
            get
            {
                return comboBox1.Text;
            }

            set
            {
                comboBox1.Text = value;
            }
        }

        /// <summary>
        /// Gets or sets the data for the grid
        /// </summary>
        public DataTable Data
        {
            get
            {
                return gridView1.DataSource as DataTable;
            }

            set
            {
                gridView1.DataSource = value;
            }
        }

        /// <summary>
        /// Gets the editor.
        /// </summary>
        public IEditorView Editor
        {
            get
            {
                return editorView1;
            }
        }

        /// <summary>
        /// The table name combo has changed.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnTableNameChanged(object sender, EventArgs e)
        {
            if (TableNameChanged != null)
            {
                TableNameChanged.Invoke(this, e);
            }
        }
    }
}
