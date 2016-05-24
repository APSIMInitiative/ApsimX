using System;
using System.Collections.Generic;
using System.Data;
using UserInterface.Interfaces;
using Gtk;
using Glade;

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
        int GraphStartYearValue { get; set; }

        /// <summary>set the minimum value for the 'Start Year' NumericUpDown control </summary>
        int GraphStartYearMinValue { get; set; }

        /// <summary>set the maximum value for the graph 'Start Year' NumericUpDown control  </summary>
        int GraphStartYearMaxValue { get; set; }

        /// <summary>Show or hide the combobox listing the names of Excel worksheets </summary>
        /// <param name="show"></param>
        void ShowExcelSheets(bool show);

        /// <summary>sets/gets the value of 'Show Years' NumericUpDown control </summary>
        int GraphShowYearsValue { get; set; }

        /// <summary>set the maximum value for the 'Show Years' NumericUpDown control  </summary>
        int GraphShowYearsMaxValue { set; }

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
    public class TabbedMetDataView : ViewBase, IMetDataView
    {
        private GraphView graphViewSummary;
        private GraphView graphViewRainfall;
        private GraphView graphViewMonthlyRainfall;
        private GraphView graphViewTemperature;
        private GraphView graphViewRadiation;

        private GridView gridViewData;

        /// <summary>Occurs when browse button is clicked</summary>
        public event BrowseDelegate BrowseClicked;

        /// <summary>Occurs when start year or show Years numericUpDowns are clicked</summary>
        public event GraphRefreshDelegate GraphRefreshClicked;

        public event ExcelSheetDelegate ExcelSheetChangeClicked;

        [Widget]
        private Label labelFileName;
        [Widget]
        private VBox vbox1;
        [Widget]
        private Notebook notebook1;
        [Widget]
        private TextView textview1;
        [Widget]
        private Alignment alignSummary;
        [Widget]
        private Alignment alignData;
        [Widget]
        private Alignment alignRainChart;
        [Widget]
        private Alignment alignRainMonthly;
        [Widget]
        private Alignment alignTemp;
        [Widget]
        private Alignment alignRadn;
        [Widget]
        private VBox vboxRainChart;
        [Widget]
        private VBox vboxRainMonthly;
        [Widget]
        private VBox vboxTemp;
        [Widget]
        private VBox vboxRadn;
        [Widget]
        private HBox hboxOptions;
        [Widget]
        private SpinButton spinStartYear;
        [Widget]
        private SpinButton spinNYears;
        [Widget]
        private Button button1;
        [Widget]
        private VPaned vpaned1;
        [Widget]
        private HBox hbox2;
        [Widget]
        private Alignment alignment10;
        private DropDownView worksheetCombo;

        /// <summary>Initializes a new instance of the <see cref="TabbedMetDataView"/> class.</summary>
        public TabbedMetDataView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.TabbedMetDataView.glade", "vbox1");
            gxml.Autoconnect(this);
            _mainWidget = vbox1;
            graphViewSummary = new GraphView(this);
            alignSummary.Add(graphViewSummary.MainWidget);
            graphViewRainfall = new GraphView(this);
            vboxRainChart.PackEnd(graphViewRainfall.MainWidget);
            graphViewMonthlyRainfall = new GraphView(this);
            vboxRainMonthly.PackEnd(graphViewMonthlyRainfall.MainWidget);
            graphViewTemperature = new GraphView(this);
            vboxTemp.PackEnd(graphViewTemperature.MainWidget);
            graphViewRadiation = new GraphView(this);
            vboxRadn.PackEnd(graphViewRadiation.MainWidget);
            gridViewData = new GridView(this);
            gridViewData.ReadOnly = true;
            alignData.Add(gridViewData.MainWidget);
            button1.Clicked += OnButton1Click;
            spinStartYear.ValueChanged += OnGraphStartYearValueChanged;
            spinNYears.ValueChanged += OnGraphShowYearsValueChanged;
            notebook1.SwitchPage += TabControl1_SelectedIndexChanged;
            GraphStartYearMaxValue = 2100;
            GraphStartYearMinValue = 1900;
            GraphStartYearValue = 2000;
            GraphShowYearsValue = 1;
            worksheetCombo = new DropDownView(this);
            alignment10.Add(worksheetCombo.MainWidget);
            worksheetCombo.IsVisible = true;
            worksheetCombo.Changed += WorksheetCombo_Changed;
        }

        /// <summary>Gets or sets the filename.</summary>
        /// <value>The filename.</value>
        public string Filename
        {
            get { return labelFileName.Text; }
            set { labelFileName.Text = value; }
        }

        public string ExcelWorkSheetName
        {
            get { return hbox2.Visible ? worksheetCombo.SelectedValue : ""; }
            set { worksheetCombo.SelectedValue = value; }
        }

        /// <summary>Sets the summarylabel.</summary>
        /// <value>The summarylabel.</value>
        public string Summarylabel
        {
            set
            {
                textview1.Buffer.Text = value.TrimEnd(null);
                int lc = textview1.Buffer.LineCount;
                Gdk.Rectangle rectEnd = textview1.GetIterLocation(textview1.Buffer.StartIter);
                vpaned1.Position = lc * rectEnd.Height;
            }
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
        public int GraphStartYearValue
        {
            get { return spinStartYear.ValueAsInt; }
            set { spinStartYear.Value = value; }
        }

        /// <summary>set the minimum value for the graph 'Year to display' </summary>
        public int GraphStartYearMinValue
        {
            get
            {
                double min, max;
                spinStartYear.GetRange(out min, out max);
                return (int)(min + 0.5);
            }
            set
            {
                double min, max;
                spinStartYear.GetRange(out min, out max);
                spinStartYear.SetRange(value, Math.Max(value, max));
            }
        }

        /// <summary>set the maximum value for the graph 'Year to display' </summary>
        public int GraphStartYearMaxValue
        {
            get
            {
                double min, max;
                spinStartYear.GetRange(out min, out max);
                return (int)(max + 0.5);
            }
            set
            {
                double min, max;
                spinStartYear.GetRange(out min, out max);
                spinStartYear.SetRange(Math.Min(value, min), value);
            }
        }

        /// <summary>Gets and sets the Graph Year</summary>
        public int GraphShowYearsValue
        {
            get { return spinNYears.ValueAsInt; }
            set { spinNYears.Value = value; }
        }

        /// <summary>set the maximum value for the graph 'Year to display' </summary>
        public int GraphShowYearsMaxValue
        {
            set
            {
                double min, max;
                spinNYears.GetRange(out min, out max);
                spinNYears.SetRange(Math.Min(value, min), value);
            }
        }

        /// <summary>Show or hide the combobox listing the names of Excel worksheets </summary>
        /// <param name="show"></param>
        public void ShowExcelSheets(bool show)
        {
            if (show)
                hbox2.ShowAll();
            hbox2.Visible = show;
        }

        /// <summary>Populates the data.</summary>
        /// <param name="Data">The data.</param>
        public void PopulateData(DataTable data)
        {
            //fill the grid with data
            gridViewData.DataSource = data;
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
            worksheetCombo.Values = sheetNames.ToArray();
            PopulatingDropDownData = false;
        }

        /// <summary>Handles the Click event of the button1 control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnButton1Click(object sender, EventArgs e)
        {
            FileChooserDialog fileChooser = new FileChooserDialog("Choose a weather file to open", null, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

            FileFilter fileFilter = new FileFilter();
            fileFilter.Name = "APSIM Weather file (*.met)";
            fileFilter.AddPattern("*.met");
            fileChooser.AddFilter(fileFilter);

            FileFilter excelFilter = new FileFilter();
            excelFilter.Name = "Excel file (*.xlsx)";
            excelFilter.AddPattern("*.xlsx");
            fileChooser.AddFilter(excelFilter);

            FileFilter allFilter = new FileFilter();
            allFilter.Name = "All files";
            allFilter.AddPattern("*");
            fileChooser.AddFilter(allFilter);

            fileChooser.SetFilename(labelFileName.Text);

            if (fileChooser.Run() == (int)ResponseType.Accept)
            {
                Filename = fileChooser.Filename;
                fileChooser.Destroy();
                if (BrowseClicked != null)
                {
                    BrowseClicked.Invoke(Filename);    //reload the grid with data
                    notebook1.CurrentPage = 0;
                }

             }
             else
                fileChooser.Destroy();
        }

        /// <summary>Handles the change event for the GraphStartYear NumericUpDown </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnGraphStartYearValueChanged(object sender, EventArgs e)
        {
            if (GraphRefreshClicked != null)
                GraphRefreshClicked.Invoke(notebook1.CurrentPage, spinStartYear.ValueAsInt, spinNYears.ValueAsInt);
        }

        /// <summary>Handles the change event for the GraphShowYears NumericUpDown </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnGraphShowYearsValueChanged(object sender, EventArgs e)
        {
            if (GraphRefreshClicked != null)
                GraphRefreshClicked.Invoke(notebook1.CurrentPage, spinStartYear.ValueAsInt, spinNYears.ValueAsInt);
        }

        /// <summary>
        /// Handles the selection change between tabs, so that we can adjust the height of the Browse Panel,
        /// showing/or hiding information that is not relevant.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl1_SelectedIndexChanged(object sender, SwitchPageArgs e)
        {
            bool moved = false;
            switch (e.PageNum)
            {
                case 2:
                    if (hboxOptions.Parent != alignRainChart)
                    {
                        hboxOptions.Reparent(alignRainChart);
                        moved = true;
                    }
                    break;
                case 3:
                    if (hboxOptions.Parent != alignRainMonthly)
                    {
                        hboxOptions.Reparent(alignRainMonthly);
                        moved = true;
                    }
                    break;
                case 4:
                    if (hboxOptions.Parent != alignTemp)
                    {
                        hboxOptions.Reparent(alignTemp);
                        moved = true;
                    }
                    break;
                case 5:
                    if (hboxOptions.Parent != alignRadn)
                    {
                        hboxOptions.Reparent(alignRadn);
                        moved = true;
                    }
                    break;
                default: break;
            }

            if (moved)
            {
                // On Windows, at least, these controls don't move correctly with the reparented HBox.
                // They think they're parented correctly, but are drawn at 0,0 of the main window.
                // We can hack around this by reparenting them somewhere else, then moving them back.
                Widget pa = spinStartYear.Parent;
                spinStartYear.Reparent(MainWidget);
                spinStartYear.Reparent(pa);
                pa = spinNYears.Parent;
                spinNYears.Reparent(MainWidget);
                spinNYears.Reparent(pa);
            }
            if (GraphRefreshClicked != null)
                GraphRefreshClicked.Invoke(notebook1.CurrentPage, spinStartYear.ValueAsInt, spinNYears.ValueAsInt);
        }

        /// <summary>
        /// This is used to handle the change in value (selected index) for the worksheet dropdown combo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WorksheetCombo_Changed(object sender, EventArgs e)
        {
            if (ExcelSheetChangeClicked != null)
               ExcelSheetChangeClicked.Invoke(Filename, worksheetCombo.SelectedValue);
            notebook1.CurrentPage = 0;
        }
    }
}
