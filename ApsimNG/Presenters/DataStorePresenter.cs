// -----------------------------------------------------------------------
// <copyright file="DataStorePresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Data;
    using System.Linq;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Factorial;
    using Views;
    using EventArguments;

    /// <summary>A data store presenter connecting a data store model with a data store view</summary>
    public class DataStorePresenter : IPresenter
    {
        /// <summary>The data store model to work with.</summary>
        private IStorageReader dataStore;

        /// <summary>The data store view to work with.</summary>
        private IDataStoreView view;

        /// <summary>
        /// The intellisense.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Gets or sets the experiment filter. When specified, will only show experiment data.</summary>
        public Experiment ExperimentFilter { get; set; }

        /// <summary>Gets or sets the simulation filter. When specified, will only show simulation data.</summary>
        public Simulation SimulationFilter { get; set; }

        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The data store model to work with.</param>
        /// <param name="view">Data store view to work with.</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            dataStore = model as IStorageReader;
            this.view = view as IDataStoreView;
            this.explorerPresenter = explorerPresenter;
            this.intellisense = new IntellisensePresenter(this.view as ViewBase);
            intellisense.ItemSelected += OnInsertIntellisenseItem;

            this.view.TableList.IsEditable = false;
            this.view.Grid.ReadOnly = true;
            this.view.Grid.NumericFormat = "N3";
            if (dataStore != null)
            {
                this.view.TableList.Values = dataStore.TableNames.ToArray();
                if (Utility.Configuration.Settings.MaximumRowsOnReportGrid > 0)
                {
                    this.view.MaximumNumberRecords.Value = Utility.Configuration.Settings.MaximumRowsOnReportGrid.ToString();
                }
            }

            this.view.TableList.Changed += this.OnTableSelected;
            this.view.ColumnFilter.Changed += OnColumnFilterChanged;
            this.view.ColumnFilter.IntellisenseItemsNeeded += OnIntellisenseNeeded;
            this.view.MaximumNumberRecords.Changed += OnMaximumNumberRecordsChanged;
            PopulateGrid();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            (view.MaximumNumberRecords as EditView).EndEdit();
            view.TableList.Changed -= OnTableSelected;
            view.ColumnFilter.Changed -= OnColumnFilterChanged;
            view.MaximumNumberRecords.Changed -= OnMaximumNumberRecordsChanged;
        }

        /// <summary>Populate the grid control with data.</summary>
        public void PopulateGrid()
        {
            using (DataTable data = GetData())
            {
                // Strip out unwanted columns.
                if (data != null)
                {
                    int numFrozenColumns = 1;
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        if (data.Columns[i].ColumnName.Contains("Date") || data.Columns[i].ColumnName.Contains("Today"))
                        {
                            numFrozenColumns = i;
                            // Make the date column the left-most column
                            data.Columns[i].SetOrdinal(0);
                            break;
                        }
                    }

                    int colPos = 1;
                    // Make "Simulation Name" the second column, if present
                    if (data.Columns.Contains("SimulationName"))
                        data.Columns["SimulationName"].SetOrdinal(colPos++);
                    if (data.Columns.Contains("Zone"))
                        data.Columns["Zone"].SetOrdinal(colPos++);

                    int simulationId = 0;
         
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        if (data.Columns[i].ColumnName == "SimulationID")
                        {
                            if (simulationId == 0 && data.Rows.Count > 0)
                            {
                                simulationId = (int)data.Rows[0][i];
                            }

                            data.Columns.RemoveAt(i);
                            i--;
                        }
                        else if (i >= numFrozenColumns &&
                                 view.ColumnFilter.Value != string.Empty &&
                                 !view.ColumnFilter.Value.Split(',').Where(x => !string.IsNullOrEmpty(x)).Any(c => data.Columns[i].ColumnName.Contains(c.Trim())))
                        {
                            data.Columns.RemoveAt(i);
                            i--;
                        }
                    }

                    // Convert the last dot to a CRLF so that the columns in the grid are narrower.
                    foreach (DataColumn column in data.Columns)
                    {
                        string units = null;

                        // Try to obtain units
                        if (dataStore != null && simulationId != 0)
                        {
                            units = dataStore.GetUnits(view.TableList.SelectedValue, column.ColumnName);
                        }

                        int posLastDot = column.ColumnName.LastIndexOf('.');
                        if (posLastDot != -1)
                        {
                            column.ColumnName = column.ColumnName.Insert(posLastDot + 1, "\r\n");
                        }

                        // Add the units, if they're available
                        if (units != null)
                        {
                            column.ColumnName = column.ColumnName + " " + units;
                        }
                    }

                    this.view.Grid.DataSource = data;
                    this.view.Grid.LockLeftMostColumns(numFrozenColumns);  // lock simulationname, zone, date.
                }
            }
        }

        /// <summary>Get data to show in grid.</summary>
        /// <returns>A data table of all data.</returns>
        private DataTable GetData()
        {
            DataTable data = null;
            if (dataStore != null)
            {
                try
                {
                    int start = 0;
                    int count = Utility.Configuration.Settings.MaximumRowsOnReportGrid;
                    if (ExperimentFilter != null)
                    {
                        string filter = "S.NAME IN " + "(" + StringUtilities.Build(ExperimentFilter.GetSimulationNames(), delimiter: ",", prefix: "'", suffix: "'") + ")";
                        data = dataStore.GetData(tableName: view.TableList.SelectedValue, filter: filter, from: start, count: count);
                    }
                    else if (SimulationFilter != null)
                    {
                        data = dataStore.GetData(
                                                 simulationName: SimulationFilter.Name,
                                                 tableName: view.TableList.SelectedValue,
                                                 from: start, 
                                                 count: count);
                    }
                    else
                    {
                        data = dataStore.GetData(
                                                tableName: view.TableList.SelectedValue,
                                                count: Utility.Configuration.Settings.MaximumRowsOnReportGrid);
                    }
                }
                catch (Exception e)
                {
                    this.explorerPresenter.MainPresenter.ShowError(new Exception("Error reading data tables.", e));
                }
            }
            else
            {
                data = new DataTable();
            }

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

        /// <summary>
        /// The view is asking for items for the intellisense.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseNeeded(object sender, NeedContextItemsArgs args)
        {
            try
            {
                if (intellisense.GenerateSeriesCompletions(args.Code, args.Offset, view.TableList.SelectedValue, dataStore))
                    intellisense.Show(args.Coordinates.Item1, args.Coordinates.Item2);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        private void OnInsertIntellisenseItem(object sender, IntellisenseItemSelectedArgs args)
        {
            try
            {
                view.ColumnFilter.InsertCompletionOption(args.ItemSelected, args.TriggerWord);
                PopulateGrid();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>The maximum number of records has changed.</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnMaximumNumberRecordsChanged(object sender, EventArgs e)
        {
            if (view.MaximumNumberRecords.Value == string.Empty)
            {
                Utility.Configuration.Settings.MaximumRowsOnReportGrid = 0;
            }
            else
            {
                try
                {
                    Utility.Configuration.Settings.MaximumRowsOnReportGrid = Convert.ToInt32(view.MaximumNumberRecords.Value);
                }
                catch (Exception)
                {  // If there are any errors, return 0
                    Utility.Configuration.Settings.MaximumRowsOnReportGrid = 0;
                }
            }

            PopulateGrid();
        }
    }
}
