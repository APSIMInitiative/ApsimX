using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using UserInterface.Commands;
using Models.Climate;
using Models.Core;
using UserInterface.Views;
using System.Linq;
using Gtk.Sheet;

namespace UserInterface.Presenters
{
    /// <summary>A presenter for displaying weather data</summary>
    public sealed class MetDataPresenter : IPresenter, IDisposable
    {
        /// <summary>The met data</summary>
        private Weather weatherData;

        /// <summary>The met data view</summary>
        private IMetDataView weatherDataView;

        /// <summary>The sheet widget.</summary>
        private GridPresenter gridPresenter;

        /// <summary>Hold the data used by the graphs</summary>
        private DataTable graphMetData;

        /// <summary>
        /// The list of sheet names
        /// </summary>
        private List<string> sheetNames;

        /// <summary>Hold the first date in datatable, for use in the graphs</summary>
        private DateTime dataFirstDate;

        /// <summary>Hold the last date in datatable, for use in the graphs</summary>
        private DateTime dataLastDate;

        /// <summary>Hold the first date in datatable; may include partial years</summary>
        private DateTime dataStartDate;

        /// <summary>Hold the last date in datatable; may include partial years</summary>
        private DateTime dataEndDate;

        /// <summary>Hold an array of months for the graph,  by default, is set to will Jan yyyy to Dec yyyy, except where
        /// data being displays is not for full year</summary>
        private string[] monthsToDisplay = DateUtilities.MONTHS_3_LETTERS;

        /// <summary>The explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Attaches the specified model.</summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.explorerPresenter = explorerPresenter;
            this.weatherData = model as Weather;
            this.weatherDataView = view as IMetDataView;

            ContainerView sheetContainer = this.weatherDataView.container;

            gridPresenter = new GridPresenter();
            gridPresenter.Attach(new DataTableProvider(new DataTable()), sheetContainer, explorerPresenter);
            gridPresenter.AddContextMenuOptions(new string[] { "Copy", "Select All" });

            this.weatherDataView.BrowseClicked += this.OnBrowse;
            this.weatherDataView.GraphRefreshClicked += this.GraphRefreshValueChanged;
            this.weatherDataView.ConstantsFileSelected += OnConstantsFileSelected;
            this.weatherDataView.ExcelSheetChangeClicked += this.ExcelSheetValueChanged;

            this.weatherDataView.ShowConstantsFile(Path.GetExtension(weatherData.FullFileName) == ".csv");
            this.WriteTableAndSummary(this.weatherData.FullFileName, this.weatherData.ExcelWorkSheetName);
            this.weatherDataView.TabIndex = this.weatherData.ActiveTabIndex;
            if (this.weatherData.StartYear >= 0)
                this.weatherDataView.GraphStartYearValue = this.weatherData.StartYear;
            this.weatherDataView.GraphShowYearsValue = this.weatherData.ShowYears;
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.weatherData.ActiveTabIndex = this.weatherDataView.TabIndex;
            this.weatherData.StartYear = this.weatherDataView.GraphStartYearValue;
            this.weatherData.ShowYears = this.weatherDataView.GraphShowYearsValue;
            this.weatherDataView.BrowseClicked -= this.OnBrowse;
            this.weatherDataView.GraphRefreshClicked -= this.GraphRefreshValueChanged;
            this.weatherDataView.ExcelSheetChangeClicked -= this.ExcelSheetValueChanged;
            this.weatherDataView.ConstantsFileSelected -= OnConstantsFileSelected;
        }

        /// <summary>Called after the user has selected a new met file.</summary>
        /// <param name="fileName">Name of the file.</param>
        public void OnBrowse(string fileName)
        {
            bool isCsv = Path.GetExtension(fileName) == ".csv";
            this.weatherDataView.ShowConstantsFile(isCsv);
            if (this.weatherData.FullFileName != PathUtilities.GetAbsolutePath(fileName, this.explorerPresenter.ApsimXFile.FileName))
            {
                if (ExcelUtilities.IsExcelFile(fileName))
                {
                    // Extend height of Browse Panel to show Drop Down for Sheet names
                    this.weatherDataView.ShowExcelSheets(true);
                    this.sheetNames = ExcelUtilities.GetWorkSheetNames(fileName);
                    this.weatherDataView.PopulateDropDownData(this.sheetNames);

                    // We want to attempt to update the table/summary now. This may fail if the
                    // sheet name is incorrect/not set.
                    this.WriteTableAndSummary(fileName);
                }
                else
                {
                    // Shrink Browse Panel so that the sheet name dropdown doesn't show
                    this.weatherDataView.ShowExcelSheets(false);

                    // as a precaution, set this to nothing
                    this.weatherData.ExcelWorkSheetName = string.Empty;
                    this.WriteTableAndSummary(fileName);
                }
            }
        }

