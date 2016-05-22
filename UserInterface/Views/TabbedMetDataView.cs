using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using UserInterface.Interfaces;
using UserInterface.Views;

// This is the view used by the WeatherFile component
namespace UserInterface.Views
{
    /// <summary>A delegate for a button click</summary>
    /// <param name="fileName">Name of the file.</param>
    public delegate void BrowseDelegate(string fileName);

    ///// <summary>A delegate for a numericUpDown click event</summary>
    ///// <param name="startYear">the start year for the data being displayed in the graph</param>
    ///// <param name="showYears">the number of years of data to be used/displayed in the graph</param>
    public delegate void GraphRefreshDelegate(int tabIndex, decimal startYear, decimal showYears);

    /// <summary>A delegate used when the sheetname dropdown value change is actived</summary>
    /// <param name="fileName"></param>
    /// <param name="sheetName"></param>
    public delegate void ExcelSheetDelegate(string fileName, string sheetName);



    /// <summary>
    /// An interface for a weather data view
    /// </summary>
    interface IMetDataView
    {
        /// <summary>Occurs when browse button is clicked</summary>
        event BrowseDelegate BrowseClicked;

        /// <summary>Occurs when the start year numericUpDown is clicked</summary>
        event GraphRefreshDelegate GraphRefreshClicked;

        /// <summary>A delegate used when the sheetname dropdown value change is actived</summary>
        event ExcelSheetDelegate ExcelSheetChangeClicked;

        /// <summary>Gets or sets the filename.</summary>
        string Filename { get; set; }

        /// <summary>Gets or sets the Excel Sheet name, where applicable</summary>
        string ExcelWorkSheetName { get; set; }

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
        decimal GraphStartYearValue { get; set; }

        /// <summary>set the minimum value for the 'Start Year' NumericUpDown control </summary>
        decimal GraphStartYearMinValue { get; set; }

        /// <summary>set the maximum value for the graph 'Start Year' NumericUpDown control  </summary>
        decimal GraphStartYearMaxValue { get; set; }

        /// <summary>sets/gets the value of 'Show Years' NumericUpDown control </summary>
        decimal GraphShowYearsValue { get; set; }

        /// <summary>set the maximum value for the 'Show Years' NumericUpDown control  </summary>
        decimal GraphShowYearsMaxValue { set; }

        /// <summary>set the height of the BrowseControlPanel </summary>
        decimal BrowsePanelControlHeight { set; }

        /// <summary>Populates the data grid</summary>
        /// <param name="Data">The data</param>
        void PopulateData(DataTable Data);

        /// <summary>
        /// Populates the DropDown of Excel WorksheetNames 
        /// </summary>
        /// <param name="sheetNames"></param>
        void PopulateDropDownData(List<string> sheetNames);


    }

    /// <summary>
    /// A view for displaying weather data.
    /// </summary>
    public partial class TabbedMetDataView : UserControl, IMetDataView
    {
        /// <summary>Occurs when browse button is clicked</summary>
        public event BrowseDelegate BrowseClicked;

        /// <summary>Occurs when start year or show Years numericUpDowns are clicked</summary>
        public event GraphRefreshDelegate GraphRefreshClicked;

        public event ExcelSheetDelegate ExcelSheetChangeClicked;

        /// <summary>Initializes a new instance of the <see cref="TabbedMetDataView"/> class.</summary>
        public TabbedMetDataView()
        {
            InitializeComponent();
        }

        /// <summary>Gets or sets the filename.</summary>
        /// <value>The filename.</value>
        public string Filename
        {
            get { return FileNameControl.Text; }
            set { FileNameControl.Text = value;}
        }

