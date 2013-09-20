using System;
using System.Collections.Generic;
using System.Text;
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

        private CommandHistory CommandHistory;
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            this.CommandHistory = CommandHistory;
            MetData = (Model as WeatherFile);
            MetDataView = (View as TabbedMetDataView);
            
            WriteTable(MetData.FileName);
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
            MetData.FileName = filename;
            DataTable data = MetData.GetAllData();
            
            //format the data into useful columns
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
            MetDataView.Filename = filename;
        }
        private void WriteSummary()
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("File name: " + MetData.FileName);
            summary.AppendLine("Latitude : " + MetData.Latitude.ToString());
            summary.AppendLine("TAV      : " + String.Format("{0, 2:f2}", MetData.Tav));
            summary.AppendLine("AMP      : " + String.Format("{0, 2:f2}", MetData.Amp));
            summary.AppendLine("Start    : " + MetData.StartDate.ToShortDateString());
            summary.AppendLine("End      : " + MetData.EndDate.ToShortDateString());

            MetDataView.Summarylabel = summary.ToString();
        }
    }
}
