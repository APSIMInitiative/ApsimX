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
        private IPivotTableView view = null;

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

            // Update the boxes based on the tracked changes
            this.view.FilterLabel = table.Filter;
            this.view.LedgerViewBoxSelectedValue = table.LedgerViewBox;
            this.view.ExpressionViewBoxSelectedValue = table.ExpressionViewBox;
            this.view.ValueViewBoxSelectedValue = table.ValueViewBox;
            this.view.RowViewBoxSelectedValue = table.RowViewBox;
            this.view.ColumnViewBoxSelectedValue = table.ColumnViewBox;
            this.view.FilterViewBoxSelectedValue = table.FilterViewBox;
            this.view.TimeViewBoxSelectedValue = table.TimeViewBox;

            // Attach events to handlers
            this.view.UpdateData += OnUpdateData;
            this.view.StoreData += OnStoreData;
            this.view.TrackChanges += OnTrackChanges;

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
            if (view.Filter == null)
            {
                return;
            }

            // Look for the data source
            var store = Apsim.Find(table, typeof(IDataStore)) as IDataStore;
            DataTable input = store.Reader.GetData(view.LedgerName);

            // Don't try to update if data source isn't found            
            if (input == null)
            {
                return;
            }

            // Update filters if the filter button was pressed
            //if (sender is Gtk.Button btn) { UpdateFilter(input, btn.Name); }

            // The timescale is the number of significant characters when grouping based on date,
            // i.e. you only need to look at 4 characters when determining what year it is
            int timeScaleCharacters = 0;
            if (view.TimeScale == "Daily")
            {
                timeScaleCharacters = 10;
            }
            else if (view.TimeScale == "Monthly")
            {
                timeScaleCharacters = 7;
            }
            else if (view.TimeScale == "Yearly")
            {
                timeScaleCharacters = 4;
            }

            // Find the data and generate the table with it
            var columns = FindColumns(input, timeScaleCharacters);
            var rows = FindRows(input, columns, timeScaleCharacters);
            GenerateTable(rows, columns);
        }

        /// <summary>
        /// Changes the filter being applied to the data
        /// </summary>
        private void UpdateFilter(DataTable table, string name)
        {
            if (view.Filter == "None")
            {
                view.FilterLabel = "";
                return;
            }

            List<string> filters = table.AsEnumerable()
                .Select(data => data.Field<string>(view.Filter))
                .Distinct()
                .ToList();

            int count = filters.Count;
            if (count == 0)
            {
                return;
            }

            if (filters.Contains(view.FilterLabel))
            {
                // Find where the label is
                int index = filters.IndexOf(view.FilterLabel);

                // Increase or decrease by 1 depending on which button was clicked
                index = name == "right" ? index + 1 : index - 1;

                if (index == count)
                {
                    index = 0;
                }

                if (index < 0)
                {
                    index = count - 1;
                }

                view.FilterLabel = filters[index];
            }
            else
            {
                view.FilterLabel = filters[0];
            }
        }

        /// <summary>
        /// Generate the list of column names from the table
        /// </summary>
        private IEnumerable<string> FindColumns(DataTable table, int timescale)
        {
            // Determine the column names from 
            var cols = table.AsEnumerable().Select(r => r.Field<object>(view.ColumnVariable).ToString()).Distinct();

            // Rescale the time over the columns if required
            if (view.ColumnVariable == "Clock.Today")
            {
                return cols.GroupBy(col => col.Substring(10 - timescale, timescale)).Select(group => group.Key);
            }
            else
            {
                return cols;
            }
        }

        /// <summary>
        /// Find a list of row data 
        /// </summary>
        private IEnumerable<string[]> FindRows(DataTable table, IEnumerable<string> cols, int timescale)
        {
            // The index of the column which contains data to pivot into rows
            int index = table.Columns[view.RowVariable].Ordinal;

            var rows = table.AsEnumerable()
                // Group the table data by the distinct elements in the column
                .GroupBy(row => row.ItemArray[index].ToString())
                // Aggregate the data in each new group of rows
                .Select(grouping => PivotRow(grouping, cols));

            // If time series data is being pivoted, further aggregate the rows based on the time scale 
            if (view.RowVariable == "Clock.Today")
            {
                // Group the data based on time interval
                return rows.GroupBy(row => row[0].Substring(10 - timescale, timescale))
                    // Compress each group into a single row with the key as the new identifier
                    .Select(group => CompressRows(group.ToList(), group.Key));
            }
            else
            {
                return rows;
            }
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
            .Where(data => data[view.ColumnVariable].ToString().Contains(c))
            // Apply the filter
            .Where(data => ApplyFilter(data))
            // Select the data from that row
            .Select(data => (double)data[view.Value]))
            // Aggregate the data
            .Select(iEnum => AggregateDoubles(iEnum).ToString())
            // Format the object
            .ToList();

            // Add the names of the rows            
            values.Insert(0, grouping.Key);

            return values.ToArray();
        }

        /// <summary>
        /// Applies the current filter to a data row to determine if it is valid
        /// </summary>
        private bool ApplyFilter(DataRow data)
        {
            if (view.Filter == "None")
            {
                return true;
            }

            return data[view.Filter].ToString().Contains(view.FilterLabel);
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
            DataTable output = new DataTable($"{view.Expression}Of{view.LedgerName}Resource{view.Value}");
            output.Columns.AddRange(cols.Select(col => new DataColumn(col, typeof(double))).ToArray());

            // Attach another column for the row names
            string name = view.RowVariable;
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
                switch (view.Expression)
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
            else
            {
                return 0;
            }
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
            if (data == null)
            {
                return;
            }

            // Store the data
            var store = Apsim.Find(table, typeof(IDataStore)) as IDataStore;
            store.Writer.WriteTable(data);
        }

        /// <summary>
        /// Detach the model from the view
        /// </summary>
        public void Detach()
        {
            view.UpdateData -= OnUpdateData;
            view.StoreData -= OnStoreData;
            view.TrackChanges -= OnTrackChanges;

            view.Detach();
            view.Grid.Dispose();
        }

        /// <summary>
        /// Track changes made to the view
        /// </summary>
        private void OnTrackChanges(object sender, EventArgs e)
        {
            if (sender is PivotTableView.ViewBox vb)
            {
                TrackChanges(vb.Name, vb.ID);
                return;
            }

            if (sender is Gtk.Label lbl)
            {
                TrackChanges(lbl.Name, lbl.Text);
                return;
            }
        }

        /// <summary>
        /// Tracks any user selections on the interface to preserve them between views/sessions.
        /// </summary>
        private void TrackChanges(string name, object value)
        {
            // Change the information of the property in the model
            var property = table.GetType().GetProperty(name);
            property.SetValue(table, value);

            // Track this change in the command history 
            ChangeProperty command = new ChangeProperty(table, name, value);
            explorer.CommandHistory.Add(command);
        }

    }
}
