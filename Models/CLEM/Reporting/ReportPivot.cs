using Models.Core;
using Models.Core.Attributes;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// Provides utility to quickly summarise data from a report
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.CLEMView")]
    [PresenterName("UserInterface.Presenters.ReportPivotPresenter")]
    [ValidParent(ParentType = typeof(Report))]
    [Description("Generates a pivot table from a Report")]
    [Version(1, 0, 0, "")]
    public class ReportPivot : Model, ICLEMUI
    {
        /// <summary>
        /// The report data
        /// </summary>
        private DataTable report = null;

        /// <summary>
        /// Tracks the active selection in the value box
        /// </summary>
        [Description("Values column")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetValueNames))]
        public string Value { get; set; }

        /// <summary>
        /// Populates the value filter
        /// </summary>
        public string[] GetValueNames() => GetColumnPivotOptions(true);

        /// <summary>
        /// Tracks the active selection in the row box
        /// </summary>
        [Description("Rows column")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetRowNames))]
        public string Row { get; set; }

        /// <summary>
        /// Populates the row filter
        /// </summary>
        public string[] GetRowNames() => GetColumnPivotOptions(false);

        /// <summary>
        /// Tracks the active selection in the column box
        /// </summary>
        [Description("Columns columns")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetColumnNames))]
        public string Column { get; set; }

        /// <summary>
        /// Populates the column filter
        /// </summary>
        public string[] GetColumnNames() => GetColumnPivotOptions(false);

        /// <summary>
        /// Tracks the active selection in the time box
        /// </summary>
        [Description("Time filter")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetTimes))]
        public string Time { get; set; }

        /// <summary>
        /// Populates the time filter
        /// </summary>
        public string[] GetTimes() => new string[] { "Day", "Month", "Year" };

        /// <summary>
        /// Tracks the active selection in the time box
        /// </summary>
        [Description("Aggregation method")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetAggregators))]
        public string Aggregator { get; set; }

        /// <summary>
        /// Populates the aggregate filter
        /// </summary>
        public string[] GetAggregators() => new string[] { "Sum", "Average", "Max", "Min", "Count" };

        /// <inheritdoc/>
        public string SelectedTab { get; set; }

        /// <summary>
        /// Searches the columns of the parent report for the pivot options
        /// </summary>
        /// <param name="value"><see langword="true"/> if we are searching for value options</param>
        public string[] GetColumnPivotOptions(bool value)
        {
            // Find the data from the parent report
            var storage = FindInScope("DataStore") as IDataStore;
            report = report ?? storage.Reader.GetData(Parent.Name);
            
            if (report is null)
            {
                return new string[] { "No available data" };
            }

            // Find the columns that meet our criteria
            var columns = report.Columns.Cast<DataColumn>();
            var result = columns.Where(c => !value ^ HasDataValues(c))
                .Select(c => c.ColumnName)
                .ToArray();

            return result;
        }        

        /// <summary>
        /// Generates the pivot table
        /// </summary>
        public DataTable GenerateTable()
        {
            // Find the DataStore
            var storage = FindInScope("DataStore") as IDataStore;

            // Find the data
            report = report ?? storage.Reader.GetData(Parent.Name);

            // Check sensibility
            if (report is null || Row is null || Column is null || Value is null || Aggregator is null)
            {
                return null;
            }

            var columns = FindPivotColumns(report);

            // Create the pivot table and populate it
            var pivot = new DataTable(Parent.Name + "_" + Name);
            pivot.Columns.Add(Row);
            pivot.Columns.AddRange(columns.ToArray());
            AddPivotRows(report, pivot);

            // Add the pivoted table to the datastore
            storage.Writer.WriteTable(pivot.DefaultView.ToTable());
            return pivot;
        }

        /// <summary>
        /// Test if a column contains data values
        /// </summary>
        /// <param name="col">The column being tested</param>
        /// <returns>
        /// <see langword="true"/> if the column contains data values,
        /// <see langword="false"/> otherwise 
        /// </returns>
        private bool HasDataValues(DataColumn col)
        {
            if (col.DataType.Name == "String")
            {
                return false;
            }

            // We are looking for data values, not IDs
            if (col.ColumnName.EndsWith("ID"))
            {
                return false;
            }

            // DateTime is handled separately from other value types
            return col.DataType != typeof(DateTime);
        }

        /// <summary>
        /// Use the source data to enerate the DataColumns for the pivot
        /// </summary>
        /// <param name="source">The source data</param>
        /// <returns>A collection of columns</returns>
        private IEnumerable<DataColumn> FindPivotColumns(DataTable source)
        {
            // Determine the column names from 
            var cols = source.AsEnumerable()
                .Select(r => r[Column])
                .Distinct();

            // Rescale the time over the columns if required
            var type = source.Columns[Column].DataType;

            // Group the data based on the time filter
            if (type == typeof(DateTime))
            {
                return cols.Cast<DateTime>()
                    .GroupBy(d => FormatDate(d))
                    .Select(g => g.Key)
                    .Select(s => new DataColumn(s));
            }

            return cols.Select(s => new DataColumn(s.ToString()));
        }

        /// <summary>
        /// Converts a date value based on the time filter
        /// </summary>
        /// <param name="date">The date to convert</param>
        /// <returns>A string representing the date in a new format</returns>
        private string FormatDate(DateTime date)
        {
            if (Time == "Day")
            {
                return date.ToString("dd/MM/yyyy");
            }
            else if (Time == "Month")
            {
                return date.ToString("MM/yyyy");
            }
            else if (Time == "Year")
            {
                return date.ToString("yyyy");
            }
            else
            {
                throw new Exception("");
            }
        }

        /// <summary>
        /// Populates the pivot table with data from the source,
        /// based on the selected pivot options
        /// </summary>
        /// <param name="source">The source data</param>
        /// <param name="pivot">The table to populate</param>
        private void AddPivotRows(DataTable source, DataTable pivot)
        {
            // Rescale the time over the rows if required
            var date = source.Columns[Row].DataType == typeof(DateTime);

            var names = source.AsEnumerable()
                    .Select(r => date ? FormatDate(Convert.ToDateTime(r[Row])) : r[Row].ToString())
                    .Distinct();

            foreach (var name in names)
            {
                var row = pivot.NewRow();
                row[0] = name;

                foreach (var col in pivot.Columns.Cast<DataColumn>().Skip(1))
                {
                    var values = source.AsEnumerable()
                        .Where(r => r[Row].ToString().Contains(name))
                        .Where(r => r[Column].ToString().Contains(col.ColumnName))
                        .Select(r => Convert.ToDouble(r[Value]));

                    // Aggregate the data, leaving blank cells for missing values
                    if (values.Any())
                    {
                        row[col] = AggregateValues(values);
                    }
                }

                pivot.Rows.Add(row);
            }
            
            pivot.AcceptChanges();
        }

        /// <summary>
        /// Aggregate a collection of doubles based on the selected aggregation method
        /// </summary>
        /// <param name="values">The values to aggregate</param>
        /// <returns>The aggregated values</returns>
        private double AggregateValues(IEnumerable<double> values)
        {
            switch (Aggregator)
            {
                case "Sum":
                    return values.Sum();

                case "Average":
                    return values.Average();

                case "Max":
                    return values.Max();

                case "Min":
                    return values.Min();

                case "Count":
                    return values.Count();

                default:
                    return 0;
            }
        }
    }
}