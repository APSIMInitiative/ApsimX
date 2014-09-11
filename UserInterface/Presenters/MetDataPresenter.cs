using System;
using System.Collections.Generic;
using System.Text;
using Models.Graph;
using Models.Core;
using UserInterface.Views;
using System.Data;
using Models;

// This presenter is for the WeatherFile component

namespace UserInterface.Presenters
{
    class MetDataPresenter : IPresenter
    {
        private WeatherFile MetData;
        private TabbedMetDataView MetDataView;

        private ExplorerPresenter ExplorerPresenter;
        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            ExplorerPresenter = explorerPresenter;
            MetData = (Model as WeatherFile);
            MetDataView = (View as TabbedMetDataView);
            
            WriteTable(MetData.FullFileName);
            WriteSummary();
            
            MetDataView.OnBrowseClicked += OnBrowse;
        }
        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            MetDataView.OnBrowseClicked -= OnBrowse;
        }
        public void OnBrowse(String FileName)
        {
            WriteTable(FileName);
            WriteSummary();
        }
        /// <summary>
        /// Get the DataTable from the WeatherFile and send it to the View
        /// </summary>
        /// <param name="filename"></param>
        private void WriteTable(String filename)
        {
            if (filename != null)
            {
                MetData.FullFileName = Utility.PathUtils.GetAbsolutePath(filename, ExplorerPresenter.ApsimXFile.FileName);
                DataTable data = MetData.GetAllData();

                //format the data into useful columns
                if (data != null)
                {
                    int siteIdx = data.Columns.IndexOf("site");
                    if (siteIdx >= 0)
                        data.Columns.RemoveAt(siteIdx);
                    int yrCol = data.Columns.IndexOf("year");
                    int dayCol = data.Columns.IndexOf("day");
                    if ((yrCol >= 0) && (dayCol >= 0))
                    {
                        //add a new column for the date string
                        DataColumn dateCol = data.Columns.Add("Date", Type.GetType("System.String"));
                        dateCol.SetOrdinal(0);
                        yrCol++;    //moved along
                        dayCol++;

                        int yr, day;
                        for (int r = 0; r < data.Rows.Count; r++)               //for each row in the grid
                        {
                            yr = Convert.ToInt32(data.Rows[r][yrCol]);
                            day = Convert.ToInt32(data.Rows[r][dayCol]);
                            DateTime rowDate = new DateTime(yr, 1, 1);
                            rowDate = rowDate.AddDays(day - 1);                 //calc date
                            data.Rows[r][0] = rowDate.ToShortDateString();      //store in Date col
                        }
                        data.Columns.RemoveAt(yrCol);       //remove unwanted columns
                        data.Columns.RemoveAt(--dayCol);
                    }
                    MetDataView.PopulateData(data);
                    MetDataView.Filename = Utility.PathUtils.GetRelativePath(filename, ExplorerPresenter.ApsimXFile.FileName);
                }
            }
        }
        /// <summary>
        /// Format a summary string about the weather file
        /// </summary>
        private void WriteSummary()
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("File name: " + MetData.FileName);
            summary.AppendLine("Latitude : " + MetData.Latitude.ToString());
            summary.AppendLine("TAV      : " + String.Format("{0, 2:f2}", MetData.Tav));
            summary.AppendLine("AMP      : " + String.Format("{0, 2:f2}", MetData.Amp));
            summary.AppendLine("Start    : " + MetData.StartDate.ToShortDateString());
            summary.AppendLine("End      : " + MetData.EndDate.ToShortDateString());
            summary.AppendLine("");

            //long term average rainfall
            double[] rainfall;
            double[] monthlyRain;
            MetData.YearlyRainfall(out rainfall, out monthlyRain);
            if (rainfall.Length != 0)
            {
                int startYr = MetData.StartDate.Year;
                int endYr = MetData.EndDate.Year;
                int count = rainfall.Length;
                List<double> yearly = new List<double>(rainfall);   //use a list as this is more flexible
                if (MetData.EndDate.DayOfYear < 365) //if the final year is truncated
                {
                    yearly.RemoveAt(count - 1);
                    count -= 1;
                    endYr -= 1;
                }
                if (MetData.StartDate.DayOfYear > 2) //if the start year is truncated
                {
                    if (yearly.Count > 0)
                    {
                        yearly.RemoveAt(0);
                        count -= 1;
                    }
                    startYr += 1;
                }
                if (count > 0)
                {
                    double total = 0;
                    for (int yr = 0; yr < count; yr++)
                    {
                        total += yearly[yr];
                    }
                    summary.AppendLine(String.Format("For years : {0} - {1}", startYr, endYr));
                    summary.AppendLine("Long term average yearly rainfall : " + String.Format("{0,3:f2}mm", total / count));
                    double stddev = getStandardDeviation(yearly, total / count);
                    summary.AppendLine("Yearly rainfall std deviation     : " + String.Format("{0,3:f2}mm", stddev));
                }

                MetDataView.Summarylabel = summary.ToString();
                PopulateGraph(monthlyRain, String.Format("Long term average monthly rainfall for years : {0} - {1}", MetData.StartDate.Year, MetData.EndDate.Year));
            }
        }
        /// <summary>
        /// Calculate the std deviation
        /// </summary>
        /// <param name="doubleList"></param>
        /// <param name="mean"></param>
        /// <returns>Std deviation</returns>
        private double getStandardDeviation(List<double> doubleList, double mean)
        {
            double sumOfDerivation = 0;
            foreach (double value in doubleList)
            {
                sumOfDerivation += (value) * (value);
            }
            double sumOfDerivationAverage = sumOfDerivation / doubleList.Count;
            return Math.Sqrt(sumOfDerivationAverage - (mean * mean)); 
        }
        /// <summary>
        /// Create the monthly lt av chart
        /// </summary>
        /// <param name="monthlyRain"></param>
        /// <param name="title"></param>
        private void PopulateGraph(double[] monthlyRain, String sTitle)
        {
            MetDataView.ChartTitle(sTitle);
            MetDataView.ClearSeries();
            MetDataView.CreateBarSeries(monthlyRain, "Monthly rain",
                                                     "Month", "mm",
                                                     OxyPlot.Axes.AxisPosition.Bottom, 
                                                     OxyPlot.Axes.AxisPosition.Left);
            MetDataView.RefreshGraph();
        }

    }
}
