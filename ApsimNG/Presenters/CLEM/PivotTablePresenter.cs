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

            var store = Apsim.Find(table, typeof(IDataStore)) as IDataStore;
            DataTable input = store.Reader.GetData(view.Ledger.Text);

            // Don't try to update if data source isn't found            
            if (input == null) return;

            // Find distinct values in the chosen pivot            
            table.Pivots = new List<string>(
                input
                .AsEnumerable()
                .Select(r => r.Field<object>(view.Pivot.Text).ToString())
                .Distinct()
                .ToList());

            // Reset the table ID if the new pivot list is too short
            if (table.Pivots.Count <= table.ID) table.ID = 0;

            // Determine the row/column values
            var rows = input.AsEnumerable().Select(r => r.Field<object>(view.Row.Text)).Distinct();
            var cols = input.AsEnumerable().Select(r => r.Field<object>(view.Column.Text)).Distinct();

            DataTable output = new DataTable($"{view.Expression.Text}Of{table.GetPivot()}{view.Value.Text}");

            // Attach columns to the output table          
            foreach (var col in cols)
            {
                output.Columns.Add(col.ToString(), typeof(double));
            }

            // Attach a column for the row titles
            string name = "Pivot: " + table.GetPivot();
            output.Columns.Add(name, typeof(string)).SetOrdinal(0);

            // Populate the table with rows
            foreach (var row in rows)
            {
                DataRow data = output.NewRow();
                data[name] = row;

                foreach (var col in cols)
                {
                    // Search DataTable for all values that match the current row/column
                    var items =
                        from item in input.AsEnumerable()
                        where item.Field<object>(view.Column.Text).ToString() == col.ToString()
                        where item.Field<object>(view.Row.Text).ToString() == row.ToString()
                        select item;

                    // Selects the values based on the current pivot
                    var values =
                        from item in items
                        where item.Field<object>(view.Pivot.Text).ToString() == table.GetPivot()
                        select item.Field<double>(view.Value.Text);

                    // Evaluate the expression on selected values                   
                    data[col.ToString()] = Aggregate(values);
                }
                output.Rows.Add(data);
                output.AcceptChanges();
            }

            view.gridview.DataSource = output;
        }

        /// <summary>
        /// Takes a collection of values from a set of rows and aggregates them
        /// </summary>
        /// <param name="values">The collection of values</param>
        private double Aggregate(EnumerableRowCollection<double> values)
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
            DataTable data = view.gridview.DataSource;
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
            view.gridview.Dispose();
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
