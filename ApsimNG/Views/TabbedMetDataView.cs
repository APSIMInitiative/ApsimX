﻿using System;
using System.Collections.Generic;
using System.Data;
using UserInterface.Interfaces;
using Gtk;

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
        public event BrowseDelegate ConstantsFileSelected;

        private Label labelFileName = null;
        private VBox vbox1 = null;
        private Notebook notebook1 = null;
        private TextView textview1 = null;
        private Alignment alignSummary = null;
        private Alignment alignData = null;
        private Alignment alignRainChart = null;
        private Alignment alignRainMonthly = null;
        private Alignment alignTemp = null;
        private Alignment alignRadn = null;
        private VBox vboxRainChart = null;
        private VBox vboxRainMonthly = null;
        private VBox vboxTemp = null;
        private VBox vboxRadn = null;
        private HBox hboxOptions = null;
        private SpinButton spinStartYear = null;
        private SpinButton spinNYears = null;
        private Button button1 = null;
        private VPaned vpaned1 = null;
        private HBox hbox2 = null;
        private Alignment alignment10 = null;
        private DropDownView worksheetCombo;
        private Button constantsFileSelector;
        private Container constantsFileSelectorContainer;
        private Label labelConstantsFileName;

        /// <summary>Initializes a new instance of the <see cref="TabbedMetDataView"/> class.</summary>
        public TabbedMetDataView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.TabbedMetDataView.glade");
            labelFileName = (Label)builder.GetObject("labelFileName");
            vbox1 = (VBox)builder.GetObject("vbox1");
            notebook1 = (Notebook)builder.GetObject("notebook1");
            textview1 = (TextView)builder.GetObject("textview1");
            alignSummary = (Alignment)builder.GetObject("alignSummary");
            alignData = (Alignment)builder.GetObject("alignData");
            alignRainChart = (Alignment)builder.GetObject("alignRainChart");
            alignRainMonthly = (Alignment)builder.GetObject("alignRainMonthly");
            alignTemp = (Alignment)builder.GetObject("alignTemp");
            alignRadn = (Alignment)builder.GetObject("alignRadn");
            vboxRainChart = (VBox)builder.GetObject("vboxRainChart");
            vboxRainMonthly = (VBox)builder.GetObject("vboxRainMonthly");
            vboxTemp = (VBox)builder.GetObject("vboxTemp");
            vboxRadn = (VBox)builder.GetObject("vboxRadn");
            hboxOptions = (HBox)builder.GetObject("hboxOptions");
            spinStartYear = (SpinButton)builder.GetObject("spinStartYear");
            spinNYears = (SpinButton)builder.GetObject("spinNYears");
            button1 = (Button)builder.GetObject("button1");
            vpaned1 = (VPaned)builder.GetObject("vpaned1");
            hbox2 = (HBox)builder.GetObject("hbox2");
            alignment10 = (Alignment)builder.GetObject("alignment10");
            constantsFileSelector = (Button)builder.GetObject("button2");
            constantsFileSelector.Clicked += OnChooseConstantsFile;
            constantsFileSelectorContainer = (Container)builder.GetObject("hbox3");
            labelConstantsFileName = (Label)builder.GetObject("labelFileName1");
            mainWidget = vbox1;
            graphViewSummary = new GraphView(this);
            alignSummary.Add(graphViewSummary.MainWidget);
            graphViewRainfall = new GraphView(this);
            vboxRainChart.PackEnd(graphViewRainfall.MainWidget, true, true, 0);
            graphViewMonthlyRainfall = new GraphView(this);
            vboxRainMonthly.PackEnd(graphViewMonthlyRainfall.MainWidget, true, true, 0);
            graphViewTemperature = new GraphView(this);
            vboxTemp.PackEnd(graphViewTemperature.MainWidget, true, true, 0);
            graphViewRadiation = new GraphView(this);
            vboxRadn.PackEnd(graphViewRadiation.MainWidget, true, true, 0);
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
            worksheetCombo.Visible = true;
            worksheetCombo.Changed += WorksheetCombo_Changed;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                button1.Clicked -= OnButton1Click;
                spinStartYear.ValueChanged -= OnGraphStartYearValueChanged;
                spinNYears.ValueChanged -= OnGraphShowYearsValueChanged;
                notebook1.SwitchPage -= TabControl1_SelectedIndexChanged;
                worksheetCombo.Changed -= WorksheetCombo_Changed;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Gets or sets the filename.</summary>
        /// <value>The filename.</value>
        public string Filename
        {
            get { return labelFileName.Text; }
            set { labelFileName.Text = value; }
        }

        /// <summary>Gets or sets the filename.</summary>
        /// <value>The filename.</value>
        public string ConstantsFileName
        {
            get { return labelConstantsFileName.Text; }
            set { labelConstantsFileName.Text = value; }
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

        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        public int TabIndex
        {
            get { return notebook1.CurrentPage; }
            set { notebook1.CurrentPage = value; }
        }

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
        /// <param name="data">The data.</param>
        public void PopulateData(DataTable data)
        {
            //fill the grid with data
            gridViewData.DataSource = data;
        }

        /// <summary>
        /// Populates the DropDown of Excel WorksheetNames 
        /// </summary>
        /// <param name="sheetNames"></param>
        public void PopulateDropDownData(List<string> sheetNames)
        {
            worksheetCombo.Values = sheetNames.ToArray();
        }

        private void OnChooseConstantsFile(object sender, EventArgs e)
        {
            try
            {
                string fileName = AskUserForFileName("Choose a constants file to open", Utility.FileDialog.FileActionType.Open, "Plain text file (*.txt)|*.txt");
                if (!String.IsNullOrEmpty(fileName))
                {
                    ConstantsFileName = fileName;
                    if (ConstantsFileSelected != null)
                    {
                        ConstantsFileSelected.Invoke(fileName);
                        notebook1.CurrentPage = 0;
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Handles the Click event of the button1 control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnButton1Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = AskUserForFileName("Choose a weather file to open", Utility.FileDialog.FileActionType.Open, "APSIM Weather file (*.met)|*.met|Excel file(*.xlsx)|*.xlsx|CSV file(*.csv)|*.csv", labelFileName.Text);
                if (!String.IsNullOrEmpty(fileName))
                {
                    Filename = fileName;
                    if (BrowseClicked != null)
                    {
                        BrowseClicked.Invoke(Filename);    //reload the grid with data
                        notebook1.CurrentPage = 0;
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Handles the change event for the GraphStartYear NumericUpDown </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnGraphStartYearValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (GraphRefreshClicked != null)
                    GraphRefreshClicked.Invoke(notebook1.CurrentPage, spinStartYear.ValueAsInt, spinNYears.ValueAsInt);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Handles the change event for the GraphShowYears NumericUpDown </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnGraphShowYearsValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (GraphRefreshClicked != null)
                    GraphRefreshClicked.Invoke(notebook1.CurrentPage, spinStartYear.ValueAsInt, spinNYears.ValueAsInt);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handles the selection change between tabs, so that we can adjust the height of the Browse Panel,
        /// showing/or hiding information that is not relevant.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl1_SelectedIndexChanged(object sender, SwitchPageArgs e)
        {
            try
            {
                bool moved = false;
                switch (e.PageNum)
                {
                    case 2:
                        if (hboxOptions.Parent != alignRainChart)
                        {
                            if (hboxOptions.Parent is Container container)
                                container.Remove(hboxOptions);

                            alignRainChart.Add(hboxOptions);
                            moved = true;
                        }
                        break;
                    case 3:
                        if (hboxOptions.Parent != alignRainMonthly)
                        {
                            if (hboxOptions.Parent is Container container)
                                container.Remove(hboxOptions);

                            alignRainMonthly.Add(hboxOptions);
                            moved = true;
                        }
                        break;
                    case 4:
                        if (hboxOptions.Parent != alignTemp)
                        {
                            if (hboxOptions.Parent is Container container)
                                container.Remove(hboxOptions);

                            alignTemp.Add(hboxOptions);
                            moved = true;
                        }
                        break;
                    case 5:
                        if (hboxOptions.Parent != alignRadn)
                        {
                            if (hboxOptions.Parent is Container container)
                                container.Remove(hboxOptions);

                            alignRadn.Add(hboxOptions);
                            moved = true;
                        }
                        break;
                    default: break;
                }

                if (moved)
                {
                    // The SpinButton controls lose their font information when they are reparented. We restore it here.
                    Container pa = spinStartYear.Parent as Container;
                    Pango.FontDescription font = pa.PangoContext.FontDescription;
                    spinStartYear.PangoContext.FontDescription = font; 
                    spinNYears.PangoContext.FontDescription = font;
                }
                if (GraphRefreshClicked != null)
                    GraphRefreshClicked.Invoke(notebook1.CurrentPage, spinStartYear.ValueAsInt, spinNYears.ValueAsInt);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// This is used to handle the change in value (selected index) for the worksheet dropdown combo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WorksheetCombo_Changed(object sender, EventArgs e)
        {
            try
            {
                if (ExcelSheetChangeClicked != null)
                    ExcelSheetChangeClicked.Invoke(Filename, worksheetCombo.SelectedValue);
                notebook1.CurrentPage = 0;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        public void ShowConstantsFile(bool show)
        {
            if (show)
                constantsFileSelectorContainer.ShowAll();
            else
                constantsFileSelectorContainer.Hide();
        }
    }
    /// <summary>
    /// An interface for a weather data view
    /// </summary>
    interface IMetDataView
    {
        /// <summary>Occurs when browse button is clicked</summary>
        event BrowseDelegate BrowseClicked;

        /// <summary>Occurs when a constants file is selected.</summary>
        event BrowseDelegate ConstantsFileSelected;

        /// <summary>Occurs when the start year numericUpDown is clicked</summary>
        event GraphRefreshDelegate GraphRefreshClicked;

        /// <summary>A delegate used when the sheetname dropdown value change is actived</summary>
        event ExcelSheetDelegate ExcelSheetChangeClicked;

        /// <summary>Gets or sets the filename.</summary>
        string Filename { get; set; }

        /// <summary>Gets or sets the filename.</summary>
        string ConstantsFileName { get; set; }

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

        /// <summary>Show or hide the constants file selector.</summary>
        /// <param name="show">If true, the selector will be shown, otherwise it will be hidden.</param>
        void ShowConstantsFile(bool show);

        /// <summary>sets/gets the value of 'Show Years' NumericUpDown control </summary>
        int GraphShowYearsValue { get; set; }

        /// <summary>set the maximum value for the 'Show Years' NumericUpDown control  </summary>
        int GraphShowYearsMaxValue { set; }

        /// <summary>Populates the data grid</summary>
        /// <param name="data">The data</param>
        void PopulateData(DataTable data);

        /// <summary>
        /// Populates the DropDown of Excel WorksheetNames 
        /// </summary>
        /// <param name="sheetNames"></param>
        void PopulateDropDownData(List<string> sheetNames);

        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        int TabIndex { get; set; }
    }
}
