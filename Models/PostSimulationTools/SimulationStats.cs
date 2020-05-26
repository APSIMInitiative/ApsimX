namespace Models.PostSimulationTools
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Run;
    using Storage;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    /// <summary>
    /// # [Name]
    /// A post processing model that produces simulation stats.
    /// </summary>
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType=typeof(DataStore))]
    [Serializable]
    public class SimulationStats : Model, IPostSimulationTool
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

            DataTable simulationData = dataStore.Reader.GetData(this.TableName);
            if (simulationData != null)
            {
                // Add required columns.
                var columnNames = new List<string>();
                foreach (DataColumn column in simulationData.Columns)
                {
                    if (column.DataType == typeof(double))
                    {
                        statsData.Columns.Add($"{ column.ColumnName}Count", typeof(int));
                        statsData.Columns.Add($"{ column.ColumnName}Total", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}Mean", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}Min", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}Max", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}StdDev", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}Percentile10", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}Percentile20", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}Percentile30", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}Percentile40", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}Percentile50", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}Percentile60", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}Percentile70", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}Percentile80", typeof(double));
                        statsData.Columns.Add($"{ column.ColumnName}Percentile90", typeof(double));

                        columnNames.Add(column.ColumnName);
                    }
                }

                var simulationNames = DataTableUtilities.GetColumnAsStrings(simulationData, "SimulationName").Distinct();

                DataView view = new DataView(simulationData);
                foreach (var simulationName in simulationNames)
                {
                    view.RowFilter = $"[SimulationName]='{simulationName}'";

                    var newRow = statsData.NewRow();
                    foreach (var columnName in columnNames)
                    {
                        var values = DataTableUtilities.GetColumnAsDoubles(view, columnName);
                        newRow["SimulationName"] = simulationName;
                        newRow[$"{ columnName}Count"] = values.Length;
                        newRow[$"{ columnName}Total"] = MathUtilities.Sum(values);
                        newRow[$"{ columnName}Mean"] = MathUtilities.Average(values);
                        newRow[$"{ columnName}Min"] = MathUtilities.Min(values);
                        newRow[$"{ columnName}Max"] = MathUtilities.Max(values);
                        newRow[$"{ columnName}StdDev"] = MathUtilities.SampleStandardDeviation(values);

                        newRow[$"{ columnName}Percentile10"] = MathUtilities.Percentile(values, 10);
                        newRow[$"{ columnName}Percentile20"] = MathUtilities.Percentile(values, 20);
                        newRow[$"{ columnName}Percentile30"] = MathUtilities.Percentile(values, 30);
                        newRow[$"{ columnName}Percentile40"] = MathUtilities.Percentile(values, 40);
                        newRow[$"{ columnName}Percentile50"] = MathUtilities.Percentile(values, 50);
                        newRow[$"{ columnName}Percentile60"] = MathUtilities.Percentile(values, 60);
                        newRow[$"{ columnName}Percentile70"] = MathUtilities.Percentile(values, 70);
                        newRow[$"{ columnName}Percentile80"] = MathUtilities.Percentile(values, 80);
                        newRow[$"{ columnName}Percentile90"] = MathUtilities.Percentile(values, 90);
                    }
                    statsData.Rows.Add(newRow);
                }

                // Write the stats data to the DataStore
                statsData.TableName = this.Name;
                dataStore.Writer.WriteTable(statsData);
            }
        }
    }
}
