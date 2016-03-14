using System;
using System.Data;
using System.Windows.Forms;
using UserInterface.Interfaces;

// This is the view used by the WeatherFile component
namespace UserInterface.Views
{
    /// <summary>A delegate for a button click</summary>
    /// <param name="FileName">Name of the file.</param>
    public delegate void BrowseDelegate(string FileName);

    ///// <summary>A delegate for a numericUpDown click event</summary>
    ///// <param name="startYear">the start year for the data being displayed in the graph</param>
    ///// <param name="showYears">the number of years of data to be used/displayed in the graph</param>
    //public delegate void GraphRefreshDelegate(decimal startYear, decimal showYears);


    /// <summary>
    /// An interface for a weather data view
    /// </summary>
    interface IMetDataView
    {
        /// <summary>Occurs when browse button is clicked</summary>
        event BrowseDelegate BrowseClicked;

        /// <summary>Occurs when the start year numericUpDown is clicked</summary>
        //event GraphRefreshDelegate GraphRefreshClicked;

        /// <summary>Gets or sets the filename.</summary>
        string Filename { get; set; }

        /// <summary>Sets the summarylabel.</summary>
        string Summarylabel { set; }

        /// <summary>Gets the graph.</summary>
        IGraphView GraphSummary { get; }

        /// <summary>Gets the Rainfall graph.</summary>
        IGraphView GraphRainfall { get; }

        /// <summary>Gets the Monthly Rainfall graph.</summary>
        IGraphView GraphMonthlyRainfall { get; }

        /// <summary>Gets the Temperature graph.</summary>
        IGraphView GraphTemperature { get; }

        /// <summary>Gets the Radiation graph.</summary>
        IGraphView GraphRadiation { get; }

        /// <summary>sets the Graph Year</summary>
         decimal GraphStartYear { get; set; }

        /// <summary>set the minimum value for the 'Start Year' NumericUpDown control </summary>
        decimal GraphStartYearMinValue { get; set; }

        /// <summary>set the maximum value for the graph 'Start Year' NumericUpDown control  </summary>
        decimal GraphStartYearMaxValue { get; set; }

        /// <summary>sets/gets the value of 'Show Years' NumericUpDown control </summary>
        decimal GraphShowYears { get; set; }

        /// <summary>set the maximum value for the 'Show Years' NumericUpDown control  </summary>
        decimal GraphShowYearsMaxValue { set; }

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

        /// <summary>Occurs when start year or show Years numericUpDowns are clicked</summary>
        //public event GraphRefreshDelegate GraphRefreshClicked;


        /// <summary>Initializes a new instance of the <see cref="TabbedMetDataView"/> class.</summary>
        public TabbedMetDataView()
        {
            InitializeComponent();
        }

        /// <summary>Gets or sets the filename.</summary>
        /// <value>The filename.</value>
        public string Filename
        {
            get { return uxFileName.Text; }
            set { uxFileName.Text = value;}
        }

        /// <summary>Sets the summarylabel.</summary>
        /// <value>The summarylabel.</value>
        public string Summarylabel
        {
            set { richTextBox1.Text = value; }
        }

        /// <summary>Gets the graph.</summary>
        /// <value>The graph.</value>
        public IGraphView GraphSummary { get { return graphViewSummary; } }

        /// <summary>/// Gets the Rainfall Graph/// </summary>
        /// <value>The Rainfall Graph</value>
        public IGraphView GraphRainfall { get { return graphViewRainfall; } }

        /// <summary>/// Gets the Monthly Rainfall Graph/// </summary>
        /// <value>The Rainfall Graph</value>
        public IGraphView GraphMonthlyRainfall { get { return graphViewMonthlyRainfall; } }

        /// <summary>/// Gets the Temperature Graph/// </summary>
        /// <value>The Temperature Graph</value>
        public IGraphView GraphTemperature { get { return graphViewTemperature; } }

        /// <summary>/// Gets the Radiation Graph/// </summary>
        /// <value>The Radiation Graph</value>
        public IGraphView GraphRadiation { get { return graphViewRadiation; } }

        /// <summary>Sets the Graph Year</summary>
        public decimal GraphStartYear 
        {
            get { return uxGraphStartYear.Value; }
            set { uxGraphStartYear.Value = Convert.ToDecimal(value); }
        }

        /// <summary>set the minimum value for the graph 'Year to display' </summary>
        public decimal GraphStartYearMinValue
        {
            get { return uxGraphStartYear.Minimum;  }
            set { uxGraphStartYear.Minimum = Convert.ToDecimal(value); }
        }

        /// <summary>set the maximum value for the graph 'Year to display' </summary>
        public decimal GraphStartYearMaxValue
        {
            get { return uxGraphStartYear.Maximum; }
            set { uxGraphStartYear.Maximum = Convert.ToDecimal(value); }
        }

        /// <summary>Sets the Graph Year</summary>
        public decimal GraphShowYears
        {
            get { return uxGraphShowYears.Value; }
            set { uxGraphShowYears.Value = Convert.ToDecimal(value); }
        }

        /// <summary>set the maximum value for the graph 'Year to display' </summary>
        public decimal GraphShowYearsMaxValue
        {
            set { uxGraphShowYears.Maximum = Convert.ToDecimal(value); }
        }

        /// <summary>Populates the data.</summary>
        /// <param name="Data">The data.</param>
        public void PopulateData(DataTable data)
        {
            //fill the grid with data
            dataGridView1.DataSource = data;
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
                if (BrowseClicked != null)
                    BrowseClicked.Invoke(label1.Text);    //reload the grid with data
            }
        }

        ///// <summary>Handles the change event for the GraphStartYear NumericUpDown </summary>
        ///// <param name="sender">The source of the event</param>
        ///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //private void uxGraphStartYear_ValueChanged(object sender, EventArgs e)
        //{
        //    //if (GraphRefreshClicked != null)
        //        GraphRefreshClicked.Invoke(uxGraphStartYear.Value, uxGraphShowYears.Value);
        //}

        ///// <summary>Handles the change event for the GraphShowYears NumericUpDown </summary>
        ///// <param name="sender">The source of the event</param>
        ///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //private void uxGraphShowYears_ValueChanged(object sender, EventArgs e)
        //{
        //    if (GraphRefreshClicked != null)
        //        GraphRefreshClicked.Invoke((int)uxGraphStartYear.Value, (int)uxGraphShowYears.Value);
        //}


        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabControl tc = (TabControl)sender;
            if (tc.SelectedIndex == 0 || tc.SelectedIndex == 1)
            {
                uxBrowsePanel.Height = 41;
            }
            else
            {
                uxBrowsePanel.Height = 68;
            }
        }
    }
}
