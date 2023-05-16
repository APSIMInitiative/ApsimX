using UserInterface.EventArguments;
using Models.Core;
using Models.Factorial;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UserInterface.Views;
using APSIM.Shared.Documentation.Extensions;

namespace UserInterface.Presenters
{

    /// <summary>A data store presenter connecting a data store model with a data store view</summary>
    public class DataStorePresenter : IPresenter
    {
        /// <summary>The data store model to work with.</summary>
        private IDataStore dataStore;

        /// <summary>The sheet widget.</summary>
        private SheetWidget grid;

        ///// <summary>The sheet scrollbars</summary>
        //SheetScrollBars scrollbars;

        /// <summary>The data provider for the sheet</summary>
        PagedDataProvider dataProvider;

        /// <summary>The container that houses the sheet.</summary>
        private ContainerView sheetContainer;

        /// <summary>The intellisense.</summary>
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
        private LabelView statusLabel;

        private ViewBase view = null;

        /// <summary>Gets or sets the experiment filter. When specified, will only show experiment data.</summary>
        public Experiment ExperimentFilter { get; set; }

        /// <summary>Gets or sets the simulation filter. When specified, will only show simulation data.</summary>
        public Simulation SimulationFilter { get; set; }

        /// <summary>When specified will only show data from a given zone.</summary>
        public Zone ZoneFilter { get; set; }

        ///<summary>
        /// The list of stored column filters.
        /// </summary>
        private string temporaryColumnFilters = "";

        ///<summary>
        /// The list of stored row filters.
        /// </summary>
        private string temporaryRowFilters = "";

        /// <summary>Default constructor</summary>
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

            intellisense = new IntellisensePresenter(view as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

            checkpointDropDown = view.GetControl<DropDownView>("checkpointDropDown");
            tableDropDown = view.GetControl<DropDownView>("tableDropDown");
            columnFilterEditBox = view.GetControl<EditView>("columnFilterEditBox");
            rowFilterEditBox = view.GetControl<EditView>("rowFilterEditBox");
            sheetContainer = view.GetControl<ContainerView>("grid");
            statusLabel = view.GetControl<LabelView>("statusLabel");

            tableDropDown.IsEditable = false;
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
                    statusLabel.Text = Utility.Configuration.Settings.MaximumRowsOnReportGrid.ToString();
                }
                tableDropDown.SelectedIndex = 0;
            }

            tableDropDown.Changed += this.OnTableSelected;
            columnFilterEditBox.Leave += OnColumnFilterChanged;
            columnFilterEditBox.IntellisenseItemsNeeded += OnIntellisenseNeeded;
            rowFilterEditBox.Leave += OnColumnFilterChanged;
            checkpointDropDown.Changed += OnCheckpointDropDownChanged;

            // Add the filter strings back in the text field.
            if (explorerPresenter.GetFilters().Count() != 0)
            {
                columnFilterEditBox.Text = explorerPresenter.GetFilters()[0];
                rowFilterEditBox.Text = explorerPresenter.GetFilters()[1];
            }
            PopulateGrid();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            //base.Detach();
            // Keep the column and row filters
            explorerPresenter.KeepFilter(temporaryColumnFilters, temporaryRowFilters); 
            temporaryRowFilters = rowFilterEditBox.Text;
            tableDropDown.Changed -= OnTableSelected;
            columnFilterEditBox.Leave -= OnColumnFilterChanged;
            rowFilterEditBox.Leave -= OnColumnFilterChanged;
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
            view.Dispose();
            CleanupSheet();
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

                string filter = rowFilterEditBox.Text;
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
                        // Cleanup existing sheet instances before creating new ones.
                        CleanupSheet();

                        dataProvider = new PagedDataProvider(dataStore.Reader,
                                                             checkpointDropDown.SelectedValue,
                                                             tableDropDown.SelectedValue,
                                                             simulationNames,
                                                             columnFilterEditBox.Text,
                                                             filter);
                        dataProvider.PagingStart += (sender, args) => explorerPresenter.MainPresenter.ShowWaitCursor(true);
                        dataProvider.PagingEnd += (sender, args) => explorerPresenter.MainPresenter.ShowWaitCursor(false);

                        grid = new SheetWidget();
                        grid.Sheet = new Sheet();
                        grid.Sheet.DataProvider = dataProvider;
                        grid.Sheet.CellSelector = new SingleCellSelect(grid.Sheet, grid);
                        grid.Sheet.ScrollBars = new SheetScrollBars(grid.Sheet, grid);
                        grid.Sheet.CellPainter = new DefaultCellPainter(grid.Sheet, grid);
                        grid.Sheet.NumberFrozenRows = dataProvider.NumHeadingRows;
                        grid.Sheet.NumberFrozenColumns = dataProvider.NumPriorityColumns;

                        sheetContainer.Add(grid.Sheet.ScrollBars.MainWidget);
                        statusLabel.Text = $"Number of rows: {dataProvider.RowCount - dataProvider.NumHeadingRows}";
                    }
                    catch (Exception err)
                    {
                        explorerPresenter.MainPresenter.ShowError(err.ToString());
                    }
                }
            }
        }

        /// <summary>Clean up the sheet components.</summary>
        private void CleanupSheet()
        {
            if (grid != null && grid.Sheet.CellSelector != null)
            {
                dataProvider.Cleanup();
                (grid.Sheet.CellSelector as SingleCellSelect).Cleanup();
                grid.Sheet.ScrollBars.Cleanup();
            }
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
            // Store the filters temporarily.
            temporaryColumnFilters = columnFilterEditBox.Text;
            temporaryRowFilters = rowFilterEditBox.Text;
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

        /// <summary>
        /// Invoked when an intellisense item is selected by user.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The event arguments.</param>
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