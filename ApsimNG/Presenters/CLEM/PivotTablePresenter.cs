// -----------------------------------------------------------------------
// <copyright file="PivotTablePresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

using ApsimNG.Views.CLEM;
using Models.Core;
using Models.CLEM.Reporting;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UserInterface.Commands;
using UserInterface.Presenters;

namespace ApsimNG.Presenters.CLEM
{
    /// <summary>
    /// Connects the PivotTableView and the PivotTable model together
    /// </summary>
    class PivotTablePresenter : IPresenter
    {
        /// <summary>
        /// The PivotTable object
        /// </summary>
        private PivotTable table = null;

        /// <summary>
        /// The PivotTableView used
        /// </summary>
        private PivotTableView view = null;

        /// <summary>
        /// The ExplorerPresenter
        /// </summary>
        private ExplorerPresenter explorer = null;

        /// <summary>
        /// Attach the model and view to the presenter
        /// </summary>
        /// <param name="model">The model to attach</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The presenter to attach to</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.table = model as PivotTable;
            this.view = view as PivotTableView;
            this.explorer = explorerPresenter;

            // Find ledgers to source data from
            this.view.SetLedgers(table);

            // Attach events to handlers
            this.view.UpdateData += OnUpdateData;
            this.view.StoreData += OnStoreData;
            this.view.ChangePivot += OnChangePivot;
            this.view.TrackChanges += OnTrackChanges;

            // Update the boxes based on the tracked changes
            this.view.Ledger.ID = table.Ledger;
            this.view.Expression.ID = table.Expression;
            this.view.Value.ID = table.Value;
            this.view.Row.ID = table.Row;
            this.view.Column.ID = table.Column;
            this.view.Pivot.ID = table.Pivot;
            this.view.Time.ID = table.ID;            

            // Update gridview data (initial loading of data)
            OnUpdateData(null, EventArgs.Empty);
        }

        /// <summary>
        /// Refreshes the data in the gridview when a change is made to one of the view options
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnUpdateData(object sender, EventArgs e)
        {
            // The process of initially setting up the view will trigger the event early.
            // This statement catches early triggers to prevent errors/needless computation
            if (view.Pivot.Text == null) return;

            // Look for the data source
            var store = Apsim.Find(table, typeof(IDataStore)) as IDataStore;
            DataTable input = store.Reader.GetData(view.Ledger.Text);

            // Don't try to update if data source isn't found            
            if (input == null) return;   

            // The timescale is the number of significant characters when grouping based on date,
            // i.e. you only need to look at 4 characters when determining what year it is
            int timescale = 0;
            if (view.Time.Text == "Daily") timescale = 10;
            else if (view.Time.Text == "Monthly") timescale = 7;
            else if (view.Time.Text == "Yearly") timescale = 4;

            // Find the data nad generate the table with it
            var columns = FindColumns(input, timescale);
            var rows = FindRows(input, columns, timescale);         
            GenerateTable(rows, columns);
        }

        /// <summary>
        /// Generate the list of column names from the table
        /// </summary>
        private IEnumerable<string> FindColumns(DataTable table, int timescale)
        {
            // Determine the column names from 
            var cols = table.AsEnumerable().Select(r => r.Field<object>(view.Column.Text).ToString()).Distinct();

            // Rescale the time over the columns if required
            if (view.Column.Text == "Clock.Today")
            {
                return cols.GroupBy(col => col.Substring(10 - timescale, timescale)).Select(group => group.Key);
            }
            else return cols;
        }
        
        /// <summary>
        /// Find a list of row data 
        /// </summary>
        private IEnumerable<string[]> FindRows(DataTable table, IEnumerable<string> cols, int timescale)
        {           
            // The index of the column which contains data to pivot into rows
            int index = table.Columns[view.Row.Text].Ordinal;         

            var rows = table.AsEnumerable()
                // Group the table data by the distinct elements in the column
                .GroupBy(row => row.ItemArray[index].ToString())
                // Aggregate the data in each new group of rows
                .Select(grouping => PivotRow(grouping, cols));

            // If time series data is being pivoted, further aggregate the rows based on the time scale 
            if (view.Row.Text == "Clock.Today")
            {
                // Group the data based on time interval
                return rows.GroupBy(row => row[0].Substring(10 - timescale, timescale))
                    // Compress each group into a single row with the key as the new identifier
                    .Select(group => CompressRows(group.ToList(), group.Key));
            }
            else return rows;
        }

