using System;
using System.Collections.Generic;
using System.Data;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models.PostSimulationTools
{

    /// <summary>
    /// A post processing model that produces time series stats.
    /// </summary>
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    [ValidParent(typeof(ParallelPostSimulationTool))]
    [ValidParent(ParentType = typeof(SerialPostSimulationTool))]
    [Serializable]
    public class TimeSeriesStats : Model, IPostSimulationTool
    {
        [Link]
        private IDataStore dataStore = null;

        /// <summary>
        /// Gets or sets the name of the predicted/observed table name.
        /// </summary>
        [Description("Predicted/observed table name")]
        [Display(Type = DisplayType.TableName)]
        public string TableName { get; set; }

        /// <summary>Main run method for performing our calculations and storing data.</summary>
        public void Run()
        {
            DataTable statsData = new DataTable();
            statsData.Columns.Add("SimulationName", typeof(string));
            statsData.Columns.Add("VariableName", typeof(string));
            statsData.Columns.Add("n", typeof(string));
            statsData.Columns.Add("residual", typeof(double));
            statsData.Columns.Add("R^2", typeof(double));
            statsData.Columns.Add("RMSD", typeof(double));
            statsData.Columns.Add("%", typeof(double));
            statsData.Columns.Add("MSD", typeof(double));
            statsData.Columns.Add("SB", typeof(double));
            statsData.Columns.Add("SDSD", typeof(double));
            statsData.Columns.Add("LCS", typeof(double));

            DataTable simulationData = dataStore.Reader.GetData(this.TableName);
            if (simulationData != null)
            {
                DataView view = new DataView(simulationData);
                string[] columnNames = DataTableUtilities.GetColumnNames(simulationData);

                foreach (string observedColumnName in columnNames)
                {
                    if (observedColumnName.StartsWith("Observed."))
                    {
                        string predictedColumnName = observedColumnName.Replace("Observed.", "Predicted.");
                        if (simulationData.Columns.Contains(predictedColumnName))
                        {
                            DataColumn predictedColumn = simulationData.Columns[predictedColumnName];
                            DataColumn observedColumn = simulationData.Columns[observedColumnName];
                            if (predictedColumn.DataType == typeof(double) &&
                                observedColumn.DataType == typeof(double))
                            {
                                // Calculate stats for each simulation and store them in a rows in our stats table.
                                var simulationNames = dataStore.Reader.SimulationNames;
                                foreach (string simulationName in simulationNames)
                                {
                                    string seriesName = simulationName;
                                    view.RowFilter = "SimulationName = '" + simulationName + "'";
                                    CalcStatsRow(view, observedColumnName, predictedColumnName, seriesName, statsData);
                                }

                                // Calculate stats for all simulations and store in a row of the stats table.
                                string overallSeriesName = "Combined " + observedColumnName.Replace("Observed.", "");
                                view.RowFilter = null;
                                CalcStatsRow(view, observedColumnName, predictedColumnName, overallSeriesName, statsData);
                            }
                        }
                    }
                }

                // Write the stats data to the DataStore
                statsData.TableName = this.Name;
                dataStore.Writer.WriteTable(statsData);
            }
        }

        /// <summary>
        /// Calculate stats on the 'view' passed in and add a DataRow to 'statsData'
        /// </summary>
        /// <param name="view">The data view to calculate stats on</param>
        /// <param name="observedColumnName">The observed column name to use</param>
        /// <param name="predictedColumnName">The predicted column name to use</param>
        /// <param name="seriesName">The name of the series</param>
        /// <param name="statsData">The stats data table to add rows to</param>
        private static void CalcStatsRow(DataView view, string observedColumnName, string predictedColumnName, string seriesName, DataTable statsData)
        {
            List<double> observedData = new List<double>();
            List<double> predictedData = new List<double>();

            for (int row = 0; row != view.Count; row++)
            {
                if (!Convert.IsDBNull(view[row][observedColumnName]) &&
                    !Convert.IsDBNull(view[row][predictedColumnName]))
                {
                    observedData.Add(Convert.ToDouble(view[row][observedColumnName],
                                                      System.Globalization.CultureInfo.InvariantCulture));
                    predictedData.Add(Convert.ToDouble(view[row][predictedColumnName],
                                                       System.Globalization.CultureInfo.InvariantCulture));
                }
            }

            if (observedData.Count > 0)
            {
                MathUtilities.Stats stats = MathUtilities.CalcTimeSeriesStats(
                                           observedData.ToArray(),
                                           predictedData.ToArray());

                // Put stats into our stats DataTable
                if (stats.Count > 0)
                {
                    DataRow newRow = statsData.NewRow();
                    newRow["SimulationName"] = seriesName;
                    newRow["VariableName"] = observedColumnName.Replace("Observed.", string.Empty);
                    if (!double.IsNaN(stats.Residual))
                    {
                        newRow["n"] = stats.Count;
                        newRow["residual"] = stats.Residual;
                        newRow["R^2"] = stats.R2;
                        newRow["RMSD"] = stats.RMSD;
                        newRow["%"] = stats.Percent;
                        newRow["MSD"] = stats.MSD;
                        newRow["SB"] = stats.SB;
                        newRow["SDSD"] = stats.SDSD;
                        newRow["LCS"] = stats.LCS;
                    }

                    statsData.Rows.Add(newRow);
                }
            }
        }
    }
}