        /// <summary>Gets and sets the worksheet name</summary>
        public string ExcelWorkSheetName
        {
            get { return WorksheetNamesControl.SelectedText; }
            set
            {
                WorksheetNamesControl.SelectedText = value;
                WorksheetNamesControl.Text = value;
            }
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
        public decimal GraphStartYearValue 
        {
            get { return GraphStartYearControl.Value; }
            set { GraphStartYearControl.Value = Convert.ToDecimal(value); }
        }

        /// <summary>set the minimum value for the graph 'Year to display' </summary>
        public decimal GraphStartYearMinValue
        {
            get { return GraphStartYearControl.Minimum;  }
            set { GraphStartYearControl.Minimum = Convert.ToDecimal(value); }
        }

        /// <summary>set the maximum value for the graph 'Year to display' </summary>
        public decimal GraphStartYearMaxValue
        {
            get { return GraphStartYearControl.Maximum; }
            set { GraphStartYearControl.Maximum = Convert.ToDecimal(value); }
        }

        /// <summary>Gets and sets the Graph Year</summary>
        public decimal GraphShowYearsValue
        {
            get { return GraphShowYearsControl.Value; }
            set { GraphShowYearsControl.Value = Convert.ToDecimal(value); }
        }

        /// <summary>set the maximum value for the graph 'Year to display' </summary>
        public decimal GraphShowYearsMaxValue
        {
            set { GraphShowYearsControl.Maximum = Convert.ToDecimal(value); }
        }

        /// <summary>set the height of the BrowseControlPanel</summary>
        public decimal BrowsePanelControlHeight
        {
            set { BrowsePanelControl.Height = Convert.ToInt16(value); }
        }

        /// <summary>Populates the data.</summary>
        /// <param name="Data">The data.</param>
        public void PopulateData(DataTable data)
        {
            //fill the grid with data
            dataGridView1.DataSource = data;
        }


        /// <summary>
        /// used to show load status of Excel sheetname combo
        /// </summary>
        private bool PopulatingDropDownData;

        /// <summary>
        /// Populates the DropDown of Excel WorksheetNames 
        /// </summary>
        /// <param name="sheetNames"></param>
        public void PopulateDropDownData(List<string> sheetNames)
        {
            PopulatingDropDownData = true;
            WorksheetNamesControl.DataSource = sheetNames;
            PopulatingDropDownData = false;
        }



        /// <summary>Handles the Click event of the button1 control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnButton1Click(object sender, EventArgs e)
        {
            
            openFileDialog1.FileName = FileNameControl.Text;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FileNameControl.Text = openFileDialog1.FileName;
                if (BrowseClicked != null)
                {
                    BrowseClicked.Invoke(FileNameControl.Text);    //reload the grid with data
                    if (tabControl1.SelectedTab.TabIndex != 0)
                        tabControl1.SelectedIndex = 0;
                }
            }
        }

        /// <summary>Handles the change event for the GraphStartYear NumericUpDown </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void uxGraphStartYear_ValueChanged(object sender, EventArgs e)
        {
            if (GraphRefreshClicked != null)
            {
                int selectedTabIndex = tabControl1.SelectedIndex;
                GraphRefreshClicked.Invoke(selectedTabIndex, GraphStartYearControl.Value, GraphShowYearsControl.Value);
            }
        }

        /// <summary>Handles the change event for the GraphShowYears NumericUpDown </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void uxGraphShowYears_ValueChanged(object sender, EventArgs e)
        {
            if (GraphRefreshClicked != null)
            {
                int selectedTabIndex = tabControl1.SelectedIndex;
                GraphRefreshClicked.Invoke(selectedTabIndex, (int)GraphStartYearControl.Value, (int)GraphShowYearsControl.Value);
            }
        }

        /// <summary>
        /// Handles the selection change between tabs, so that we can adjust the height of the Browse Panel,
        /// showing/or hiding information that is not relevant.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabControl tc = (TabControl)sender;
            if (tc.SelectedIndex == 0 || tc.SelectedIndex == 1)
            {
                //BrowsePanelControl.Height = 68;
                GraphOptionPanelControl.Visible = false;
                GraphRefreshClicked.Invoke(tc.SelectedIndex, GraphStartYearControl.Value, GraphShowYearsControl.Value);
            }
            else
            {
                //BrowsePanelControl.Height = 68;
                GraphOptionPanelControl.Visible = true;
                GraphRefreshClicked.Invoke(tc.SelectedIndex, GraphStartYearControl.Value, GraphShowYearsControl.Value);
            }
        }

        /// <summary>
        /// This is used to handle the change in value (selected index) for the worksheet dropdown combo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WorkSheetNameControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PopulatingDropDownData == false)
            {
                ComboBox cb = (ComboBox)sender;
                ExcelSheetChangeClicked.Invoke(FileNameControl.Text, cb.SelectedValue.ToString());
                if (tabControl1.SelectedTab.TabIndex == -1) { tabControl1.SelectedIndex = 0; }
            }
        }
    }
}