        private void OnConstantsFileSelected(string fileName)
        {
            ICommand changeConstantsFile = new ChangeProperty(weatherData, nameof(weatherData.ConstantsFile), fileName);
            explorerPresenter.CommandHistory.Add(changeConstantsFile);
            WriteTableAndSummary(weatherData.FileName);
        }

        /// <summary>
        /// This is called when the Graph StartYear or the Graphing ShowYears Numeric updown controls are changed by the user.
        /// It refreshes the graphs accordingly.
        /// </summary>
        /// <param name="tabIndex">The tab</param>
        /// <param name="startYear">The start year</param>
        /// <param name="showYears">Number of years to show</param>
        public void GraphRefreshValueChanged(int tabIndex, decimal startYear, decimal showYears)
        {
            try
            {
                using (DataTable data = this.graphMetData)
                {
                    DateTime startDate = new DateTime(Convert.ToInt16(startYear, CultureInfo.InvariantCulture), 1, 1);
                    DateTime endDate = new DateTime(Convert.ToInt16(startYear, CultureInfo.InvariantCulture), 12, 31);
                    if (showYears > 1)
                    {
                        endDate = endDate.AddYears(Convert.ToInt16(showYears, CultureInfo.InvariantCulture) - 1);
                    }

                    this.DisplayDetailedGraphs(data, tabIndex, startDate, endDate, false);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// This is called when the value of DropDown combo list containing sheet names is changed.
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <param name="sheetName">The sheet name</param>
        public void ExcelSheetValueChanged(string fileName, string sheetName)
        {
            if (!string.IsNullOrEmpty(sheetName))
            {
                if ((this.weatherData.FullFileName != PathUtilities.GetAbsolutePath(fileName, this.explorerPresenter.ApsimXFile.FileName)) ||
                    (this.weatherData.ExcelWorkSheetName != sheetName))
                {
                    this.WriteTableAndSummary(fileName, sheetName);
                }
            }
        }

        /// <summary>
        /// Get data from the weather file and present it to the view as both a table and a summary
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="sheetName">The name of the sheet</param>
        private void WriteTableAndSummary(string filename, string sheetName = "")
        {
            // Clear any previos error message
            this.explorerPresenter.MainPresenter.ShowMessage(" ", Simulation.MessageType.Information);
            // Clear any previous summary
            this.weatherDataView.Summarylabel = string.Empty;
            this.weatherDataView.GraphSummary.Clear();
            this.weatherDataView.GraphSummary.Refresh();
            this.weatherDataView.GraphRainfall.Clear();
            this.weatherDataView.GraphRainfall.Refresh();
            this.weatherDataView.GraphMonthlyRainfall.Clear();
            this.weatherDataView.GraphMonthlyRainfall.Refresh();
            this.weatherDataView.GraphTemperature.Clear();
            this.weatherDataView.GraphTemperature.Refresh();
            this.weatherDataView.GraphRadiation.Clear();
            this.weatherDataView.GraphRadiation.Refresh();
            this.graphMetData = new DataTable();
            if (filename != null)
            {
                try
                {
                    if (ExcelUtilities.IsExcelFile(filename))
                    {
                        // Extend height of Browse Panel to show Drop Down for Sheet names
                        this.weatherDataView.ShowExcelSheets(true);
                        if (this.sheetNames == null)
                        {
                            this.sheetNames = ExcelUtilities.GetWorkSheetNames(filename);
                            this.weatherDataView.ExcelSheetChangeClicked -= this.ExcelSheetValueChanged;
                            this.weatherDataView.PopulateDropDownData(this.sheetNames);
                            this.weatherDataView.ExcelSheetChangeClicked += this.ExcelSheetValueChanged;
                        }
                    }
                    else
                    {
                        // Shrink Browse Panel so that the sheet name dropdown doesn't show
                        this.weatherDataView.ShowExcelSheets(false);
                    }

                    ViewBase.MasterView.WaitCursor = true;
                    try
                    {
                        this.weatherData.ExcelWorkSheetName = sheetName;
                        string newFileName = PathUtilities.GetAbsolutePath(filename, this.explorerPresenter.ApsimXFile.FileName);
                        var changes = new List<ChangeProperty.Property>();
                        if (weatherData.FullFileName != newFileName)
                            changes.Add(new ChangeProperty.Property(weatherData, nameof(weatherData.FullFileName), newFileName));
                        // Set constants file name to null iff the new file name is not a csv file.
                        if (Path.GetExtension(newFileName) != ".csv" && weatherData.ConstantsFile != null)
                            changes.Add(new ChangeProperty.Property(weatherData, nameof(weatherData.ConstantsFile), null));
                        if (changes.Count > 0)
                        {
                            ICommand changeFileName = new ChangeProperty(changes);
                            explorerPresenter.CommandHistory.Add(new ChangeProperty(changes));
                        }
                        using (DataTable data = this.weatherData.GetAllData())
                        {
                            this.dataStartDate = this.weatherData.StartDate;
                            this.dataEndDate = this.weatherData.EndDate;
                            this.WriteTable(data);
                            this.WriteSummary(data);
                            this.DisplayDetailedGraphs(data);
                        }

                    }
                    catch (Exception err)
                    {
                        explorerPresenter.MainPresenter.ShowError(err);
                    }
                    finally
                    {
                        ViewBase.MasterView.WaitCursor = false;
                        this.weatherData.CloseDataFile();
                    }
                }
                catch (Exception err)
                {
                    string message = err.Message;
                    message += "\r\n" + err.StackTrace;
                    this.weatherDataView.Summarylabel = err.Message;
                    this.explorerPresenter.MainPresenter.ShowError(err);
                }
            }

            string fullFilePath = PathUtilities.GetAbsolutePath(filename, this.explorerPresenter.ApsimXFile.FileName);
            string relativeFilePath = fullFilePath;
            Simulations simulations = weatherData.FindAncestor<Simulations>();
            if (simulations != null)
                relativeFilePath = PathUtilities.GetRelativePathAndRootExamples(filename, simulations.FileName);

            this.weatherDataView.Filename = fullFilePath;
            this.weatherDataView.FilenameRelative = relativeFilePath;
            this.weatherDataView.ConstantsFileName = weatherData.ConstantsFile;
            this.weatherDataView.ExcelWorkSheetName = sheetName;
        }

        /// <summary>Send the DataTable to the View</summary>
        /// <param name="data">The data set</param>
        private void WriteTable(DataTable data)
        {
            // format the data into useful columns
            if (data != null)
            {
                int siteIdx = data.Columns.IndexOf("site");
                if (siteIdx >= 0)
                {
                    data.Columns.RemoveAt(siteIdx);
                }

                // modLMC - 10/03/2016 - Add the Qmax (Max Radiation) column that we require for the graphs
                // This is done here so that we can use the "day" or "doy" column if it exists, as it will be quicker
                MetUtilities.CalcQmax(data, this.weatherData.Latitude);

                // modLMC - 10/03/2016 - Modified to use this new function, as some data has "doy" and not "day"
                int dayCol = data.Columns.IndexOf("day");
                int yearCol = data.Columns.IndexOf("year");

                if ((yearCol >= 0) && (dayCol >= 0))
                {
                    // add a new column for the date string
                    DataColumn dateCol = data.Columns.Add("Date", typeof(DateTime));
                    dateCol.SetOrdinal(0);
                    yearCol++;    // moved along
                    dayCol++;

                    int yr, day;

                    // for each row in the grid
                    for (int r = 0; r < data.Rows.Count; r++)
                    {
                        DateTime rowDate;
                        try
                        {
                            yr = Convert.ToInt32(data.Rows[r][yearCol], CultureInfo.InvariantCulture);
                            day = Convert.ToInt32(data.Rows[r][dayCol], CultureInfo.InvariantCulture);
                            rowDate = new DateTime(yr, 1, 1);
                        }
                        catch (Exception err)
                        {
                            DateTime previousRowDate;
                            if (r > 0 && DateTime.TryParse((string)data.Rows[r - 1][0], out previousRowDate))
                                throw new Exception("Invalid date detected in file: " + this.weatherData.FileName + ". Previous row: " + previousRowDate.ToShortDateString() + " (day of year = " + previousRowDate.DayOfYear + ")");
                            else
                                throw new Exception("Encountered an error while parsing date: " + err.Message);
                        }
                        rowDate = rowDate.AddDays(day - 1);   // calc date
                        data.Rows[r][0] = rowDate;
                    }

                    if (dayCol > yearCol)
                    {
                        data.Columns.RemoveAt(dayCol);
                        data.Columns.RemoveAt(yearCol);       // remove unwanted columns
                    }
                    else
                    {
                        data.Columns.RemoveAt(yearCol);       // remove unwanted columns
                        data.Columns.RemoveAt(dayCol);
                    }
                }

                this.graphMetData = data;
                this.PopulateData(data);
            }
        }

        /// <summary>Format a summary string about the weather file</summary>
        /// <param name="table">The data set</param>
        private void WriteSummary(DataTable table)
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("File name : " + Path.GetFileName(this.weatherData.FileName));
            if (!string.IsNullOrEmpty(this.weatherData.ExcelWorkSheetName))
            {
                summary.AppendLine("Sheet Name: " + this.weatherData.ExcelWorkSheetName.ToString());
            }

            foreach (string validationMessage in weatherData.Validate())
                summary.AppendLine($"WARNING: {validationMessage}");

            summary.AppendLine("Latitude  : " + this.weatherData.Latitude.ToString());
            summary.AppendLine("Longitude : " + this.weatherData.Longitude.ToString());
            summary.AppendLine("TAV       : " + string.Format("{0, 2:f2}", this.weatherData.Tav));
            summary.AppendLine("AMP       : " + string.Format("{0, 2:f2}", this.weatherData.Amp));
            summary.AppendLine("Start     : " + this.dataStartDate.ToShortDateString());
            summary.AppendLine("End       : " + this.dataEndDate.ToShortDateString());
            summary.AppendLine(string.Empty);

            if (table != null && table.Rows.Count > 0)
            {
                this.dataFirstDate = DataTableUtilities.GetDateFromRow(table.Rows[0]);
                this.dataLastDate = DataTableUtilities.GetDateFromRow(table.Rows[table.Rows.Count - 1]);

                TimeSpan diff = this.dataLastDate - this.dataFirstDate;

                // modLMC - 16/03/2016 - don't change dates if data is within the same year
                if (diff.Days > 365)
                {
                    if (this.dataFirstDate.DayOfYear != 1)
                    {
                        this.dataFirstDate = new DateTime(this.dataFirstDate.Year + 1, 1, 1);
                    }
                }

                // modLMC - 16/03/2016 - don't change dates if data is within the same year
                if (this.dataFirstDate.Year != this.dataLastDate.Year)
                {
                    if (this.dataLastDate.Day != 31 || this.dataLastDate.Month != 12)
                    {
                        this.dataLastDate = new DateTime(this.dataLastDate.Year - 1, 12, 31);
                    }
                }

                double[] yearlyRainfall = DataTableUtilities.YearlyTotals(table, "Rain", this.dataFirstDate, this.dataLastDate);
                double[] monthlyRainfall = DataTableUtilities.AverageMonthlyTotals(table, "rain", this.dataFirstDate, this.dataLastDate);
                double[] monthlyMaxT = DataTableUtilities.AverageMonthlyAverages(table, "maxt", this.dataFirstDate, this.dataLastDate);
                double[] monthlyMinT = DataTableUtilities.AverageMonthlyAverages(table, "mint", this.dataFirstDate, this.dataLastDate);

                // what do we do if the date range is less than 1 year.
                // modlmc - 15/03/2016 - modified to pass in the "Month" values, and they may/may not contain a full year.
                if (monthlyRainfall.Length <= 12)
                {
                    this.monthsToDisplay = DataTableUtilities.GetDistinctMonthsasStrings(table, this.dataFirstDate, this.dataLastDate);
                }

                // long term average rainfall
                if (yearlyRainfall.Length != 0)
                {
                    double totalYearlyRainfall = MathUtilities.Sum(yearlyRainfall);
                    int numYears = this.dataLastDate.Year - this.dataFirstDate.Year + 1;
                    double meanYearlyRainfall = totalYearlyRainfall / numYears;
                    double stddev = MathUtilities.StandardDeviation(yearlyRainfall);

                    summary.AppendLine(string.Format("For years : {0} - {1}", this.dataFirstDate.Year, this.dataLastDate.Year));
                    summary.AppendLine("Long term average yearly rainfall : " + string.Format("{0,3:f2}mm", meanYearlyRainfall));
                    summary.AppendLine("Yearly rainfall std deviation     : " + string.Format("{0,3:f2}mm", stddev));

                    string title = string.Format("Long term average data for years : {0} - {1}", this.dataFirstDate.Year, this.dataLastDate.Year);

                    // modlmc - 15/03/2016 - modified to pass in the "Month" values, and they may/may not contain a full year.
                    this.PopulateSummaryGraph(
                                        title,
                                        this.monthsToDisplay,
                                        monthlyRainfall,
                                        monthlyMaxT,
                                        monthlyMinT);
                }

                this.weatherDataView.Summarylabel = summary.ToString();
            }
        }

        /// <summary>Sets the date range for the graphs, and calls the graph display functions</summary>
        /// <param name="table">The data set</param>
        private void DisplayDetailedGraphs(DataTable table)
        {
            if (table != null && table.Rows.Count > 0)
            {
                // By default, only do one year (the first year)
                DateTime endDate = new DateTime(this.dataFirstDate.Year, 12, 31);

                // by default, assume if not passed in, then we are displaying first tab (tab 0)
                this.DisplayDetailedGraphs(table, 0, this.dataFirstDate, endDate, true);
            }
        }

        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table">The data table</param>
        /// <param name="tabIndex">The index of the tab</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="updateYears">Update the years</param>
        private void DisplayDetailedGraphs(DataTable table, int tabIndex, DateTime startDate, DateTime endDate, bool updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    switch (tabIndex)
                    {
                        case 2:     // Daily Rain
                            this.DisplayGraphDailyRain(table, startDate, endDate, true);
                            break;
                        case 3:     // Monthly Rain
                            this.DisplayGraphMonthlyRain(table, startDate, endDate, true);
                            break;
                        case 4:     // Temperature
                            this.DisplayGraphTemperature(table, startDate, endDate, true);
                            break;
                        case 5:     // Radiation
                            this.DisplayGraphRadiation(table, startDate, endDate, true);
                            break;
                    }

                    if (updateYears == true)
                    {
                        this.SetGraphControlsDefaultValues();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to display Detailed Graphs due to insufficient data: " + e.Message.ToString());
            }
        }

        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table">The data set</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="updateYears">Update the years</param>
        private void DisplayGraphDailyRain(DataTable table, DateTime startDate, DateTime endDate, bool updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    // Need to be able to filter the table based on first date and last date, so that we can graph
                    // graph the daily values for rainfall, temperature and radiation
                    DateTime[] dailyDates = DataTableUtilities.GetColumnAsDates(table, "Date", startDate, endDate);
                    double[] dailyRain = DataTableUtilities.GetColumnAsDoubles(table, "rain", startDate, endDate);

                    string rainMessage = string.Empty;
                    if (dailyRain.Length != 0)
                    {
                        if (startDate.Year == endDate.Year)
                        {
                            double totalRainfall = Math.Round(MathUtilities.Sum(dailyRain), 1);
                            rainMessage = "Total Rainfall for the year " + startDate.Year.ToString()
                                        + " is " + totalRainfall.ToString() + " mm.";
                        }
                        else
                        {
                            double meanRainfall = Math.Round(MathUtilities.Sum(dailyRain) / ((endDate - startDate).TotalDays + 1) * 365.25, 1);
                            rainMessage = "Mean rainfall for the years " + startDate.Year.ToString()
                                        + " to " + endDate.Year.ToString()
                                        + " is " + meanRainfall.ToString() + " mm/yr.";
                        }
                        this.PopulateRainfallGraph(rainMessage, dailyDates, dailyRain);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to display Detailed Graphs due to insufficient data: " + e.Message.ToString());
            }
        }

        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table">The data set</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="updateYears">Update the years</param>
        private void DisplayGraphMonthlyRain(DataTable table, DateTime startDate, DateTime endDate, bool updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    double[] monthlyRainfall = DataTableUtilities.AverageMonthlyTotals(table, "rain", startDate, endDate);

                    if (monthlyRainfall.Length != 0)
                    {
                        double[] avgMonthlyRainfall = DataTableUtilities.AverageMonthlyTotals(table, "rain", this.dataFirstDate, this.dataLastDate);
                        this.PopulateMonthlyRainfallGraph(
                                                       "Monthly Rainfall",
                                                        this.monthsToDisplay, 
                                                        monthlyRainfall, 
                                                        avgMonthlyRainfall);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to display Detailed Graphs due to insufficient data: " + e.Message.ToString());
            }
        }

        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table">The data set</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="updateYears">Update the years</param>
        private void DisplayGraphTemperature(DataTable table, DateTime startDate, DateTime endDate, bool updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    DateTime[] dailyDates = DataTableUtilities.GetColumnAsDates(table, "Date", startDate, endDate);
                    double[] dailyMaxTemp = DataTableUtilities.GetColumnAsDoubles(table, "maxt", startDate, endDate);
                    double[] dailyMinTemp = DataTableUtilities.GetColumnAsDoubles(table, "mint", startDate, endDate);

                    if (dailyMaxTemp.Length != 0)
                    {
                        this.PopulateTemperatureGraph("Temperature", dailyDates, dailyMaxTemp, dailyMinTemp);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to display Detailed Graphs due to insufficient data: " + e.Message.ToString());
            }
        }

        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table">The data set</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="updateYears">Update the years</param>
        private void DisplayGraphRadiation(DataTable table, DateTime startDate, DateTime endDate, bool updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    DateTime[] dailyDates = DataTableUtilities.GetColumnAsDates(table, "Date", startDate, endDate);
                    double[] dailyRain = DataTableUtilities.GetColumnAsDoubles(table, "rain", startDate, endDate);
                    double[] dailyRadn = DataTableUtilities.GetColumnAsDoubles(table, "radn", startDate, endDate);
                    double[] dailyMaxRadn = DataTableUtilities.GetColumnAsDoubles(table, "Qmax", startDate, endDate);

                    if (dailyRadn.Length != 0)
                    {
                        this.PopulateRadiationGraph("Radiation", dailyDates, dailyRain, dailyRadn, dailyMaxRadn);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to display Detailed Graphs due to insufficient data: " + e.Message.ToString());
            }
        }

        /// <summary>
        /// Sets the default values for the Numeric Updown spin controls used for displaying graph data (Start Year and Years to Show).
        /// </summary>
        private void SetGraphControlsDefaultValues()
        {
            // Options for new Min Year:  less than current, greater than current, and greater than max
            // if less, just set it first, then value, then max
            // else is greater than max, then do max first, then value set value to max, set min, then reset value
            // if greater than current but less than max, then set value first, then min, then max
            if (this.dataStartDate.Year < this.weatherDataView.GraphStartYearMinValue)
            {
                this.weatherDataView.GraphStartYearMinValue = this.dataStartDate.Year;
                this.weatherDataView.GraphStartYearValue = this.dataStartDate.Year;
                this.weatherDataView.GraphStartYearMaxValue = this.dataEndDate.Year;
            }
            else if (this.weatherDataView.GraphStartYearMinValue >= this.dataEndDate.Year)
            {
                this.weatherDataView.GraphStartYearMaxValue = this.dataEndDate.Year;
                this.weatherDataView.GraphStartYearValue = this.dataEndDate.Year;
                this.weatherDataView.GraphStartYearMinValue = this.dataStartDate.Year;
                this.weatherDataView.GraphStartYearValue = this.dataStartDate.Year;
            }
            else  
            {
                // we are between our original range
                if (this.weatherDataView.GraphStartYearMinValue < this.dataStartDate.Year)
                {
                    this.weatherDataView.GraphStartYearMinValue = this.dataStartDate.Year;
                    this.weatherDataView.GraphStartYearValue = this.dataStartDate.Year;
                }
                else
                {
                    this.weatherDataView.GraphStartYearValue = this.dataStartDate.Year;
                    this.weatherDataView.GraphStartYearMinValue = this.dataStartDate.Year;
                }

                this.weatherDataView.GraphStartYearMaxValue = this.dataEndDate.Year;
            }
            int oldValue = this.weatherDataView.GraphShowYearsValue;
            this.weatherDataView.GraphShowYearsValue = 1;
            int maxNYears = this.dataEndDate.Year - this.dataStartDate.Year + 1;
            this.weatherDataView.GraphShowYearsMaxValue = maxNYears;
            this.weatherDataView.GraphShowYearsValue = Math.Min(oldValue, maxNYears);
        }

        /// <summary>Create the monthly Summary chart</summary>
        /// <param name="title">The title</param>
        /// <param name="months">Array of months</param>
        /// <param name="monthlyRain">Monthly rainfall</param>
        /// <param name="monthlyMaxT">Monthly Maximum Temperatures</param>
        /// <param name="monthlyMinT">Monthly Minimum Temperatures</param>
        private void PopulateSummaryGraph(string title, string[] months, double[] monthlyRain, double[] monthlyMaxT, double[] monthlyMinT)
        {
            this.weatherDataView.GraphSummary.Clear();
            this.weatherDataView.GraphSummary.DrawBar(
                                      "Rainfall",
                                      months,
                                      monthlyRain,
                                      AxisPosition.Bottom,
                                      AxisPosition.Left,
                                      Color.LightSkyBlue,
                                      true);
            this.weatherDataView.GraphSummary.DrawLineAndMarkers(
                                                     "Maximum Temperature",
                                                     months,
                                                     monthlyMaxT,
                                                     null,
                                                     null,
                                                     null,
                                                     null,
                                                     AxisPosition.Bottom,
                                                     AxisPosition.Right,
                                                     Color.Red,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThickness.Normal,
                                                     MarkerSize.Normal,
                                                     1,
                                                     true);
            this.weatherDataView.GraphSummary.DrawLineAndMarkers(
                                                     "Minimum Temperature",
                                                     months,
                                                     monthlyMinT,
                                                     null,
                                                     null,
                                                     null,
                                                     null,
                                                     AxisPosition.Bottom,
                                                     AxisPosition.Right,
                                                     Color.Orange,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThickness.Normal,
                                                     MarkerSize.Normal,
                                                     1,
                                                     true);

            double startDate = 0;
            double endDate = months.Length;
            double minTemp = MathUtilities.Min(monthlyMinT);
            double maxTemp = MathUtilities.Max(monthlyMaxT);
            double minRain = 0;
            double maxRain = MathUtilities.Max(monthlyRain);

            this.weatherDataView.GraphSummary.FormatAxis(AxisPosition.Bottom, "Month", false, startDate, endDate, double.NaN, false, false);
            this.weatherDataView.GraphSummary.FormatAxis(AxisPosition.Left, "Rainfall (mm)", false, minRain, maxRain, double.NaN, false, false);
            this.weatherDataView.GraphSummary.FormatAxis(AxisPosition.Right, "Temperature (oC)", false, minTemp, maxTemp, double.NaN, false, false);
            this.weatherDataView.GraphSummary.FormatTitle(title);
            this.weatherDataView.GraphSummary.Refresh();
        }

        /// <summary>Creates the Rainfall Chart</summary>
        /// <param name="title">The title to display on the chart</param>
        /// <param name="dates">An array of Dates for the x Axis</param>
        /// <param name="rain">An array of Rainfall amounts for the Y Axis</param>
        private void PopulateRainfallGraph(string title, DateTime[] dates, double[] rain)
        {
            this.weatherDataView.GraphRainfall.Clear();
            this.weatherDataView.GraphRainfall.DrawBar(
                                                       title,
                                                       dates,
                                                       rain,
                                                       AxisPosition.Bottom,
                                                       AxisPosition.Left,
                                                       Color.LightSkyBlue,
                                                       false);

            double startDate = dates.Min<DateTime>().ToOADate();
            double endDate = dates.Max<DateTime>().ToOADate();
            double minVal = 0;
            double maxVal = MathUtilities.Max(rain);

            this.weatherDataView.GraphRainfall.FormatAxis(AxisPosition.Bottom, "Date", false, startDate, endDate, double.NaN, false, false);
            this.weatherDataView.GraphRainfall.FormatAxis(AxisPosition.Left, "Rainfall (mm)", false, minVal, maxVal, double.NaN, false, false);
            this.weatherDataView.GraphRainfall.FormatTitle(title);
            this.weatherDataView.GraphRainfall.Refresh();
        }

        /// <summary>
        /// Displays the Monthly rainfall chart, which shows the current years rain (by month), and the long term average monthly rainfall, 
        /// based on all data in metfile
        /// </summary>
        /// <param name="title">The title</param>
        /// <param name="months">Array of months</param>
        /// <param name="monthlyRain">Monthly rain data</param>
        /// <param name="avgMonthlyRain">Average monthly rain</param>
        private void PopulateMonthlyRainfallGraph(string title, string[] months, double[] monthlyRain, double[] avgMonthlyRain)
        {
            this.weatherDataView.GraphMonthlyRainfall.Clear();
            if (months.Length == monthlyRain.Length)
            {
                this.weatherDataView.GraphMonthlyRainfall.DrawBar(
                                                           title,
                                                           months,
                                                           monthlyRain,
                                                           AxisPosition.Bottom,
                                                           AxisPosition.Left,
                                                           Color.LightSkyBlue,
                                                           true);
            }

            if ((avgMonthlyRain.Length != 0) && (avgMonthlyRain.Length == monthlyRain.Length))
            {
                this.weatherDataView.GraphMonthlyRainfall.DrawLineAndMarkers(
                                                 "Long term average Rainfall",
                                                 months,
                                                 avgMonthlyRain,
                                                 null,
                                                 null,
                                                 null,
                                                 null,
                                                 AxisPosition.Bottom,
                                                 AxisPosition.Left,
                                                 Color.Blue,
                                                 LineType.Solid,
                                                 MarkerType.None,
                                                 LineThickness.Normal,
                                                 MarkerSize.Normal,
                                                 1,
                                                 true);
            }

            double startDate = 0;
            double endDate = months.Length;
            double minVal = 0;
            double maxVal = MathUtilities.Max(avgMonthlyRain);

            this.weatherDataView.GraphMonthlyRainfall.FormatAxis(AxisPosition.Bottom, "Date", false, startDate, endDate, double.NaN, false, false);
            this.weatherDataView.GraphMonthlyRainfall.FormatAxis(AxisPosition.Left, "Rainfall (mm)", false, minVal, maxVal, double.NaN, false, false);
            this.weatherDataView.GraphMonthlyRainfall.FormatTitle(title);
            this.weatherDataView.GraphMonthlyRainfall.FormatLegend(LegendPosition.TopLeft, LegendOrientation.Vertical);
            this.weatherDataView.GraphMonthlyRainfall.Refresh();
        }

        /// <summary>Creates the Temperature Chart</summary>
        /// <param name="title">The title to display on the chart</param>
        /// <param name="dates">An array of Dates for the x Axis</param>
        /// <param name="maxTemps">An array of Max Temperatures amounts for the Y Axis</param>
        /// <param name="minTemps">An array of Minimum Temperatures amounts for the Y Axis</param>
        private void PopulateTemperatureGraph(string title, DateTime[] dates, double[] maxTemps, double[] minTemps)
        {
            this.weatherDataView.GraphTemperature.Clear();
            this.weatherDataView.GraphTemperature.DrawLineAndMarkers(
                                                     "Maximum Temperature",
                                                     dates,
                                                     maxTemps,
                                                     null,
                                                     null,
                                                     null,
                                                     null,
                                                     AxisPosition.Bottom,
                                                     AxisPosition.Left,
                                                     Color.Blue,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThickness.Normal,
                                                     MarkerSize.Normal,
                                                     1,
                                                     true);

            this.weatherDataView.GraphTemperature.DrawLineAndMarkers(
                                                     "Minimum Temperature",
                                                     dates,
                                                     minTemps,
                                                     null,
                                                     null,
                                                     null,
                                                     null,
                                                     AxisPosition.Bottom,
                                                     AxisPosition.Left,
                                                     Color.Orange,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThickness.Normal,
                                                     MarkerSize.Normal,
                                                     1,
                                                     true);

            double startDate = dates.Min<DateTime>().ToOADate();
            double endDate = dates.Max<DateTime>().ToOADate();
            double minVal = MathUtilities.Min(minTemps);
            double maxVal = MathUtilities.Max(maxTemps);

            this.weatherDataView.GraphTemperature.FormatAxis(AxisPosition.Bottom, "Date", false, startDate, endDate, double.NaN, false, false);
            this.weatherDataView.GraphTemperature.FormatAxis(AxisPosition.Left, "Temperature (oC)", false, minVal, maxVal, double.NaN, false, false);
            this.weatherDataView.GraphTemperature.FormatTitle(title);
            this.weatherDataView.GraphTemperature.FormatLegend(LegendPosition.TopLeft, LegendOrientation.Vertical);
            this.weatherDataView.GraphTemperature.Refresh();
        }

        /// <summary>Creates the Radiation Chart</summary>
        /// <param name="title">The title to display on the chart</param>
        /// <param name="dates">An array of Dates for the x Axis</param>
        /// <param name="rain">An array of Rainfall amounts for the Y Axis</param>
        /// <param name="radn">Radiation values</param>
        /// <param name="maxRadn">Max radiation values</param>
        private void PopulateRadiationGraph(string title, DateTime[] dates, double[] rain, double[] radn, double[] maxRadn)
        {
            this.weatherDataView.GraphRadiation.Clear();
            this.weatherDataView.GraphRadiation.DrawBar(
                                                       "Rainfall",
                                                       dates,
                                                       rain,
                                                       AxisPosition.Bottom,
                                                       AxisPosition.Left,
                                                       Color.LightSkyBlue,
                                                       true);
            this.weatherDataView.GraphRadiation.DrawLineAndMarkers(
                                                     "Radiation",
                                                     dates,
                                                     radn,
                                                     null,
                                                     null,
                                                     null,
                                                     null,
                                                     AxisPosition.Bottom,
                                                     AxisPosition.Right,
                                                     Color.Blue,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThickness.Normal,
                                                     MarkerSize.Normal,
                                                     1,
                                                     true);
            this.weatherDataView.GraphRadiation.DrawLineAndMarkers(
                                                     "Maximum Radiation",
                                                     dates,
                                                     maxRadn,
                                                     null,
                                                     null,
                                                     null,
                                                     null,
                                                     AxisPosition.Bottom,
                                                     AxisPosition.Right,
                                                     Color.Orange,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThickness.Normal,
                                                     MarkerSize.Normal,
                                                     1,
                                                     true);

            double startDate = dates.Min<DateTime>().ToOADate();
            double endDate = dates.Max<DateTime>().ToOADate();
            double minRad = MathUtilities.Min(radn);
            double maxRad = MathUtilities.Max(radn);
            double minRain = 0;
            double maxRain = MathUtilities.Max(rain);

            this.weatherDataView.GraphRadiation.FormatAxis(AxisPosition.Bottom, "Date", false, startDate, endDate, double.NaN, false, false);
            this.weatherDataView.GraphRadiation.FormatAxis(AxisPosition.Left, "Rainfall (mm)", false, minRain, maxRain, double.NaN, false, false);
            this.weatherDataView.GraphRadiation.FormatAxis(AxisPosition.Right, "Radiation (mJ/m2)", false, minRad, maxRad, double.NaN, false, false);
            this.weatherDataView.GraphRadiation.FormatTitle(title);
            this.weatherDataView.GraphRadiation.FormatLegend(LegendPosition.TopLeft, LegendOrientation.Vertical);
            this.weatherDataView.GraphRadiation.Refresh();
        }

        /// <summary>Populates the data.</summary>
        /// <param name="data">The data.</param>
        public void PopulateData(DataTable data)
        {
            //fill the grid with data
            DataTableProvider provider = new DataTableProvider(data);
            gridPresenter.PopulateWithDataProvider(provider);
        }

        public void Dispose()
        {
            if (graphMetData != null)
                graphMetData.Dispose();
        }
    }
}
