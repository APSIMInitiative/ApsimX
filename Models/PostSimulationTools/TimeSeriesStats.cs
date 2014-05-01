using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.Data;
using System.IO;
using System.Xml.Serialization;

namespace Models.PostSimulationTools
{
    /// <summary>
    /// A simple post processing model that produces some time series stats.
    /// </summary>
    [Serializable]
    public class TimeSeriesStats : Model
    {
        public string TableName { get; set; }


        /// <summary>
        /// Go find the top level simulations object.
        /// </summary>
        public Simulations Simulations
        {
            get
            {
                Model obj = this;
                while (obj.Parent != null && obj.GetType() != typeof(Simulations))
                    obj = obj.Parent;
                if (obj == null)
                    throw new ApsimXException(FullPath, "Cannot find a root simulations object");
                return obj as Simulations;
            }
        }

        /// <summary>
        /// Simulation has completed. Create a regression table in the data store.
        /// </summary>
        public override void OnAllCompleted()
        {
            DataStore dataStore = new DataStore();
            dataStore.Connect(Path.ChangeExtension(Simulations.FileName, ".db"), readOnly: false);

            dataStore.DeleteTable(this.Name);

            DataTable statsData = new DataTable();
            statsData.Columns.Add("Series", typeof(string));
            statsData.Columns.Add("residual", typeof(double));
            statsData.Columns.Add("RMSD", typeof(double));
            statsData.Columns.Add("%", typeof(double));
            statsData.Columns.Add("SB", typeof(double));
            statsData.Columns.Add("SDSD", typeof(double));
            statsData.Columns.Add("LCS", typeof(double));

            DataTable simulationData = dataStore.GetData("*", TableName);
            DataView view = new DataView(simulationData);
            string[] columnNames = Utility.DataTable.GetColumnNames(simulationData);

            foreach (string observedColumnName in columnNames)
            {
                if (observedColumnName.StartsWith("Observed."))
                {
                    string predictedColumnName = observedColumnName.Replace("Observed.", "Predicted.");
                    if (simulationData.Columns.Contains(predictedColumnName))
                    {
                        DataColumn predictedColumn = simulationData.Columns[predictedColumnName];
                        DataColumn observedColumn = simulationData.Columns[observedColumnName];
                        if (predictedColumn.DataType == typeof(double[]) &&
                            observedColumn.DataType == typeof(double[]))
                        {
                            string[] simulationNames = dataStore.SimulationNames;
                            foreach (string simulationName in simulationNames)
                            {
                                string seriesName = observedColumnName.Replace("Observed.", "");
                                view.RowFilter = "SimulationName = '" + simulationName + "'";
                                CalcStatsRow(view, observedColumnName, predictedColumnName, seriesName, statsData);
                            }

                            // Calc stats for all data.
                            string overallSeriesName = "Overall." + observedColumnName.Replace("Observed.", "");
                            view.RowFilter = null;
                            CalcStatsRow(view, observedColumnName, predictedColumnName, overallSeriesName, statsData);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Calculate stats on the 'view' passed in and add a DataRow to 'statsData'
        /// </summary>
        private static void CalcStatsRow(DataView view, string observedColumnName, string predictedColumnName, string seriesName, DataTable statsData)
        {
            double[] observedData = Utility.DataTable.GetColumnAsDoubles(view, observedColumnName);
            double[] predictedData = Utility.DataTable.GetColumnAsDoubles(view, predictedColumnName);

            Utility.Math.Stats stats = Utility.Math.CalcTimeSeriesStats(observedData, predictedData);

            // Put stats into our stats DataTable
            DataRow newRow = statsData.NewRow();
            newRow["Series"] =
            newRow["residual"] = stats.Residual;
            newRow["RMSD"] = stats.RMSD;
            newRow["%"] = stats.Percent;
            newRow["SB"] = stats.ResidualSquared;
            newRow["SDSD"] = stats.SDSD;
            newRow["LCS"] = stats.LCS;
            statsData.Rows.Add(newRow);
        }
        
    }
}
