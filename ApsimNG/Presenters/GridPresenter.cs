using System;
using System.Collections.Generic;
using System.Data;
using Models.Core;
using Models.Interfaces;
using Models.Utilities;
using UserInterface.EventArguments;
using UserInterface.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// A generic grid presenter for displaying a data table and allowing editing.
    /// This is built to run within other presenters, so pass in a ContainerView in the attach method to have it drawn.
    /// Table Presenter can be used for a two grid view, or for additional functionality like intellisense
    /// </summary>
    class GridPresenter : IPresenter
    {
        /// <summary>Stores a reference to the model for intellisense or if it was passed in when attached.</summary>
        private Model model;

        /// <summary>The data store model to work with.</summary>
        private GridTable gridTable;

        /// <summary>The sheet widget.</summary>
        private SheetWidget grid;

        /// <summary>Currently Selected Row. Used to detect when a cell is slected</summary>
        private int selectedRow = -1;

        /// <summary>Currently Selected Column. Used to detect when a cell is slected</summary>
        private int selectedColumn = -1;

        /// <summary>The container that houses the sheet.</summary>
        private ContainerView sheetContainer;

        /// <summary>The popup context menu helper.</summary>
        private ContextMenuHelper contextMenuHelper;

        /// <summary>The popup context menu.</summary>
        private MenuView contextMenu;

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Parent explorer presenter.</summary>
        private List<string> contextMenuOptions;

        /// <summary>
        /// The intellisense.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>Delegate for a CellChanged event.</summary>
        /// <param name="dataProvider">The data provider.</param>
        /// <param name="colIndices">The indices of the columns that were changed.</param>
        /// <param name="rowIndices">The indices of the rows that were changed.</param>
        /// <param name="values">The values of the cells changed.</param>
        public delegate void CellChangedDelegate(ISheetDataProvider dataProvider, int[] colIndices, int[] rowIndices, string[] values);

        /// <summary>An event invoked when a cell changes.</summary>
        public event CellChangedDelegate CellChanged;

        /// <summary>Delegate for a SelectedCellChanged event.</summary>
        /// <param name="colIndex">The index of the column that was changed.</param>
        /// <param name="rowIndex">The index of the row that was changed.</param>
        public delegate void SelectedCellChangedDelegate(int colIndex, int rowIndex);

        /// <summary>An event invoked when a cell changes.</summary>
        public event SelectedCellChangedDelegate SelectedCellChanged;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to.</param>
        /// <param name="v">The view to connect to.</param>
        /// <param name="parentPresenter">The parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter parentPresenter)
        {
            //this allows a data provider to be passed in instead of a model.
            //For example, the datastore presenter uses this to fill out the table.
            ISheetDataProvider dataProvider = null;
            if (model as ISheetDataProvider != null)
            {
                dataProvider = model as ISheetDataProvider;
                gridTable = null;
            }
            //else we are receiving a model with IGridTable that we should get our GridTable from
            else if (model as IGridModel != null)
            {
                this.model = (model as Model);
                IGridModel m = (model as IGridModel);
                gridTable = m.Tables[0];
            }
            //else we are receiving a GridTable that was created by another presenter
            else if (model as GridTable != null)
            {
                gridTable = (model as GridTable);
            }

            else
            {
                throw new Exception($"Model {model.GetType()} passed to GridPresenter, does not inherit from GridTable.");
            }

            //we are receiving a container from another presenter to put the grid into
            if (v as ContainerView != null)
            {
                sheetContainer = v as ContainerView;
            }
            //we are receiving a GridView and should setup and load into that directly
            else if (v as IGridView != null)
            {
                IGridView view = v as IGridView;
                sheetContainer = view.Grid1;
                view.ShowGrid(1, true, parentPresenter.GetView() as ExplorerView);
                view.SetDescriptionText("");
                view.SetLabelHeight(0);
                AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All" });

                if (model as IGridModel != null)
                {
                    string text = (model as IGridModel).GetDescription();
                    if (text.Length > 0)
                    {
                        view.SetDescriptionText(text);
                        view.SetLabelHeight(0.1f);
                    }
                }
            }

            //Create the sheet widget here.
            SetupSheet(dataProvider);

            explorerPresenter = parentPresenter;
            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;

            //this is created with AddIntellisense by another presenter if intellisense is required
            intellisense = null;

            Refresh();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            contextMenuHelper.ContextMenu -= OnContextMenuPopup;

            if (grid.Sheet.DataProvider is DataTableProvider dataProvider)
                dataProvider.CellChanged -= OnCellChanged;

            SaveGridToModel();

            CleanupSheet();

            if (intellisense != null)
            {
                intellisense.Cleanup();
                intellisense.ItemSelected -= OnIntellisenseItemSelected;
                intellisense.ContextItemsNeeded -= OnIntellisenseNeedContextItems;
            }

        }

        public void SetupSheet(ISheetDataProvider dataProvider)
        {
            grid = new SheetWidget();
            grid.Sheet = new Sheet();
            grid.Sheet.DataProvider = dataProvider;
            grid.Sheet.CellSelector = new MultiCellSelect(grid.Sheet, grid);
            grid.Sheet.ScrollBars = new SheetScrollBars(grid.Sheet, grid);
            grid.Sheet.CellPainter = new DefaultCellPainter(grid.Sheet, grid);
            //we don't want an editor on grids that are linked to a dataProvider instead of a model
            if (dataProvider == null)
                grid.Sheet.CellEditor = new CellEditor(grid.Sheet, grid);

            if (gridTable != null)
            {
                if (gridTable.HasUnits())
                    grid.Sheet.NumberFrozenRows = 2;
                else
                    grid.Sheet.NumberFrozenRows = 1;
            }

            //Add the sheet's scrollbar widget to the view. (sheet sits within the scrollbar objects)
            sheetContainer.Add(grid.Sheet.ScrollBars.MainWidget);
            grid.Sheet.RedrawNeeded += OnRedraw;

            contextMenu = new MenuView();
            contextMenuHelper = new ContextMenuHelper(grid);
            contextMenuHelper.ContextMenu += OnContextMenuPopup;
            if (contextMenuOptions == null)
                contextMenuOptions = new List<string>(); //this will be populated by a presenter
        }

        /// <summary>
        /// Provide a new data provider to populate the table with.
        /// Used by presentors that are displaying data instead of editting a model. (eg DataStore)
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <param name="frozenColumns"></param>
        /// <param name="frozenRows"></param>
        public void PopulateWithDataProvider(ISheetDataProvider dataProvider, int frozenColumns, int frozenRows)
        {
            if (gridTable == null)
            {
                CleanupSheet();
                SetupSheet(dataProvider);
                grid.Sheet.ScrollBars.SetScrollbarAdjustments(dataProvider.ColumnCount, dataProvider.RowCount);
                grid.Sheet.NumberFrozenColumns = frozenColumns;
                grid.Sheet.NumberFrozenRows = frozenRows;
            }
            else
                throw new Exception($"PopulateWithDataProvider cannot be used on a presenter that has supplied a Model");
        }

        /// <summary>Refresh the grid.</summary>
        public void Refresh()
        {
            if (gridTable != null)
            {
                if (grid.Sheet.DataProvider != null)
                    (grid.Sheet.DataProvider as DataTableProvider).CellChanged -= OnCellChanged;

                DataTable data = gridTable.Data;

                // Assemble column units to pass to DataTableProvider constructor.
                List<string> units = null;
                if (gridTable.HasUnits())
                {
                    units = new List<string>();
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        units.Add(data.Rows[0].ItemArray[i].ToString());
                    }
                    data.Rows.Remove(data.Rows[0]);
                }

                // Assemble cell states (calculated cells) to pass to DataTableProvider constructor.
                List<List<bool>> isCalculated = new();
                for (int i = 0; i < data.Columns.Count; i++)
                   isCalculated.Add(gridTable.GetIsCalculated(i));

                // Create instance of DataTableProvider.
                DataTableProvider dataProvider = new DataTableProvider(data, units, isCalculated);

                // Give DataTableProvider to grid sheet.
                grid.Sheet.RowCount = grid.Sheet.NumberFrozenRows + data.Rows.Count + 1;
                grid.Sheet.DataProvider = dataProvider;

                dataProvider.CellChanged += OnCellChanged;
            }

            UpdateScrollBars();
        }

        public int NumRows()
        {
            var provider = grid.Sheet.DataProvider as DataTableProvider;
            return grid.Sheet.DataProvider.RowCount - grid.Sheet.NumberFrozenRows;
        }

        /// <summary>
        /// Adds options to the right-click context menu, valid options are:
        /// Cut - Any Editable Cell
        /// Copy - Any Cell
        /// Paste - Any Editable Cell
        /// Delete - Any Editable Cell
        /// Select All - Any Cell
        /// Units - Change Units on Solute grids, only on row == 1
        /// </summary>
        public void AddContextMenuOptions(string[] options)
        {
            if (contextMenuOptions == null)
                contextMenuOptions = new List<string>();
            foreach (string text in options)
            {
                string textLower = text.ToLower();
                if (textLower == "cut" || textLower == "copy" || textLower == "paste" || textLower == "delete" || textLower == "select all" || textLower == "units")
                    contextMenuOptions.Add(textLower);
                else
                    throw new Exception(text + " is not a valid context menu option for a Grid");
            }
        }

        /// <summary>
        /// Adds intellisense to the grid. 
        /// </summary>
        public void AddIntellisense(Model model)
        {
            this.model = model;
            intellisense = new IntellisensePresenter(sheetContainer as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;
            grid.Sheet.CellEditor.ShowIntellisense += OnIntellisenseNeedContextItems;
        }

        /// <summary>
        /// User has changed a cell.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="colIndices"></param>
        /// <param name="rowIndices"></param>
        /// <param name="values"></param>
        /// <param name="colIndex">The indices of the column that was changed.</param>
        /// <param name="rowIndex">The indices of the row that was changed.</param>
        private void OnCellChanged(ISheetDataProvider sender, int[] colIndices, int[] rowIndices, string[] values)
        {
            if (CellChanged != null)
            {
                try
                {
                    SaveGridToModel();
                    CellChanged?.Invoke(sender, colIndices, rowIndices, values);

                }
                catch (Exception err)
                {
                    explorerPresenter.MainPresenter.ShowError(err.ToString());
                }
            }
        }

        /// <summary>
        /// User has changed a cell.
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">event</param>
        private void OnRedraw(object sender, EventArgs e)
        {
            UpdateScrollBars();

            int row = 0;
            int column = 0;
            (sender as Sheet).CellSelector.GetSelection(out row, out column);

            if (selectedRow != row || selectedColumn != column)
            {
                selectedRow = row;
                selectedColumn = column;
                if (SelectedCellChanged != null)
                    SelectedCellChanged?.Invoke(selectedRow, selectedColumn);
            }
        }

        /// <summary>
        /// User has right clicked - display popup menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnContextMenuPopup(object sender, ContextMenuEventArgs e)
        {
            bool validCell = grid.Sheet.CellHitTest((int)e.X, (int)e.Y, out int columnIndex, out int rowIndex);

            if (validCell)
            {
                bool isReadOnly = false;
                if (columnIndex < grid.Sheet.NumberFrozenColumns)
                    isReadOnly = true;
                if (rowIndex < grid.Sheet.NumberFrozenRows)
                    isReadOnly = true;

                var menuItems = new List<MenuDescriptionArgs>();

                foreach (string option in contextMenuOptions)
                {
                    if (option.CompareTo("units") == 0) //used by solute grids to change units
                    {
                        if (rowIndex == 1)
                        {
                            foreach (string units in gridTable.GetUnits(columnIndex))
                            {
                                var menuItem = new MenuDescriptionArgs()
                                {
                                    Name = units,
                                };
                                menuItem.OnClick += (s, e) => { gridTable.SetUnits(columnIndex, menuItem.Name); SaveGridToModel(); Refresh(); };
                                menuItems.Add(menuItem);
                            }
                        }
                    }

                    bool showingUnits = false;
                    if (contextMenuOptions.Contains("units") && rowIndex == 1)
                        showingUnits = true;

                    if (!showingUnits)
                    {
                        if (option.CompareTo("cut") == 0 && !isReadOnly)
                        {
                            MenuDescriptionArgs menuItem = new MenuDescriptionArgs()
                            {
                                Name = "Cut",
                                ShortcutKey = "Ctrl+X"
                            };
                            menuItem.OnClick += OnCut;
                            menuItems.Add(menuItem);
                        }
                        if (option.CompareTo("copy") == 0)
                        {
                            MenuDescriptionArgs menuItem = new MenuDescriptionArgs()
                            {
                                Name = "Copy",
                                ShortcutKey = "Ctrl+C"
                            };
                            menuItem.OnClick += OnCopy;
                            menuItems.Add(menuItem);
                        }
                        if (option.CompareTo("paste") == 0 && !isReadOnly)
                        {
                            MenuDescriptionArgs menuItem = new MenuDescriptionArgs()
                            {
                                Name = "Paste",
                                ShortcutKey = "Ctrl+V"
                            };
                            menuItem.OnClick += OnPaste;
                            menuItems.Add(menuItem);
                        }
                        if (option.CompareTo("delete") == 0 && !isReadOnly)
                        {
                            MenuDescriptionArgs menuItem = new MenuDescriptionArgs()
                            {
                                Name = "Delete",
                                ShortcutKey = "Delete"
                            };
                            menuItem.OnClick += OnDelete;
                            menuItems.Add(menuItem);
                        }
                        if (option.CompareTo("select all") == 0)
                        {
                            if (grid.Sheet.CellSelector is MultiCellSelect)
                            {
                                MenuDescriptionArgs menuItem = new MenuDescriptionArgs()
                                {
                                    Name = "Select All",
                                    ShortcutKey = ""
                                };
                                menuItem.OnClick += OnSelectAll;
                                menuItems.Add(menuItem);
                            }
                        }
                    }
                }

                if (menuItems.Count > 0)
                {
                    contextMenu.Populate(menuItems);
                    contextMenu.Show();
                }
            }
        }

        /// <summary>
        /// User has selected cut.
        /// </summary>
        private void OnCut(object sender, EventArgs e)
        {
            grid.Sheet.CellSelector.Cut();
        }

        /// <summary>
        /// User has selected copy.
        /// </summary>
        private void OnCopy(object sender, EventArgs e)
        {
            grid.Sheet.CellSelector.Copy();
        }

        /// <summary>
        /// User has selected paste.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPaste(object sender, EventArgs e)
        {
            grid.Sheet.CellSelector.Paste();
        }

        /// <summary>
        /// User has selected cut.
        /// </summary>
        private void OnDelete(object sender, EventArgs e)
        {
            grid.Sheet.CellSelector.Delete();
        }

        /// <summary>
        /// User has selected cut.
        /// </summary>
        private void OnSelectAll(object sender, EventArgs e)
        {
            grid.Sheet.CellSelector.SelectAll();
        }

        /// <summary>Clean up the sheet components.</summary>
        private void CleanupSheet()
        {
            if (grid != null && grid.Sheet.CellSelector != null)
            {
                (grid.Sheet.CellSelector as SingleCellSelect).Cleanup();
                grid.Sheet.ScrollBars.Cleanup();
            }
        }

        /// <summary>
        /// The mode has changed (probably via undo/redo).
        /// </summary>
        /// <param name="changedModel">The model with changes</param>
        private void OnModelChanged(object changedModel)
        {
            Refresh();
        }

        /// <summary>Save the contents of the grid to the model.</summary>
        private void SaveGridToModel()
        {
            if (gridTable != null)
            {
                if (grid.Sheet.DataProvider != null)
                {
                    var data = (grid.Sheet.DataProvider as DataTableProvider).Data;
                    List<string> unitsRow = new List<string>();
                    for (int i = 0; i < data.Columns.Count; i++)
                        unitsRow.Add(grid.Sheet.DataProvider.GetColumnUnits(i));

                    DataRow row = data.NewRow();
                    row.ItemArray = unitsRow.ToArray();

                    data.Rows.InsertAt(row, 0);
                    explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(gridTable, "Data", data));
                }
            }
        }

        private void UpdateScrollBars()
        {
            int width = grid.Sheet.Width;
            int column_widths = 0;
            if (grid.Sheet.ColumnWidths != null && width > 0)
            {
                for (int i = 0; i < grid.Sheet.ColumnWidths.Length; i++)
                    column_widths += grid.Sheet.ColumnWidths[i];

                if (column_widths > width)
                    sheetContainer.SetScrollbarVisible(false, true);
                else
                    sheetContainer.SetScrollbarVisible(false, false);
            }
            else
            {
                sheetContainer.SetScrollbarVisible(false, false);
            }

            int height = grid.Sheet.Height;
            int row_heights = grid.Sheet.RowHeight * (grid.Sheet.RowCount + 1); //plus 1 for the empty row
            if (height > 0)
            {
                if (row_heights > height)
                    sheetContainer.SetScrollbarVisible(true, true);
                else
                    sheetContainer.SetScrollbarVisible(true, false);
            }
            else
            {
                sheetContainer.SetScrollbarVisible(true, false);
            }
        }


        /// <summary>
        /// Invoked when the user types a . into the editter.
        /// Inserts the selected item at the caret.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseNeedContextItems(object sender, NeedContextItemsArgs args)
        {
            try
            {
                if (intellisense.GenerateGridCompletions(args.Code, args.Code.Length, model, true, false, false, false))
                    intellisense.Show(args.Coordinates.X, args.Coordinates.Y);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user selects an item in the intellisense.
        /// Inserts the selected item at the caret.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            try
            {
                grid.Sheet.CellEditor.EndEdit();
                grid.Sheet.CellSelector.GetSelection(out int columnIndex, out int rowIndex);
                string text = grid.Sheet.DataProvider.GetCellContents(columnIndex, rowIndex);
                grid.Sheet.DataProvider.SetCellContents(new int[]{columnIndex}, 
                                                        new int[] {rowIndex}, 
                                                        new string[]{text + args.ItemSelected});
                grid.Sheet.CalculateBounds(columnIndex, rowIndex);

                grid.Sheet.CellEditor.Edit(); //keep editting window open
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }
    }

}