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
            this.WriteTableAndSummary(this.weatherData.FullFileName);
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.weatherDataView.BrowseClicked -= this.OnBrowse;
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
        /// Get data from the weather file and present it to the view as both a table and a summary
        /// </summary>
        /// <param name="filename">The filename.</param>
        private void WriteTableAndSummary(string filename)
        {
            // Clear any previous summary
            this.weatherDataView.Summarylabel = string.Empty;
            this.weatherDataView.Graph.Clear();
            this.weatherDataView.Graph.Refresh();
            if (filename != null)
            {
                try
                {
                    this.weatherData.FullFileName = PathUtilities.GetAbsolutePath(filename, this.explorerPresenter.ApsimXFile.FileName);
                    DataTable data = this.weatherData.GetAllData();
                    this.WriteTable(data);
                    this.WriteSummary(data);
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
                int yrCol = data.Columns.IndexOf("year");
                int dayCol = data.Columns.IndexOf("day");
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
                    data.Columns.RemoveAt(yrCol);       // remove unwanted columns
                    data.Columns.RemoveAt(--dayCol);
                }
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

            // Make sure the data in the table consists of full years of data i.e.
            // exclude the first and/or last year of data if they aren't complete.
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
                    this.PopulateGraph(monthlyRainfall,
                                       monthlyMaxT,
                                       monthlyMinT,
                                       title);

                }
            }
        }

        /// <summary>Create the monthly chart</summary>
        /// <param name="monthlyRain">The monthly rain.</param>
        /// <param name="title">The title.</param>
        private void PopulateGraph(double[] monthlyRain, double[] montlyMaxt, double[] monthlyMint, string title)
        {
            this.weatherDataView.Graph.Clear();
            this.weatherDataView.Graph.DrawBar("",
                                      DateUtilities.LowerCaseMonths,
                                      monthlyRain,
                                      Axis.AxisType.Bottom,
                                      Axis.AxisType.Left,
                                      Color.LightSkyBlue,
                                      true);
            this.weatherDataView.Graph.DrawLineAndMarkers("Maximum temperature",
                                                     DateUtilities.LowerCaseMonths,
                                                     montlyMaxt,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Right,
                                                     Color.Red,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     true);
            this.weatherDataView.Graph.DrawLineAndMarkers("Minimum temperature",
                                                     DateUtilities.LowerCaseMonths,
                                                     monthlyMint,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Right,
                                                     Color.Orange,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     true);
            this.weatherDataView.Graph.FormatAxis(Axis.AxisType.Bottom, "Month", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.Graph.FormatAxis(Axis.AxisType.Left, "Rainfall (mm)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.Graph.FormatAxis(Axis.AxisType.Right, "Temperature (oC)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.Graph.FormatTitle(title);
            this.weatherDataView.Graph.Refresh();
        }
    }
}
