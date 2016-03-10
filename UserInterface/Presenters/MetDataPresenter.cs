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

        private DataTable graphMetData;

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
        public void GraphRefreshValueChanged(int startYear, int showYears)
        {
            try
            {
                DataTable data = this.graphMetData;
                DateTime startDate = new DateTime(startYear, 1, 1);
                DateTime endDate = new DateTime(startYear, 12, 31);
                if (showYears > 1)
                    endDate = endDate.AddYears(showYears - 1);

                this.DisplayDetailedGraphs(data, startDate, endDate);
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
            if (filename != null)
            {
                try
                {
                    this.weatherData.FullFileName = PathUtilities.GetAbsolutePath(filename, this.explorerPresenter.ApsimXFile.FileName);
                    DataTable data = this.weatherData.GetAllData();
                    this.WriteTable(data);
                    this.WriteSummary(data);
                    this.SetGraphControlsDefaultValues();
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
                CalcQmax(data, weatherData.Latitude);

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
                DateTime firstDate = DataTableUtilities.GetDateFromRow(table.Rows[0]);
                DateTime lastDate = DataTableUtilities.GetDateFromRow(table.Rows[table.Rows.Count - 1]);
                if (firstDate.DayOfYear != 1)
                    firstDate = new DateTime(firstDate.Year + 1, 1, 1);
                if (lastDate.Day != 31 || lastDate.Month != 12)
                    lastDate = new DateTime(lastDate.Year - 1, 12, 31);

                double[] yearlyRainfall = MathUtilities.YearlyTotals(table, "Rain", firstDate, lastDate);
                double[] monthlyRainfall = MathUtilities.AverageMonthlyTotals(table, "rain", firstDate, lastDate);
                double[] monthlyMaxT = MathUtilities.AverageDailyTotalsForEachMonth(table, "maxt", firstDate, lastDate);
                double[] monthlyMinT = MathUtilities.AverageDailyTotalsForEachMonth(table, "mint", firstDate, lastDate);

                // long term average rainfall
                if (yearlyRainfall.Length != 0)
                {
                    double totalYearlyRainfall = MathUtilities.Sum(yearlyRainfall);
                    int numYears = lastDate.Year - firstDate.Year + 1;
                    double meanYearlyRainfall = totalYearlyRainfall / numYears;
                    double stddev = MathUtilities.StandardDeviation(yearlyRainfall);

                    summary.AppendLine(String.Format("For years : {0} - {1}", firstDate.Year, lastDate.Year));
                    summary.AppendLine("Long term average yearly rainfall : " + String.Format("{0,3:f2}mm", meanYearlyRainfall));
                    summary.AppendLine("Yearly rainfall std deviation     : " + String.Format("{0,3:f2}mm", stddev));

                    this.weatherDataView.Summarylabel = summary.ToString();
                    string title = String.Format("Long term average data for years : {0} - {1}", firstDate.Year, lastDate.Year);
                    this.PopulateSummaryGraph(title, 
                                        monthlyRainfall,
                                        monthlyMaxT,
                                        monthlyMinT);

                }
            }
        }

        /// <summary>
        /// Sets the default values for the Numeric Updown spin controls used for displaying graph data (Start Year and Years to Show).
        /// </summary>
        private void SetGraphControlsDefaultValues()
        {
            //Set the default values for these based on the data.
            weatherDataView.GraphStartYearMinValue = this.weatherData.StartDate.Year;
            weatherDataView.GraphStartYearMaxValue = this.weatherData.EndDate.Year;
            weatherDataView.GraphStartYear = this.weatherData.StartDate.Year;
            weatherDataView.GraphShowYears = 1;
            if (this.weatherData.EndDate.Year > this.weatherData.StartDate.Year)
            {
                weatherDataView.GraphShowYearsMaxValue = (this.weatherData.EndDate.Year - this.weatherData.StartDate.Year);
            }
            else
            {
                weatherDataView.GraphShowYearsMaxValue = 1;
            }
        }

        /// <summary>sets the date range for the graphs, and calls the graph display functions</summary>
        /// <param name="table"></param>
        private void DisplayDetailedGraphs(DataTable table)
        {
            if (table != null && table.Rows.Count > 0)
            {
                DateTime firstDate = DataTableUtilities.GetDateFromRow(table.Rows[0]);
                if (firstDate.DayOfYear != 1)
                    firstDate = new DateTime(firstDate.Year + 1, 1, 1);

                //By default, only do one year (the first year)
                DateTime lastDate = new DateTime(firstDate.Year, 12, 31);
                DisplayDetailedGraphs(table, firstDate, lastDate);
            }
        }


        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table"></param>
        /// <param name="firstDate"></param>
        /// <param name="lastDate"></param>
        private void DisplayDetailedGraphs(DataTable table, DateTime firstDate, DateTime lastDate)
        { 

            // Make sure the data in the table consists of full years of data i.e.
            // exclude the first and/or last year of data if they aren't complete.
            if (table != null && table.Rows.Count > 0)
            {
                // Need to be able to filter the table based on first date and last date, so that we can graph
                // graph the daily values for rainfall, temperature and radiation
                DateTime[] dailyDates = GetArrayofDates(firstDate, lastDate);
                double[] dailyRain = GetColumnAsDoubles(table, "rain", firstDate, lastDate);
                double[] dailyMaxTemp = GetColumnAsDoubles(table, "maxt", firstDate, lastDate);
                double[] dailyMinTemp = GetColumnAsDoubles(table, "mint", firstDate, lastDate);
                double[] dailyRadn = GetColumnAsDoubles(table, "radn", firstDate, lastDate);
                double[] dailyMaxRadn = GetColumnAsDoubles(table, "Qmax", firstDate, lastDate);
                double[] monthlyRainfall = MathUtilities.AverageMonthlyTotals(table, "rain", firstDate, lastDate);

                String rainMessage = string.Empty ;
                if (dailyRain.Length != 0)
                {
                    double totalYearlyRainfall = Math.Round(MathUtilities.Sum(dailyRain), 1);
                    rainMessage = "Total Rainfall for the year " + firstDate.Year.ToString() 
                                + " is " + totalYearlyRainfall.ToString() + "mm.";
                }

                //this requires a different date range as we want the monthly average rainfall for all years
                DateTime fDate = DataTableUtilities.GetDateFromRow(table.Rows[0]);
                DateTime lDate = DataTableUtilities.GetDateFromRow(table.Rows[table.Rows.Count - 1]);
                if (fDate.DayOfYear != 1)
                    fDate = new DateTime(fDate.Year + 1, 1, 1);
                if (lDate.Day != 31 || lDate.Month != 12)
                    lDate = new DateTime(lDate.Year - 1, 12, 31);

                double[] avgMonthlyRainfall = MathUtilities.AverageMonthlyTotals(table, "rain", fDate, lDate);


                this.PopulateRainfallGraph(rainMessage, dailyDates, dailyRain);
                this.PopulateMonthlyRainfallGraph("Monthly Rainfall", monthlyRainfall, avgMonthlyRainfall);
                this.PopulateTemperatureGraph("Temperature", dailyDates, dailyMaxTemp, dailyMinTemp);
                this.PopulateRadiationGraph("Radiation", dailyDates, dailyRain, dailyRadn, dailyMaxRadn);

            }
        }


        /// <summary>Create the monthly Summary chart</summary>
        /// <param name="monthlyRain">Monthly rainfall</param>
        /// <param name="monthlyMaxT">Monthly Maximum Temperatures</param>
        /// <param name="monthlyMinT">Monthly Minimum Temperatures</param>
        /// <param name="title">The title.</param>
        private void PopulateSummaryGraph(string title, double[] monthlyRain, double[] monthlyMaxT, double[] monthlyMinT)
        {
            this.weatherDataView.GraphSummary.Clear();
            this.weatherDataView.GraphSummary.DrawBar("",
                                      DateUtilities.LowerCaseMonths,
                                      monthlyRain,
                                      Axis.AxisType.Bottom,
                                      Axis.AxisType.Left,
                                      Color.LightSkyBlue,
                                      true);
            this.weatherDataView.GraphSummary.DrawLineAndMarkers("Maximum Temperature",
                                                     DateUtilities.LowerCaseMonths,
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
                                                     DateUtilities.LowerCaseMonths,
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
        private void PopulateMonthlyRainfallGraph(string title, double[] monthlyRain, double[] avgMonthlyRain)
        {
            this.weatherDataView.GraphMonthlyRainfall.Clear();
            this.weatherDataView.GraphMonthlyRainfall.DrawBar(title,
                                                       DateUtilities.LowerCaseMonths,
                                                       monthlyRain,
                                                       Axis.AxisType.Bottom,
                                                       Axis.AxisType.Left,
                                                       Color.LightSkyBlue,
                                                       true);

            this.weatherDataView.GraphMonthlyRainfall.DrawLineAndMarkers("Long term average Rainfall",
                                                     DateUtilities.LowerCaseMonths,
                                                     avgMonthlyRain,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Left,
                                                     Color.Blue,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);

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



        //-----------------------------------------------------------------------------------------------------------------------------
        //To be added to DateUtilities 
        //-----------------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a Datetime array of dates for a specific date range</summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns>a DateTime array</returns>
        private DateTime[] GetArrayofDates(DateTime startDate, DateTime endDate)
        {
            List<DateTime> rValues = new List<DateTime>();
            for (DateTime dt = startDate; dt <= endDate; dt = dt.AddDays(1))
            {
                rValues.Add(dt);
            }
            return rValues.ToArray();
        }


        //-----------------------------------------------------------------------------------------------------------------------------
        //To be added to MetUtilities 
        //-----------------------------------------------------------------------------------------------------------------------------

        /// <summary>Calculates the MaxRadiation on all rows in datatable, based on latitude</summary>
        /// <param name="table"></param>
        /// <param name="latitude"></param>
        private void CalcQmax(DataTable table, double latitude)
        {
            if (!double.IsNaN(latitude))
            {
                // ----------------------------------------------------------------------------------
                // Add a calculated QMax column to the daily data.
                // ----------------------------------------------------------------------------------
                if (((table.Columns["Qmax"] == null)))
                {
                    table.Columns.Add("Qmax", Type.GetType("System.Single"));
                }
                // Do we have a VP column?
                bool haveVPColumn = (table.Columns["VP"] != null);

                //do we have a "doy" or "day" column, and which column is it in
                bool haveDOYColumn = true;
                int dayCol = table.Columns.IndexOf("day");

                // Loop through all rows and calculate a QMax
                DateTime cDate;
                DataRow row;
                int doy = 0;
                for (int r = 0; r <= table.Rows.Count - 1; r++)
                {
                    if (haveDOYColumn == true)
                    {
                        doy = Convert.ToInt16(table.Rows[r][dayCol]);
                    }
                    else {
                        row = table.Rows[r];
                        cDate = DataTableUtilities.GetDateFromRow(row);
                        doy = cDate.DayOfYear;
                    }

                    if (haveVPColumn && !Convert.IsDBNull(table.Rows[r]["vp"]))
                    {
                        table.Rows[r]["Qmax"] = MetUtilities.QMax(doy + 1, latitude, MetUtilities.Taz, MetUtilities.Alpha,
                            Convert.ToSingle(table.Rows[r]["vp"]));
                    }
                    else
                    {
                        table.Rows[r]["Qmax"] = MetUtilities.QMax(doy + 1, latitude, MetUtilities.Taz, MetUtilities.Alpha,
                            MetUtilities.svp(Convert.ToSingle(table.Rows[r]["mint"])));
                    }
                }
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------
        //To be added to DataTableUtilities
        //-----------------------------------------------------------------------------------------------------------------------------
            
        /// <summary>Get columns as doubles within specific data range</summary>
        /// <param name="dTable"></param>
        /// <param name="colName"></param>
        /// <param name="firstDate"></param>
        /// <param name="lastDate"></param>
        /// <returns></returns>
        private double[] GetColumnAsDoubles(System.Data.DataTable dTable, string colName, DateTime firstDate, DateTime lastDate)
        {
            var result = from row in dTable.AsEnumerable()
                         where (DataTableUtilities.GetDateFromRow(row) >= firstDate &&
                                DataTableUtilities.GetDateFromRow(row) <= lastDate)
                         select new
                         {
                             val = row.Field<float>(colName)
                         };

            List<double> rValues = new List<double>();
            foreach (var row in result)
                rValues.Add(row.val);

            return rValues.ToArray();
        }

    }
}
