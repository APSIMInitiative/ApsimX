using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

// This is the view used by the WeatherFile component

namespace UserInterface.Views
{
    public delegate void BrowseDelegate(string FileName);

    interface IMetDataView
    {
        void PopulateData(DataTable Data);
        event BrowseDelegate OnBrowseClicked;
    }

    public partial class TabbedMetDataView : UserControl, IMetDataView
    {
        public event BrowseDelegate OnBrowseClicked;

        public TabbedMetDataView()
        {
            InitializeComponent();
        }

        public String Filename
        {
            get { return label1.Text; }
            set { label1.Text = value;}
        }
        public String Summarylabel
        {
            set { label2.Text = value; }
        }
        public void PopulateData(DataTable Data)
        {
            //fill the grid with data
            dataGridView1.DataSource = Data;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            openFileDialog1.FileName = label1.Text;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                label1.Text = openFileDialog1.FileName;
                OnBrowseClicked.Invoke(label1.Text);    //reload the grid with data
            }
        }
    }
}
