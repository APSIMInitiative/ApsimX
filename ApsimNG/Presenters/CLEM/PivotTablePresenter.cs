// -----------------------------------------------------------------------
// <copyright file="CustomQueryPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

using ApsimNG.Views.CLEM;
using Models.Core;
using Models.CLEM;
using Models.CLEM.Reporting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using UserInterface.Commands;
using UserInterface.Presenters;

namespace ApsimNG.Presenters.CLEM
{
    class PivotTablePresenter : IPresenter
    {
        /// <summary>
        /// The CustomQuery object
        /// </summary>
        private PivotTable table = null;

        /// <summary>
        /// The CustomQueryView used
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

            SetLedgers();

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

            OnUpdateData(null, EventArgs.Empty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetLedgers()
        {
            CLEMFolder folder = new CLEMFolder();
            folder = Apsim.Find(table, typeof(CLEMFolder)) as CLEMFolder;

            foreach (var child in folder.Children)
            {
                if (child.GetType() != typeof(ReportResourceLedger)) continue;
                
                ReportResourceLedger ledger = child as ReportResourceLedger;
                view.AddLedger(ledger.Name);                
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUpdateData(object sender, EventArgs e)
        {
            IStorageReader reader = Apsim.Find(table, typeof(IStorageReader)) as IStorageReader;
            DataTable input = reader.GetData(view.Ledger.Text);

            // Find distinct values in the chosen pivot
            table.Pivots = new List<string>(
                input
                .AsEnumerable()
                .Select(r => r.Field<object>(view.Pivot.Text).ToString())
                .Distinct()
                .ToList());

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

            string name = "Pivot: " + table.GetPivot();
            output.Columns.Add(name, typeof(string)).SetOrdinal(0);

            // Populate the table with rows
            foreach (var row in rows)
            {
                DataRow data = output.NewRow();
                data[name] = row;

                foreach(var col in cols)
                {
                    double value = 0;

                    // Search DataTable for all values that match the current row/column/pivot
                    var values =
                        from item in input.AsEnumerable()
                        where item.Field<object>(view.Column.Text).ToString() == col.ToString()
                        where item.Field<object>(view.Row.Text).ToString() == row.ToString()
                        where item.Field<object>(view.Pivot.Text).ToString() == table.GetPivot()
                        select item.Field<double>(view.Value.Text);

                    // Evaluate the expression on selected values
                    if (values.Count() > 0) switch (view.Expression.Text)
                    {
                        case "Sum":
                            value = values.Sum();
                            break;

                        case "Average":
                            value = values.Average();
                            break;

                        case "Max":
                            value = values.Max();
                            break;

                        case "Min":
                            value = values.Min();
                            break;

                        default:                            
                            break;
                    }

                    data[col.ToString()] = value;                    
                }
                output.Rows.Add(data);
                output.AcceptChanges();
            }            

            view.gridview.DataSource = output;
        }

        private void OnStoreData(object sender, EventArgs e)
        {
            DataTable data = view.gridview.DataSource;
            IStorageReader reader = Apsim.Find(table, typeof(IStorageReader)) as IStorageReader;
            reader.DeleteDataInTable(data.TableName);
            reader.WriteTable(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            var p = table.GetType().GetProperty(args.Name);
            p.SetValue(table, args.Value);
            
            ChangeProperty command = new ChangeProperty(table, args.Name, args.Value);
            explorer.CommandHistory.Add(command);
        }

    }
}
