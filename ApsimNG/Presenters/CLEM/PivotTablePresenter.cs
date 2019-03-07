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
        private PivotTable pivot = null;

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
            this.pivot = model as PivotTable;
            this.view = view as PivotTableView;
            this.explorer = explorerPresenter;

            this.view.OnUpdateData += UpdateData;

            GetLedgers();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetLedgers()
        {
            CLEMFolder folder = new CLEMFolder();
            folder = Apsim.Find(pivot, typeof(CLEMFolder)) as CLEMFolder;

            foreach (var child in folder.Children)
            {
                if (child.GetType() == typeof(ReportResourceLedger))
                {
                    ReportResourceLedger ledger = child as ReportResourceLedger;

                    view.AddLedger(ledger.Name);
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateData(object sender, EventArgs e)
        {
            IStorageReader reader = Apsim.Find(pivot, typeof(IStorageReader)) as IStorageReader;

            DataTable input = reader.GetData(view.Ledger);
            DataTable output = new DataTable($"{view.Expression}Of{view.Value}");

            var rows = input.AsEnumerable().Select(r => r.Field<string>(view.Row)).Distinct();
            var cols = input.AsEnumerable().Select(r => r.Field<string>(view.Column)).Distinct();

            output.Columns.Add(new DataColumn(view.Row, typeof(string)));
            foreach(var col in cols)
            {
                output.Columns.Add(new DataColumn(col.ToString(), typeof(double)));
            }

            foreach(var row in rows)
            {
                DataRow data = output.NewRow();
                data[view.Row] = row;

                foreach(var col in cols)
                {
                    double value = 0;

                    var values =
                        from item in input.AsEnumerable()
                        where item.Field<string>(view.Column) == col
                        where item.Field<string>(view.Row) == row
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

                    data[col] = value;                    
                }
                output.Rows.Add(data);
                output.AcceptChanges();
            }            

            view.gridview.DataSource = output;
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
