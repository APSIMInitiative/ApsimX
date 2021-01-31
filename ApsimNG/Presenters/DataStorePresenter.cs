using UserInterface.Commands;

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
    using Models.Core.Run;
    using Models.Storage;
    using System.Globalization;
    using System.Collections.Generic;

    /// <summary>A data store presenter connecting a data store model with a data store view</summary>
    public class DataStorePresenter : GridPresenter, IPresenter
    {
        /// <summary>The data store model to work with.</summary>
        private IDataStore dataStore;

        /// <summary>The data store view to work with.</summary>
        private ViewBase view;

        /// <summary>
        /// The intellisense.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Checkpoint name drop down.</summary>
        private DropDownView checkpointDropDown;

        /// <summary>Only allow these tables to be selected/displayed.</summary>
        private string[] tablesFilter = new string[0];

        /// <summary>table name drop down.</summary>
        public DropDownView tableDropDown { get; private set; }

        /// <summary>Column filter edit box.</summary>
        private EditView columnFilterEditBox;

        /// <summary>Row filter edit box.</summary>
        private EditView rowFilterEditBox;

        /// <summary>Row filter edit box.</summary>
        private EditView maxNumRecordsEditBox;

        /// <summary>Gets or sets the experiment filter. When specified, will only show experiment data.</summary>
        public Experiment ExperimentFilter { get; set; }

        /// <summary>Gets or sets the simulation filter. When specified, will only show simulation data.</summary>
        public Simulation SimulationFilter { get; set; }

        /// <summary>When specified will only show data from a given zone.</summary>
        public Zone ZoneFilter { get; set; }

        public DataStorePresenter()
        {

        }

        /// <summary>
        /// Constructor. Used to restrict which tables can be selected.
        /// </summary>
        /// <param name="tables">Tables which may be displayed to the user.</param>
        public DataStorePresenter(string[] tables)
        {
            if (tables == null)
                throw new ArgumentNullException(nameof(tables));
            tablesFilter = tables;
        }

        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The data store model to work with.</param>
        /// <param name="v">Data store view to work with.</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public override void Attach(object model, object v, ExplorerPresenter explorerPresenter)
        {
            dataStore = model as IDataStore;
            view = v as ViewBase;
            this.explorerPresenter = explorerPresenter;

            intellisense = new IntellisensePresenter(this.view as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

            checkpointDropDown = view.GetControl<DropDownView>("checkpointDropDown");
            tableDropDown = view.GetControl<DropDownView>("tableDropDown");
            columnFilterEditBox = view.GetControl<EditView>("columnFilterEditBox");
            rowFilterEditBox = view.GetControl<EditView>("rowFilterEditBox");
            grid = view.GetControl<GridView>("grid");
            maxNumRecordsEditBox = view.GetControl<EditView>("maxNumRecordsEditBox");

            base.Attach(model, grid, explorerPresenter);

            tableDropDown.IsEditable = false;
            grid.ReadOnly = true;
            grid.NumericFormat = "N3";
            if (dataStore != null)
            {
                tableDropDown.Values = dataStore.Reader.TableAndViewNames.ToArray();
                if (tablesFilter != null && tablesFilter.Length > 0)
                    tableDropDown.Values = tableDropDown.Values.Intersect(tablesFilter).ToArray();
                checkpointDropDown.Values = dataStore.Reader.CheckpointNames.ToArray();
                if (checkpointDropDown.Values.Length > 0)
                    checkpointDropDown.SelectedValue = checkpointDropDown.Values[0];
                if (Utility.Configuration.Settings.MaximumRowsOnReportGrid > 0)
                {
                    maxNumRecordsEditBox.Text = Utility.Configuration.Settings.MaximumRowsOnReportGrid.ToString();
                }
                tableDropDown.SelectedIndex = -1;
            }

            tableDropDown.Changed += this.OnTableSelected;
            columnFilterEditBox.Leave += OnColumnFilterChanged;
            columnFilterEditBox.IntellisenseItemsNeeded += OnIntellisenseNeeded;
            rowFilterEditBox.Leave += OnColumnFilterChanged;
            maxNumRecordsEditBox.Leave += OnMaximumNumberRecordsChanged;
            checkpointDropDown.Changed += OnCheckpointDropDownChanged;
            PopulateGrid();
        }

        /// <summary>Detach the model from the view.</summary>
        public override void Detach()
        {
            base.Detach();
            maxNumRecordsEditBox.EndEdit();
            tableDropDown.Changed -= OnTableSelected;
            columnFilterEditBox.Leave -= OnColumnFilterChanged;
            rowFilterEditBox.Leave -= OnColumnFilterChanged;
            maxNumRecordsEditBox.Leave -= OnMaximumNumberRecordsChanged;
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
        }

        /// <summary>Populate the grid control with data.</summary>
        public void PopulateGrid()
        {
            using (DataTable data = GetData())
            {
                // Strip out unwanted columns.
                if (data != null)
                {
                    int colPos = 0;
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        if (data.Columns[i].ColumnName.Contains("Date") || data.Columns[i].ColumnName.Contains("Today"))
                        {
                            //numFrozenColumns = i;
                            // Make the date column the left-most column
                            data.Columns[i].SetOrdinal(0);
                            colPos = 1;
                            break;
                        }
                    }

                    // Make order of columns "Date, Simulation Name, Zone"
                    if (data.Columns.Contains("SimulationName"))
                        data.Columns["SimulationName"].SetOrdinal(colPos++);
                    if (data.Columns.Contains("Zone"))
                        data.Columns["Zone"].SetOrdinal(colPos++);
                    int numFrozenColumns = colPos;

                    // Remove checkpoint columns.
                    if (data.Columns.Contains("CheckpointName"))
                        data.Columns.Remove("CheckpointName");

                    if (data.Columns.Contains("CheckpointID"))
                        data.Columns.Remove("CheckpointID");

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
                                 columnFilterEditBox.Text != string.Empty &&
                                 !columnFilterEditBox.Text.Split(',').Where(x => !string.IsNullOrEmpty(x)).Any(c => data.Columns[i].ColumnName.Contains(c.Trim())))
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
                            units = dataStore.Reader.Units(tableDropDown.SelectedValue, column.ColumnName);
                        }

                        int posLastDot = column.ColumnName.LastIndexOf('.');
                        if (posLastDot != -1)
                        {
                            column.ColumnName = column.ColumnName.Insert(posLastDot + 1, "\r\n");
                        }

                        // Add the units, if they're available
                        if (units != null)
                        {
                            column.ColumnName = column.ColumnName + " (" + units + ")";
                        }
                    }

                    grid.DataSource = data;
                    grid.LockLeftMostColumns(numFrozenColumns);  // lock simulationname, zone, date.
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

                    // Note that the filter contains the zone filter and experiment filter but not simulation filter.
                    string filter = GetFilter();

                    data = dataStore.Reader.GetData(tableName: tableDropDown.SelectedValue,
                                                    checkpointName: checkpointDropDown.SelectedValue,
                                                    simulationName: SimulationFilter?.Name,
                                                    filter: filter,
                                                    from: start,
                                                    count: count);
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

        private string GetFilter()
        {
            string filter = rowFilterEditBox.Text;

            if (ExperimentFilter != null)
            {
                // fixme: this makes some serious assumptions about how the query is generated in the data store layer...
                IEnumerable<string> names = ExperimentFilter.GenerateSimulationDescriptions().Select(s => s.Name);
                string exptFilter = "S.[Name] IN " + "(" + StringUtilities.Build(names, delimiter: ",", prefix: "'", suffix: "'") + ")";
                filter = AppendToFilter(filter, exptFilter);
            }

            if (ZoneFilter != null)
            {
                // More assumptions about column names
                string zoneFilter = $"T.[Zone] = '{ZoneFilter.Name}'";
                filter = AppendToFilter(filter, zoneFilter);
            }

            return filter;
        }

        /// <summary>
        /// Appends a clause to the filter.
        /// </summary>
        /// <param name="filter">Existing filter to append to.</param>
        /// <param name="value">Value to append to the filter.</param>
        private string AppendToFilter(string filter, string value)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return value;

            return $"{filter} AND {value}";
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
                if (intellisense.GenerateSeriesCompletions(args.Code, args.Offset, tableDropDown.SelectedValue, dataStore.Reader))
                    intellisense.Show(args.Coordinates.X, args.Coordinates.Y);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            try
            {
                columnFilterEditBox.InsertCompletionOption(args.ItemSelected, args.TriggerWord);
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
            if (maxNumRecordsEditBox.Text == string.Empty)
            {
                Utility.Configuration.Settings.MaximumRowsOnReportGrid = 0;
            }
            else
            {
                try
                {
                    Utility.Configuration.Settings.MaximumRowsOnReportGrid = Convert.ToInt32(maxNumRecordsEditBox.Text, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                }
            }

            PopulateGrid();
        }

        /// <summary>
        /// Checkpoint name has changed by user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCheckpointDropDownChanged(object sender, EventArgs e)
        {
            PopulateGrid();
        }

    }
}