        /// <summary>
        /// Pivot a collection of rows into a single row, where the new columns
        /// a based on the distinct elements of one column in the original set.
        /// </summary>
        /// <remarks>
        /// We need a row which contains a value for each column in the new table.
        /// So, select is used similarly to a 'foreach' to find all the available data
        /// for a column. Once all the data is found, it is aggregated into a single value.
        /// There may be no rows in the grouping which contain data for a column,
        /// in which case a 0 is produced. The returned row is identified based on the key
        /// of the original grouping of rows.
        /// </remarks>
        private string[] PivotRow(IGrouping<string, DataRow> grouping, IEnumerable<string> cols)
        {
            // For each column
            var values = cols.Select(c => grouping.ToList()
            // Select the rows where the column matches
            .Where(data => data[view.Column.Text].ToString().Contains(c))
            // Select the data from that row
            .Select(data => (double)data[view.Value.Text]))
            // Aggregate the data
            .Select(IEnum => AggregateDoubles(IEnum).ToString())
            // Format the object
            .ToList();

            // Add the names of the rows            
            values.Insert(0, grouping.Key);

            return values.ToArray();
        }

        /// <summary>
        /// Collapses a collection of complete rows into a single row, where the
        /// name is the unique identifier of the new row
        /// </summary>
        private string[] CompressRows(List<string[]> rows, string name)
        {
            string[] row = new string[rows[0].Length];

            row[0] = name;
            for (int i = 1; i < rows[0].Length; i++)
            {
                row[i] = AggregateDoubles(rows.Select(r => double.Parse(r[i]))).ToString();
            }

            return row;
        }
        
        /// <summary>
        /// Generates the table seen in the view using the given row and column data
        /// </summary>
        private void GenerateTable(IEnumerable<string[]> rows, IEnumerable<string> cols)
        {
            // Create the output table and add the columns to it
            DataTable output = new DataTable($"{view.Expression.Text}Of{view.Ledger.Text}Resource{view.Value.Text}");
            output.Columns.AddRange(cols.Select(col => new DataColumn(col, typeof(double))).ToArray());

            // Attach another column for the row names
            string name = view.Row.Text;
            output.Columns.Add(name, typeof(string)).SetOrdinal(0);

            // Attach the data to the view
            foreach (var array in rows)
            {
                DataRow row = output.NewRow();
                row.ItemArray = array;
                output.Rows.Add(row);
            }
            output.AcceptChanges();
            view.Grid.DataSource = output;
        }
                
        /// <summary>
        /// Takes a collection of doubles and aggregates them based on the selection in the interface
        /// </summary>
        /// <param name="values">The collection of values</param>
        private double AggregateDoubles(IEnumerable<double> values)
        {
            // If there are no values, default to 0
            if (values.Count() > 0)
            {
                switch (view.Expression.Text)
                {
                    case "Sum":
                        return values.Sum();

                    case "Average":
                        return values.Average();

                    case "Max":
                        return values.Max();

                    case "Min":
                        return values.Min();

                    default:
                        return 0;
                }
            }
            else return 0;
        }

        /// <summary>
        /// Stores the current gridview in the DataStore
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnStoreData(object sender, EventArgs e)
        {
            // Check that data exists
            DataTable data = view.Grid.DataSource;
            if (data == null) return;
            
            // Store the data
            var store = Apsim.Find(table, typeof(IDataStore)) as IDataStore;
            store.Writer.WriteTable(data);
        }

        /// <summary>
        /// Switches the current pivot focus
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        private void OnChangePivot(object sender, PivotTableView.ChangePivotArgs args)
        {
            // Don't need to change anything if there is no pivot
            if (view.Pivot.Text == "None") return;

            if (args.Increase)
            {
                if (table.ID < table.Pivots.Count() - 1) table.ID += 1;
                else table.ID = 0;
            }
            else
            {
                if (table.ID > 0) table.ID -= 1;
                else table.ID = table.Pivots.Count() - 1;
            }

            OnTrackChanges(sender, new PivotTableView.TrackChangesArgs("ID", table.ID));
        }

        /// <summary>
        /// Detach the model from the view
        /// </summary>
        public void Detach()
        {
            view.UpdateData -= OnUpdateData;
            view.StoreData -= OnStoreData;
            view.ChangePivot -= OnChangePivot;
            view.TrackChanges -= OnTrackChanges;

            view.Detach();
            view.Grid.Dispose();
        }

        /// <summary>
        /// Track changes made to the view
        /// </summary>
        private void OnTrackChanges(object sender, PivotTableView.TrackChangesArgs args)
        {
            // Change the information of the property in the model
            var p = table.GetType().GetProperty(args.Name);
            p.SetValue(table, args.Value);

            // Track this change in the command history 
            ChangeProperty command = new ChangeProperty(table, args.Name, args.Value);
            explorer.CommandHistory.Add(command);
        }

    }
}
