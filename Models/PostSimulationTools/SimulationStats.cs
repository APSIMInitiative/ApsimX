using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models.PostSimulationTools
{

    /// <summary>
    /// A post processing model that produces simulation stats.
    /// </summary>
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    [ValidParent(typeof(ParallelPostSimulationTool))]
    [ValidParent(ParentType = typeof(SerialPostSimulationTool))]
    [Serializable]
    public class SimulationStats : Model, IPostSimulationTool
    {
        [Link]
        private IDataStore dataStore = null;

        /// <summary>Source table name.</summary>
        [Description("Source table name")]
        [Display(Type = DisplayType.TableName)]
        public string TableName { get; set; }

        /// <summary>The fields to split on.</summary>
        [Description("Fields to split on (csv)")]
        [Tooltip("Values must be separated by commas")]
        public string[] FieldNamesToSplitOn { get; set; } = new string[] { "SimulationName" };

        /// <summary>.</summary>
        [Description("Count")]
        [Display]
        public bool CalcCount { get; set; } = true;

        /// <summary>.</summary>
        [Description("Total")]
        [Display]
        public bool CalcTotal { get; set; } = true;

        /// <summary>.</summary>
        [Description("Mean")]
        [Display]
        public bool CalcMean { get; set; } = true;

        /// <summary>.</summary>
        [Description("Median")]
        [Display]
        public bool CalcMedian { get; set; } = true;

        /// <summary>.</summary>
        [Description("Min")]
        [Display]
        public bool CalcMin { get; set; } = true;

        /// <summary>.</summary>
        [Description("Max")]
        [Display]
        public bool CalcMax { get; set; } = true;

        /// <summary>.</summary>
        [Description("StdDev")]
        [Display]
        public bool CalcStdDev { get; set; } = true;

        /// <summary>.</summary>
        [Description("Percentiles")]
        [Display]
        public bool CalcPercentiles { get; set; } = true;

        /// <summary>Main run method for performing our calculations and storing data.</summary>
        public void Run()
        {
            DataTable statsData = new DataTable();

            DataTable simulationData = dataStore.Reader.GetData(this.TableName);
            if (simulationData != null)
            {
                if (FieldNamesToSplitOn != null && FieldNamesToSplitOn.Length != 0)
                {
                    // Add the split fields to the stats table.
                    foreach (var splitFieldName in FieldNamesToSplitOn)
                    {
                        if (!simulationData.Columns.Contains(splitFieldName))
                            throw new Exception($"Cannot find field {splitFieldName} in table {simulationData.TableName}");
                        var fieldType = simulationData.Columns[splitFieldName].DataType;
                        statsData.Columns.Add(splitFieldName, fieldType);
                    }
                }

                // Add required columns.
                var columnNames = new List<string>();
                foreach (DataColumn column in simulationData.Columns)
                {
                    if (column.DataType == typeof(double))
                    {
                        if (CalcCount)
                            statsData.Columns.Add($"{column.ColumnName}Count", typeof(int));
                        if (CalcTotal)
                            statsData.Columns.Add($"{column.ColumnName}Total", typeof(double));
                        if (CalcMean)
                            statsData.Columns.Add($"{column.ColumnName}Mean", typeof(double));
                        if (CalcMin)
                            statsData.Columns.Add($"{column.ColumnName}Min", typeof(double));
                        if (CalcMax)
                            statsData.Columns.Add($"{column.ColumnName}Max", typeof(double));
                        if (CalcStdDev)
                            statsData.Columns.Add($"{column.ColumnName}StdDev", typeof(double));
                        if (CalcMedian)
                            statsData.Columns.Add($"{column.ColumnName}Median", typeof(double));
                        if (CalcPercentiles)
                        {
                            statsData.Columns.Add($"{column.ColumnName}Percentile10", typeof(double));
                            statsData.Columns.Add($"{column.ColumnName}Percentile20", typeof(double));
                            statsData.Columns.Add($"{column.ColumnName}Percentile30", typeof(double));
                            statsData.Columns.Add($"{column.ColumnName}Percentile40", typeof(double));
                            statsData.Columns.Add($"{column.ColumnName}Percentile60", typeof(double));
                            statsData.Columns.Add($"{column.ColumnName}Percentile70", typeof(double));
                            statsData.Columns.Add($"{column.ColumnName}Percentile80", typeof(double));
                            statsData.Columns.Add($"{column.ColumnName}Percentile90", typeof(double));
                        }
                        columnNames.Add(column.ColumnName);
                    }
                }

                DataView view = new DataView(simulationData);
                if (FieldNamesToSplitOn != null && FieldNamesToSplitOn.Length != 0)
                {
                    var rowFilters = GetRowFilters(simulationData);

                    foreach (var rowFilter in rowFilters)
                    {
                        view.RowFilter = rowFilter;
                        if (view.Count > 0)
                        {
                            var newRow = statsData.NewRow();

                            // add in split fields.
                            foreach (var splitFieldName in FieldNamesToSplitOn)
                                newRow[splitFieldName] = view[0][splitFieldName];

                            // add in stats.
                            CalculateStatsForDataView(newRow, columnNames, view);

                            // add row to stats data table.
                            statsData.Rows.Add(newRow);
                        }
                    }
                }
                else
                {
                    // one row for the entire dataset.
                    var newRow = statsData.NewRow();
                    CalculateStatsForDataView(newRow, columnNames, view);
                    statsData.Rows.Add(newRow);
                }

                // Write the stats data to the DataStore
                statsData.TableName = this.Name;
                dataStore.Writer.WriteTable(statsData);
            }
        }

        /// <summary>
        /// Get a list of row filters that define the blocks of data that we have 
        /// to calculate stats for.
        /// </summary>
        /// <returns></returns>
        private List<string> GetRowFilters(DataTable data)
        {
            var rowFilters = new List<string>();

            List<List<string>> fieldValues = new List<List<string>>();
            foreach (var fieldName in FieldNamesToSplitOn)
            {
                if (!data.Columns.Contains(fieldName))
                    throw new Exception($"Cannot find field {fieldName} in table {data.TableName}");
                fieldValues.Add(DataTableUtilities.GetColumnAsStrings(data, fieldName, CultureInfo.InvariantCulture).Distinct().ToList());
            }

            var permutations = MathUtilities.AllCombinationsOf<string>(fieldValues.ToArray());
            foreach (var permutation in permutations)
                rowFilters.Add(CreateRowFilter(permutation, data));

            return rowFilters;
        }

        /// <summary>
        /// Create a row filter for the specified set of values for the split fields
        /// </summary>
        /// <param name="permutation"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        private string CreateRowFilter(List<string> permutation, DataTable table)
        {
            string rowFilter = null;

            for (int i = 0; i < FieldNamesToSplitOn.Length; i++)
            {
                if (i > 0)
                    rowFilter += " and ";
                var fieldType = table.Columns[FieldNamesToSplitOn[i]].DataType;
                if (fieldType == typeof(string))
                    rowFilter += $"[{FieldNamesToSplitOn[i]}] ='{permutation[i]}'";
                else if (fieldType == typeof(DateTime))
                    rowFilter += $"[{FieldNamesToSplitOn[i]}] = #{permutation[i]}#";
                else
                    rowFilter += $"[{FieldNamesToSplitOn[i]}] = {permutation[i]}";
            }
            return rowFilter;
        }

        /// <summary>
        /// Calculate stats for a given data view and store them in a data row.
        /// </summary>
        /// <param name="row">The row to store the stats in.</param>
        /// <param name="columnNames">The column names to calculate stats for.</param>
        /// <param name="view">The data view to use.</param>
        private void CalculateStatsForDataView(DataRow row, List<string> columnNames, DataView view)
        {
            foreach (var columnName in columnNames)
            {
                var values = DataTableUtilities.GetColumnAsDoubles(view, columnName);
                if (CalcCount)
                    row[$"{columnName}Count"] = values.Length;

                if (CalcTotal)
                    row[$"{columnName}Total"] = MathUtilities.Sum(values);
                if (CalcMean)
                    row[$"{columnName}Mean"] = MathUtilities.Average(values);
                if (CalcMin)
                    row[$"{columnName}Min"] = MathUtilities.Min(values);
                if (CalcMax)
                    row[$"{columnName}Max"] = MathUtilities.Max(values);
                if (CalcStdDev)
                    row[$"{columnName}StdDev"] = MathUtilities.SampleStandardDeviation(values);
                if (CalcMedian)
                    row[$"{columnName}Median"] = MathUtilities.Percentile(values, 0.5);
                if (CalcPercentiles)
                {
                    row[$"{columnName}Percentile10"] = MathUtilities.Percentile(values, 0.1);
                    row[$"{columnName}Percentile20"] = MathUtilities.Percentile(values, 0.2);
                    row[$"{columnName}Percentile30"] = MathUtilities.Percentile(values, 0.3);
                    row[$"{columnName}Percentile40"] = MathUtilities.Percentile(values, 0.4);
                    row[$"{columnName}Percentile60"] = MathUtilities.Percentile(values, 0.6);
                    row[$"{columnName}Percentile70"] = MathUtilities.Percentile(values, 0.7);
                    row[$"{columnName}Percentile80"] = MathUtilities.Percentile(values, 0.8);
                    row[$"{columnName}Percentile90"] = MathUtilities.Percentile(values, 0.9);
                }
            }
        }
    }
}