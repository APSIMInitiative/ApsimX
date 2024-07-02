using System;
using System.Collections.Generic;
using System.Data;
using Gtk.Sheet;
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

        /// <summary>The data provider.</summary>
        private ISheetDataProvider dataProvider;

        // /// <summary>Currently Selected Row. Used to detect when a cell is slected</summary>
        // private int selectedRow = -1;

        // /// <summary>Currently Selected Column. Used to detect when a cell is slected</summary>
        // private int selectedColumn = -1;

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

        ///// <summary>An event invoked when a cell changes.</summary>
        //public event SelectedCellChangedDelegate SelectedCellChanged;

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

            if (dataProvider is DataTableProvider)
                dataProvider.CellChanged -= OnCellChanged;

            SaveGridToModel();

            grid.Cleanup();

            if (intellisense != null)
            {
                intellisense.Cleanup();
                //intellisense.ItemSelected -= OnIntellisenseItemSelected;
                intellisense.ContextItemsNeeded -= OnIntellisenseNeedContextItems;
            }

        }

        public void SetupSheet(ISheetDataProvider dataProvider)
        {
            grid = new SheetWidget(sheetContainer.Widget,  
                                   dataProvider, 
                                   multiSelect: true,
                                   onException: (err) => ViewBase.MasterView.ShowError(err));

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
                grid.Cleanup();
                SetupSheet(dataProvider);
            }
            else
                throw new Exception($"PopulateWithDataProvider cannot be used on a presenter that has supplied a Model");
        }

        /// <summary>Refresh the grid.</summary>
        public void Refresh()
        {
            if (gridTable != null && grid != null)
            {
                if (dataProvider != null)
                    (dataProvider as DataTableProvider).CellChanged -= OnCellChanged;

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

                if (data != null)
                {
                    for (int i = 0; i < data.Columns.Count; i++)
                    isCalculated.Add(gridTable.GetIsCalculated(i));

                    // Create instance of DataTableProvider.
                    dataProvider = new DataTableProvider(data, isReadOnly: false, units, isCalculated);
                
                    // Give DataTableProvider to grid sheet.
                    grid.SetDataProvider(dataProvider);

                    // Add an extra empty row to the grid so that new rows can be created.
                    grid.RowCount = grid.NumberFrozenRows + data.Rows.Count + 1;

                    dataProvider.CellChanged += OnCellChanged;
                }
            }

            grid?.UpdateScrollBars();
        }

        /// <summary>The number of rows of data in the grid.</summary>
        public int RowCount()
        {
            return grid.RowCount - grid.NumberFrozenRows;
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
            // this.model = model;
            // intellisense = new IntellisensePresenter(sheetContainer as ViewBase);
            // intellisense.ItemSelected += OnIntellisenseItemSelected;
            //grid.Sheet.CellEditor.ShowIntellisense += OnIntellisenseNeedContextItems;
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
        /// User has right clicked - display popup menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnContextMenuPopup(object sender, ContextMenuEventArgs e)
        {
            bool validCell = grid.CellHitTest((int)e.X, (int)e.Y, out int columnIndex, out int rowIndex);

            if (validCell)
            {
                bool isReadOnly = grid.IsCellReadOnly(columnIndex, rowIndex);
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
            grid.Cut();
        }

        /// <summary>
        /// User has selected copy.
        /// </summary>
        private void OnCopy(object sender, EventArgs e)
        {
            grid.Copy();
        }

        /// <summary>
        /// User has selected paste.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPaste(object sender, EventArgs e)
        {
            grid.Paste();
        }

        /// <summary>
        /// User has selected cut.
        /// </summary>
        private void OnDelete(object sender, EventArgs e)
        {
            grid.Delete();
        }

        /// <summary>
        /// User has selected cut.
        /// </summary>
        private void OnSelectAll(object sender, EventArgs e)
        {
            grid.SelectAll();
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
                if (dataProvider != null)
                {
                    var data = (dataProvider as DataTableProvider).Data;
                    List<string> unitsRow = new List<string>();
                    for (int i = 0; i < data.Columns.Count; i++)
                        unitsRow.Add(dataProvider.GetColumnUnits(i));

                    DataRow row = data.NewRow();
                    row.ItemArray = unitsRow.ToArray();

                    data.Rows.InsertAt(row, 0);
                    explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(gridTable, "Data", data));
                }
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

        // /// <summary>
        // /// Invoked when the user selects an item in the intellisense.
        // /// Inserts the selected item at the caret.
        // /// </summary>
        // /// <param name="sender">Sender object.</param>
        // /// <param name="args">Event arguments.</param>
        // private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        // {
        //     try
        //     {
        //         grid.Sheet.CellEditor.EndEdit();
        //         grid.Sheet.CellSelector.GetSelection(out int columnIndex, out int rowIndex);
        //         string text = grid.Sheet.DataProvider.GetCellContents(columnIndex, rowIndex);
        //         grid.Sheet.DataProvider.SetCellContents(new int[]{columnIndex}, 
        //                                                 new int[] {rowIndex}, 
        //                                                 new string[]{text + args.ItemSelected});
        //         grid.Sheet.CalculateBounds(columnIndex, rowIndex);

        //         grid.Sheet.CellEditor.Edit(); //keep editting window open
        //     }
        //     catch (Exception err)
        //     {
        //         explorerPresenter.MainPresenter.ShowError(err);
        //     }
        // }
    }

}