// -----------------------------------------------------------------------
// <copyright file="DataStorePresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Factorial;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Views;

    /// <summary>A data store presenter connecting a data store model with a data store view</summary>
    public class DataStorePresenter : IPresenter
    {
        /// <summary>The data store model to work with.</summary>
        private DataStore dataStore;

        /// <summary>The data store view to work with.</summary>
        private IDataStoreView view;

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>When specified, will only show experiment data.</summary>
        public Experiment ExperimentFilter { get; set; }

        /// <summary>When specified, will only show simulation data.</summary>
        public Simulation SimulationFilter { get; set; }

        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The data store model to work with.</param>
        /// <param name="view">Data store view to work with.</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            dataStore = model as DataStore;
            this.view = view as IDataStoreView;
            this.explorerPresenter = explorerPresenter;

            this.view.TableList.IsEditable = false;
            this.view.Grid.ReadOnly = true;
            this.view.Grid.NumericFormat = "N3";
            this.view.TableList.Values = this.GetTableNames();
            if (dataStore != null && dataStore.MaximumResultsPerPage > 0)
                this.view.MaximumNumberRecords.Value = dataStore.MaximumResultsPerPage.ToString();

            this.view.Grid.ResizeControls();
            this.view.TableList.Changed += this.OnTableSelected;
            this.view.ColumnFilter.Changed += OnColumnFilterChanged;
            this.view.MaximumNumberRecords.Changed += OnMaximumNumberRecordsChanged;
            PopulateGrid();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            view.TableList.Changed -= OnTableSelected;
            view.ColumnFilter.Changed -= OnColumnFilterChanged;
            view.MaximumNumberRecords.Changed -= OnMaximumNumberRecordsChanged;
        }

        /// <summary>Get a list of table names to send to the view.</summary>
        private string[] GetTableNames()
        {
            List<string> tableNames = new List<string>();
            if (this.dataStore != null)
            {
                foreach (string tableName in this.dataStore.TableNames)
                {
                    if (tableName != "Messages" && tableName != "InitialConditions")
                    {
                        tableNames.Add(tableName);
                    }
                }
            }

            return tableNames.ToArray();
        }

        /// <summary>Populate the grid control with data.</summary>
        public void PopulateGrid()
        {
            DataTable data  = GetData();

            // Strip out unwanted columns.
            if (data != null)
            {
                int numFrozenColumns = 1;
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    if (data.Columns[i].ColumnName.Contains("Date") || data.Columns[i].ColumnName.Contains("Today"))
                    {
                        numFrozenColumns = i;
                        break;
                    }
                }


                for (int i = 0; i < data.Columns.Count; i++)
                {

                    if (data.Columns[i].ColumnName == "SimulationID")
                    {
                        data.Columns.RemoveAt(i);
                        i--;
                    }
                    else if (i >= numFrozenColumns &&
                             this.view.ColumnFilter.Value != string.Empty &&
                             !data.Columns[i].ColumnName.Contains(this.view.ColumnFilter.Value))
                    {
                        data.Columns.RemoveAt(i);
                        i--;
                    }
                }

                // Convert the last dot to a CRLF so that the columns in the grid are narrower.
                foreach (DataColumn column in data.Columns)
                {
                    int posLastDot = column.ColumnName.LastIndexOf('.');
                    if (posLastDot != -1)
                        column.ColumnName = column.ColumnName.Insert(posLastDot + 1, "\r\n");
                }

                this.view.Grid.DataSource = data;
                this.view.Grid.LockLeftMostColumns(numFrozenColumns);  // lock simulationname, zone, date.
            }
        }

        /// <summary>Get data to show in grid.</summary>
        /// <returns>A data table of all data.</returns>
        private DataTable GetData()
        {
            DataTable data;
            if (dataStore != null)
            {
                int start = 0;
                int count = dataStore.MaximumResultsPerPage;
                if (ExperimentFilter != null)
                {
                    string filter = "NAME IN " + "(" + StringUtilities.Build(ExperimentFilter.Names(), delimiter: ",", prefix: "'", suffix: "'") + ")";
                    data = dataStore.GetFilteredData(view.TableList.SelectedValue, filter, start, count);
                }
                else if (SimulationFilter != null)
                    data = dataStore.GetData(SimulationFilter.Name, view.TableList.SelectedValue, false, start, count);
                else
                    data = dataStore.GetData("*", view.TableList.SelectedValue, true, 0, dataStore.MaximumResultsPerPage);
            }
            else
                data = new DataTable();
            return data;
        }

        /// <summary>The selected table has changed.</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnTableSelected(object sender, EventArgs e)
        {
            PopulateGrid();
        }

        /// <summary>The column filter has changed.</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnColumnFilterChanged(object sender, EventArgs e)
        {
            PopulateGrid();
        }

        /// <summary>The maximum number of records has changed.</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnMaximumNumberRecordsChanged(object sender, EventArgs e)
        {
            if (view.MaximumNumberRecords.Value == string.Empty)
                dataStore.MaximumResultsPerPage = 0;
            else
                dataStore.MaximumResultsPerPage = Convert.ToInt32(view.MaximumNumberRecords.Value);
            PopulateGrid();
        }
    }
}
