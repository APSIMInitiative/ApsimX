// -----------------------------------------------------------------------
// <copyright file="MetDataPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using Models.Graph;
    using Models.Core;
    using Views;
    using Models;
    using APSIM.Shared.Utilities;

    /// <summary>A presenter for displaying weather data</summary>
    class MetDataPresenter : IPresenter
    {
        /// <summary>The met data</summary>
        private Weather weatherData;

        /// <summary>The met data view</summary>
        private IMetDataView weatherDataView;

        //these are used to display the graphs, and refresh graphs as required
        /// <summary>Hold the data used by the graphs</summary>
        private DataTable graphMetData;

        /// <summary>Hold the first date in datatable, for use in the graphs</summary>
        private DateTime dataFirstDate;
        /// <summary>Hold the last date in datatable, for use in the graphs</summary>
        private DateTime dataLastDate;

        /// <summary>Hold an array of months for the graph,  by default, is set to will Jan yyyy to Dec yyyy, except where
        /// data being displays is not for full year</summary>
        private string[] monthsToDisplay = DateUtilities.LowerCaseMonths;

        /// <summary>The explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Attaches the specified model.</summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.explorerPresenter = explorerPresenter;
            this.weatherData = (model as Weather);
            this.weatherDataView = (view as IMetDataView);

            this.weatherDataView.BrowseClicked += this.OnBrowse;
            this.weatherDataView.GraphRefreshClicked += this.GraphRefreshValueChanged;

            this.WriteTableAndSummary(this.weatherData.FullFileName);

        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.weatherDataView.BrowseClicked -= this.OnBrowse;
            this.weatherDataView.GraphRefreshClicked -= this.GraphRefreshValueChanged;
        }

        /// <summary>Called when [browse].</summary>
        /// <param name="fileName">Name of the file.</param>
        public void OnBrowse(string fileName)
        {
            if (this.weatherData.FullFileName != PathUtilities.GetAbsolutePath(fileName, this.explorerPresenter.ApsimXFile.FileName))
            {
                this.WriteTableAndSummary(fileName);
            }
        }

        /// <summary>
        /// This is called when the Graph StartYear or the Graphing ShowYears Numeric updown controls are changed by the user.
        /// It refreshes the graphs accordingly.
        /// </summary>
        /// <param name="startYear"></param>
        /// <param name="showYears"></param>
        public void GraphRefreshValueChanged(int tabIndex, decimal startYear, decimal showYears)
        {
            try
            {
                DataTable data = this.graphMetData;
                DateTime startDate = new DateTime(Convert.ToInt16(startYear), 1, 1);
                DateTime endDate = new DateTime(Convert.ToInt16(startYear), 12, 31);
                if (showYears > 1)
                    endDate = endDate.AddYears(Convert.ToInt16(showYears) - 1);

                this.DisplayDetailedGraphs(data, tabIndex, startDate, endDate, false);
            }
            catch (Exception)
            {
                throw;
            }
        }


        /// <summary>
        /// Get data from the weather file and present it to the view as both a table and a summary
        /// </summary>
        /// <param name="filename">The filename.</param>
        private void WriteTableAndSummary(string filename)
        {
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
            if (filename != null)
            {
                try
                {
                    this.weatherData.FullFileName = PathUtilities.GetAbsolutePath(filename, this.explorerPresenter.ApsimXFile.FileName);
                    DataTable data = this.weatherData.GetAllData();
                    this.WriteTable(data);
                    this.WriteSummary(data);
                    this.DisplayDetailedGraphs(data);

                }
                catch (Exception err)
                {
                    string message = err.Message;
                    message += "\r\n" + err.StackTrace;
                    this.weatherDataView.Summarylabel = err.Message;
                    this.explorerPresenter.ShowMessage(message, DataStore.ErrorLevel.Error);
                }
            }
            this.weatherDataView.Filename = PathUtilities.GetRelativePath(filename, this.explorerPresenter.ApsimXFile.FileName);
        }

        /// <summary>Send the DataTable to the View</summary>
        private void WriteTable(DataTable data)
        {
            // format the data into useful columns
            if (data != null)
            {
                int siteIdx = data.Columns.IndexOf("site");
                if (siteIdx >= 0)
                    data.Columns.RemoveAt(siteIdx);

                //modLMC - 10/03/2016 - Add the Qmax (Max Radiation) column that we require for the graphs
                //This is done here so that we can use the "day" or "doy" column if it exists, as it will be quicker
                MetUtilities.CalcQmax(data, weatherData.Latitude);

                //modLMC - 10/03/2016 - Modified to use this new function, as some data has "doy" and not "day"
                int dayCol = data.Columns.IndexOf("day");
                int yrCol = data.Columns.IndexOf("year");

                if ((yrCol >= 0) && (dayCol >= 0))
                {
                    // add a new column for the date string
                    DataColumn dateCol = data.Columns.Add("Date", Type.GetType("System.String"));
                    dateCol.SetOrdinal(0);
                    yrCol++;    // moved along
                    dayCol++;

                    int yr, day;
                    // for each row in the grid
                    for (int r = 0; r < data.Rows.Count; r++)
                    {
                        yr = Convert.ToInt32(data.Rows[r][yrCol]);
                        day = Convert.ToInt32(data.Rows[r][dayCol]);
                        DateTime rowDate = new DateTime(yr, 1, 1);
                        rowDate = rowDate.AddDays(day - 1);                 // calc date
                        data.Rows[r][0] = rowDate.ToShortDateString();      // store in Date col
                    }

                    if (dayCol > yrCol)
                    {
                        data.Columns.RemoveAt(dayCol);
                        data.Columns.RemoveAt(yrCol);       // remove unwanted columns
                    }
                    else
                    {
                        data.Columns.RemoveAt(yrCol);       // remove unwanted columns
                        data.Columns.RemoveAt(dayCol);
                    }
                }

                this.graphMetData = data;
                this.weatherDataView.PopulateData(data);
            }
        }

        /// <summary>Format a summary string about the weather file</summary>
        private void WriteSummary(DataTable table)
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("File name: " + this.weatherData.FileName);
            summary.AppendLine("Latitude : " + this.weatherData.Latitude.ToString());
            summary.AppendLine("TAV      : " + String.Format("{0, 2:f2}", this.weatherData.Tav));
            summary.AppendLine("AMP      : " + String.Format("{0, 2:f2}", this.weatherData.Amp));
            summary.AppendLine("Start    : " + this.weatherData.StartDate.ToShortDateString());
            summary.AppendLine("End      : " + this.weatherData.EndDate.ToShortDateString());
            summary.AppendLine("");

            if (table != null && table.Rows.Count > 0)
            {
                dataFirstDate = DataTableUtilities.GetDateFromRow(table.Rows[0]);
                dataLastDate = DataTableUtilities.GetDateFromRow(table.Rows[table.Rows.Count - 1]);

                TimeSpan diff = dataLastDate - dataFirstDate;
                //modLMC - 16/03/2016 - don't change dates if data is within the same year
                if (diff.Days > 365)
                {
                    if (dataFirstDate.DayOfYear != 1)
                        dataFirstDate = new DateTime(dataFirstDate.Year + 1, 1, 1);
                }

                //modLMC - 16/03/2016 - don't change dates if data is within the same year
                if (dataFirstDate.Year != dataLastDate.Year)
                { 
                    if (dataLastDate.Day != 31 || dataLastDate.Month != 12)
                        dataLastDate = new DateTime(dataLastDate.Year - 1, 12, 31);
                }

                double[] yearlyRainfall = MathUtilities.YearlyTotals(table, "Rain", dataFirstDate, dataLastDate);
                double[] monthlyRainfall = MathUtilities.AverageMonthlyTotals(table, "rain", dataFirstDate, dataLastDate);
                double[] monthlyMaxT = MathUtilities.AverageDailyTotalsForEachMonth(table, "maxt", dataFirstDate, dataLastDate);
                double[] monthlyMinT = MathUtilities.AverageDailyTotalsForEachMonth(table, "mint", dataFirstDate, dataLastDate);

                //what do we do if the date range is less than 1 year.
                //modlmc - 15/03/2016 - modified to pass in the "Month" values, and they may/may not contain a full year.
                if (monthlyRainfall.Length <= 12)
                    monthsToDisplay = DataTableUtilities.GetDistinctMonthsasStrings(table, dataFirstDate, dataLastDate);

                // long term average rainfall
                if (yearlyRainfall.Length != 0)
                {
                    double totalYearlyRainfall = MathUtilities.Sum(yearlyRainfall);
                    int numYears = dataLastDate.Year - dataFirstDate.Year + 1;
                    double meanYearlyRainfall = totalYearlyRainfall / numYears;
                    double stddev = MathUtilities.StandardDeviation(yearlyRainfall);

                    summary.AppendLine(String.Format("For years : {0} - {1}", dataFirstDate.Year, dataLastDate.Year));
                    summary.AppendLine("Long term average yearly rainfall : " + String.Format("{0,3:f2}mm", meanYearlyRainfall));
                    summary.AppendLine("Yearly rainfall std deviation     : " + String.Format("{0,3:f2}mm", stddev));

                    string title = String.Format("Long term average data for years : {0} - {1}", dataFirstDate.Year, dataLastDate.Year);

                    //modlmc - 15/03/2016 - modified to pass in the "Month" values, and they may/may not contain a full year.
                    this.PopulateSummaryGraph(title,
                                        monthsToDisplay,
                                        monthlyRainfall,
                                        monthlyMaxT,
                                        monthlyMinT);

                }
                this.weatherDataView.Summarylabel = summary.ToString();
            }
        }

        /// <summary>sets the date range for the graphs, and calls the graph display functions</summary>
        /// <param name="table"></param>
        private void DisplayDetailedGraphs(DataTable table)
        {
            if (table != null && table.Rows.Count > 0)
            {
                //By default, only do one year (the first year)
                DateTime endDate = new DateTime(dataFirstDate.Year, 12, 31);
                //by default, assume if not passed in, then we are displaying first tab (tab 0)
                DisplayDetailedGraphs(table, 0, dataFirstDate, endDate, true);
            }
        }

        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void DisplayDetailedGraphs(DataTable table, int tabIndex, DateTime startDate, DateTime endDate, Boolean updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    switch (tabIndex)
                    {
                        case 2:     //Daily Rain
                            DisplayGraphDailyRain(table, startDate, endDate, true);
                            break;
                        case 3:     //Monthly Rain
                            DisplayGraphMonthlyRain(table, startDate, endDate, true);
                            break;
                        case 4:     //Temperature
                            DisplayGraphTemperature(table, startDate, endDate, true);
                            break;
                        case 5:     //Radiation
                            DisplayGraphRadiation(table, startDate, endDate, true);
                            break;
                    }

                    if (updateYears == true)
                        this.SetGraphControlsDefaultValues();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to display Detailed Graphs due to insufficient data: " + e.Message.ToString());
            }

        }

        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void DisplayGraphDailyRain(DataTable table, DateTime startDate, DateTime endDate, Boolean updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    // Need to be able to filter the table based on first date and last date, so that we can graph
                    // graph the daily values for rainfall, temperature and radiation
                    DateTime[] dailyDates = DataTableUtilities.GetColumnAsDates(table, "Date", startDate, endDate);
                    double[] dailyRain = DataTableUtilities.GetColumnAsDoubles(table, "rain", startDate, endDate);

                    String rainMessage = string.Empty ;
                    if (dailyRain.Length != 0)
                    {
                        double totalYearlyRainfall = Math.Round(MathUtilities.Sum(dailyRain), 1);
                        rainMessage = "Total Rainfall for the year " + startDate.Year.ToString()
                                    + " is " + totalYearlyRainfall.ToString() + "mm.";


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
        /// <param name="table"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void DisplayGraphMonthlyRain(DataTable table, DateTime startDate, DateTime endDate, Boolean updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    double[] monthlyRainfall = MathUtilities.AverageMonthlyTotals(table, "rain", startDate, endDate);

                    if (monthlyRainfall.Length != 0)
                    {
                        double[] avgMonthlyRainfall = MathUtilities.AverageMonthlyTotals(table, "rain", dataFirstDate, dataLastDate);
                        this.PopulateMonthlyRainfallGraph("Monthly Rainfall", 
                                                        monthsToDisplay, 
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
        /// <param name="table"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void DisplayGraphTemperature(DataTable table, DateTime startDate, DateTime endDate, Boolean updateYears)
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
        /// <param name="table"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        private void DisplayGraphRadiation(DataTable table, DateTime startDate, DateTime endDate, Boolean updateYears)
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
            //Options for new Min Year:  less than current, greater than current, and greater than max
            //if less, just set it first, then value, then max
            //else is greater than max, then do max first, then value set value to max, set min, then reset value
            //if greater than current but less than max, then set value first, then min, then max

            if (this.weatherData.StartDate.Year < weatherDataView.GraphStartYearMinValue)
            {
                weatherDataView.GraphStartYearMinValue = this.weatherData.StartDate.Year;
                weatherDataView.GraphStartYearValue = this.weatherData.StartDate.Year;
                weatherDataView.GraphStartYearMaxValue = this.weatherData.EndDate.Year;
            }
            else if (weatherDataView.GraphStartYearMinValue >= this.weatherData.EndDate.Year)
            {
                weatherDataView.GraphStartYearMaxValue = this.weatherData.EndDate.Year;
                weatherDataView.GraphStartYearValue = this.weatherData.EndDate.Year;
                weatherDataView.GraphStartYearMinValue = this.weatherData.StartDate.Year;
                weatherDataView.GraphStartYearValue = this.weatherData.StartDate.Year;
            }
            else  //we are between our original range
            {
                if (weatherDataView.GraphStartYearMinValue < this.weatherData.StartDate.Year)
                {
                    weatherDataView.GraphStartYearMinValue = this.weatherData.StartDate.Year;
                    weatherDataView.GraphStartYearValue = this.weatherData.StartDate.Year;
                }
                else
                {
                    weatherDataView.GraphStartYearValue = this.weatherData.StartDate.Year;
                    weatherDataView.GraphStartYearMinValue = this.weatherData.StartDate.Year;
                }
                weatherDataView.GraphStartYearMaxValue = this.weatherData.EndDate.Year;
            }
        }



        /// <summary>Create the monthly Summary chart</summary>
        /// <param name="monthlyRain">Monthly rainfall</param>
        /// <param name="monthlyMaxT">Monthly Maximum Temperatures</param>
        /// <param name="monthlyMinT">Monthly Minimum Temperatures</param>
        /// <param name="title">The title.</param>
        private void PopulateSummaryGraph(string title, string[] months, double[] monthlyRain, double[] monthlyMaxT, double[] monthlyMinT)
        {
            this.weatherDataView.GraphSummary.Clear();
            this.weatherDataView.GraphSummary.DrawBar("",
                                      months,
                                      monthlyRain,
                                      Axis.AxisType.Bottom,
                                      Axis.AxisType.Left,
                                      Color.LightSkyBlue,
                                      true);
            this.weatherDataView.GraphSummary.DrawLineAndMarkers("Maximum Temperature",
                                                     months,
                                                     monthlyMaxT,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Right,
                                                     Color.Red,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);
            this.weatherDataView.GraphSummary.DrawLineAndMarkers("Minimum Temperature",
                                                     months,
                                                     monthlyMinT,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Right,
                                                     Color.Orange,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);
            this.weatherDataView.GraphSummary.FormatAxis(Axis.AxisType.Bottom, "Month", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphSummary.FormatAxis(Axis.AxisType.Left, "Rainfall (mm)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphSummary.FormatAxis(Axis.AxisType.Right, "Temperature (oC)", false, double.NaN, double.NaN, double.NaN);
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
            this.weatherDataView.GraphRainfall.DrawBar(title,
                                                       dates,
                                                       rain,
                                                       Axis.AxisType.Bottom,
                                                       Axis.AxisType.Left,
                                                       Color.LightSkyBlue,
                                                       false);

            this.weatherDataView.GraphRainfall.FormatAxis(Axis.AxisType.Bottom, "Date", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphRainfall.FormatAxis(Axis.AxisType.Left, "Rainfall (mm)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphRainfall.FormatTitle(title);
            this.weatherDataView.GraphRainfall.Refresh();
        }

        /// <summary>
        /// Displays the Monthly rainfall chart, which shows the current years rain (by month), and the long term average monthly rainfall, 
        /// based on all data in metfile
        /// </summary>
        /// <param name="title"></param>
        /// <param name="monthlyRain"></param>
        /// <param name="avgMonthlyRain"></param>
        private void PopulateMonthlyRainfallGraph(string title, string[] months, double[] monthlyRain, double[] avgMonthlyRain)
        {
            this.weatherDataView.GraphMonthlyRainfall.Clear();
            if (months.Length == monthlyRain.Length)
            {
                this.weatherDataView.GraphMonthlyRainfall.DrawBar(title,
                                                           months,
                                                           monthlyRain,
                                                           Axis.AxisType.Bottom,
                                                           Axis.AxisType.Left,
                                                           Color.LightSkyBlue,
                                                           true);
            }

            if ((avgMonthlyRain.Length != 0) && (avgMonthlyRain.Length == monthlyRain.Length))
            {
                this.weatherDataView.GraphMonthlyRainfall.DrawLineAndMarkers("Long term average Rainfall",
                                                 months,
                                                 avgMonthlyRain,
                                                 Axis.AxisType.Bottom,
                                                 Axis.AxisType.Left,
                                                 Color.Blue,
                                                 LineType.Solid,
                                                 MarkerType.None,
                                                 LineThicknessType.Normal,
                                                 MarkerSizeType.Normal,
                                                 true);
            }

            this.weatherDataView.GraphMonthlyRainfall.FormatAxis(Axis.AxisType.Bottom, "Date", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphMonthlyRainfall.FormatAxis(Axis.AxisType.Left, "Rainfall (mm)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphMonthlyRainfall.FormatTitle(title);
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
            this.weatherDataView.GraphTemperature.DrawLineAndMarkers("Maximum Temperature",
                                                     dates,
                                                     maxTemps,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Left,
                                                     Color.Blue,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);

            this.weatherDataView.GraphTemperature.DrawLineAndMarkers("Minimum Temperature",
                                                     dates,
                                                     minTemps,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Left,
                                                     Color.Orange,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);

            this.weatherDataView.GraphTemperature.FormatAxis(Axis.AxisType.Bottom, "Date", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphTemperature.FormatAxis(Axis.AxisType.Left, "Temperature (oC)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphTemperature.FormatTitle(title);
            this.weatherDataView.GraphTemperature.Refresh();

        }

        /// <summary>Creates the Radiation Chart</summary>
        /// <param name="title">The title to display on the chart</param>
        /// <param name="dates">An array of Dates for the x Axis</param>
        /// <param name="rain">An array of Rainfall amounts for the Y Axis</param>
        private void PopulateRadiationGraph(string title, DateTime[] dates, double[] rain, double[] radn, double[] maxRadn)
        {
            this.weatherDataView.GraphRadiation.Clear();
            this.weatherDataView.GraphRadiation.DrawBar("Rainfall",
                                                       dates,
                                                       rain,
                                                       Axis.AxisType.Bottom,
                                                       Axis.AxisType.Left,
                                                       Color.LightSkyBlue,
                                                       true);
            this.weatherDataView.GraphRadiation.DrawLineAndMarkers("Radiation",
                                                     dates,
                                                     radn,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Right,
                                                     Color.Blue,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);
            this.weatherDataView.GraphRadiation.DrawLineAndMarkers("Maximum Radiation",
                                                     dates,
                                                     maxRadn,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Right,
                                                     Color.Orange,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);

            this.weatherDataView.GraphRadiation.FormatAxis(Axis.AxisType.Bottom, "Date", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphRadiation.FormatAxis(Axis.AxisType.Left, "Rainfall (mm)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphRadiation.FormatAxis(Axis.AxisType.Right, "Radiation (mJ/m2)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphRadiation.FormatTitle(title);
            this.weatherDataView.GraphRadiation.Refresh();
        }

    }
}
