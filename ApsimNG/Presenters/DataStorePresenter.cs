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
    public class DataStorePresenter : IPresenter
    {
        /// <summary>The data store model to work with.</summary>
        private IDataStore dataStore;

        /// <summary>The data store view to work with.</summary>
        private ViewBase view;

        private SheetView grid;

        private ContainerView container;

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
        public void Attach(object model, object v, ExplorerPresenter explorerPresenter)
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

            container = view.GetControl<ContainerView>("grid");
                
            maxNumRecordsEditBox = view.GetControl<EditView>("maxNumRecordsEditBox");

            //base.Attach(model, grid, explorerPresenter);

            tableDropDown.IsEditable = false;
            //grid.ReadOnly = true;
            //grid.NumericFormat = "N3";
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
                tableDropDown.SelectedIndex = 0;
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
        public void Detach()
        {
            //base.Detach();
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
            if (!string.IsNullOrEmpty(tableDropDown.SelectedValue))
            {
                // Note that the filter contains the zone filter and experiment filter but not simulation filter.
                IEnumerable<string> simulationNames = null;
                if (ExperimentFilter != null)
                {
                    // fixme: this makes some serious assumptions about how the query is generated in the data store layer...
                    simulationNames = ExperimentFilter.GenerateSimulationDescriptions().Select(s => s.Name);
                }
                else if (SimulationFilter == null)
                    simulationNames = null;
                else
                    simulationNames = new string[] { SimulationFilter.Name };

                string filter = GetFilter();
                if (ZoneFilter != null)
                {
                    // More assumptions about column names
                    filter = AppendToFilter(filter, $"[Zone] = '{ZoneFilter.Name}'");
                }

                // Create sheet control
                if (tableDropDown.SelectedValue != null)
                {
                    try
                    {
                        grid = new SheetView();
                        var dataProvider = new PagedDataTableProvider(dataStore.Reader,
                                                                      checkpointDropDown.SelectedValue,
                                                                      tableDropDown.SelectedValue,
                                                                      simulationNames,
                                                                      columnFilterEditBox.Text,
                                                                      filter);
                        grid.DataProvider = dataProvider;
                        grid.NumberFrozenRows = dataProvider.NumHeadingRows;
                        grid.NumberFrozenColumns = dataProvider.NumPriorityColumns;
                        container.Add(grid);
                        var cellSelector = new SingleCellSelect(grid);
                        grid.CellPainter = new DefaultCellPainter(grid, sheetSelection: cellSelector);
                        var scrollbars = new SheetScrollBars(grid);
                    }
                    catch (Exception err)
                    {
                        explorerPresenter.MainPresenter.ShowError(err.ToString());
                    }
                }
            }
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

            if (filter == string.Empty)
                return null;
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
