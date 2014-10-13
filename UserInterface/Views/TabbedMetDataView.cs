using System;
using System.Data;
using System.Windows.Forms;
using UserInterface.Interfaces;

// This is the view used by the WeatherFile component
namespace UserInterface.Views
{
    /// <summary>
    /// A delegate for a button click
    /// </summary>
    /// <param name="FileName">Name of the file.</param>
    public delegate void BrowseDelegate(string FileName);

    /// <summary>
    /// An interface for a weather data view
    /// </summary>
    interface IMetDataView
    {
        /// <summary>Occurs when browse button is clicked</summary>
        event BrowseDelegate BrowseClicked;

        /// <summary>Gets or sets the filename.</summary>
        string Filename { get; set; }

        /// <summary>Sets the summarylabel.</summary>
        string Summarylabel { set; }

        /// <summary>Gets the graph.</summary>
        IGraphView Graph { get; }

        /// <summary>Populates the data grid</summary>
        /// <param name="Data">The data</param>
        void PopulateData(DataTable Data);
    }

    /// <summary>
    /// A view for displaying weather data.
    /// </summary>
    public partial class TabbedMetDataView : UserControl, IMetDataView
    {
        /// <summary>Occurs when browse button is clicked</summary>
        public event BrowseDelegate BrowseClicked;

        /// <summary>Initializes a new instance of the <see cref="TabbedMetDataView"/> class.</summary>
        public TabbedMetDataView()
        {
            InitializeComponent();
        }

        /// <summary>Gets or sets the filename.</summary>
        /// <value>The filename.</value>
        public string Filename
        {
            get { return label1.Text; }
            set { label1.Text = value;}
        }

        /// <summary>Sets the summarylabel.</summary>
        /// <value>The summarylabel.</value>
        public string Summarylabel
        {
            set { richTextBox1.Text = value; }
        }

        /// <summary>Gets the graph.</summary>
        /// <value>The graph.</value>
        public IGraphView Graph { get { return graphView1; } }

        /// <summary>Populates the data.</summary>
        /// <param name="Data">The data.</param>
        public void PopulateData(DataTable Data)
        {
            //fill the grid with data
            dataGridView1.DataSource = Data;
            
        }

        /// <summary>Handles the Click event of the button1 control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnButton1Click(object sender, EventArgs e)
        {
            
            openFileDialog1.FileName = label1.Text;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                label1.Text = openFileDialog1.FileName;
                BrowseClicked.Invoke(label1.Text);    //reload the grid with data
            }
        }
    }
}
