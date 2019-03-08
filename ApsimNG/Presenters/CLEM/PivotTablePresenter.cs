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

            this.view.OnUpdateData += UpdateData;
            this.view.OnChangePivot += ChangePivot;

            SetLedgers();                                   
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
        private void UpdateData(object sender, EventArgs e)
        {
            IStorageReader reader = Apsim.Find(table, typeof(IStorageReader)) as IStorageReader;
            DataTable input = reader.GetData(view.Ledger);

            table.Pivots = new List<string>(
                input
                .AsEnumerable()
                .Select(r => r.Field<object>(view.Pivot).ToString())
                .Distinct()
                .ToList());

            if (table.Pivots.Count <= table.Id) table.Id = 0;
            
            var rows = input.AsEnumerable().Select(r => r.Field<object>(view.Row)).Distinct();
            var cols = input.AsEnumerable().Select(r => r.Field<object>(view.Column)).Distinct();

            DataTable output = new DataTable($"{view.Expression}Of{table.GetPivot()}{view.Value}");

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

                    var values =
                        from item in input.AsEnumerable()
                        where item.Field<object>(view.Column).ToString() == col.ToString()
                        where item.Field<object>(view.Row).ToString() == row.ToString()
                        where item.Field<object>(view.Pivot).ToString() == table.GetPivot()
                        select item.Field<double>(view.Value);

                    if (values.Count() > 0) switch (view.Expression)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangePivot(object sender, PivotTableView.ChangePivotArgs args)
        {
            if (args.Increase)
            {
                if (table.Id < table.Pivots.Count() - 1) table.Id += 1;
                else table.Id = 0;                
            }
            else
            {
                if (table.Id > 0) table.Id -= 1;
                else table.Id = table.Pivots.Count() - 1;
            }
        }

        /// <summary>
        /// Detach the model from the view
        /// </summary>
        public void Detach()
        {
            view.OnUpdateData -= UpdateData;

            TrackChanges();
        }

        /// <summary>
        /// Track changes made to the view
        /// </summary>
        private void TrackChanges()
        {
            //ChangeProperty sqlcom = new ChangeProperty(this.query, "Sql", view.Sql);
            //explorer.CommandHistory.Add(sqlcom);
        }

    }
}
