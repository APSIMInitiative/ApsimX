using UserInterface.Interfaces;
using Models.Utilities;
using System;
using System.Collections.Generic;
using UserInterface.Views;
using System.Data;

namespace UserInterface.Presenters
{
    /// <summary>A generic grid presenter for displaying a data table and allowing editing.
    /// This is built to run within other presenters, so pass in a ContainerView in the attach method to have it drawn.</summary>
    class NewGridPresenter : IPresenter
    {
        /// <summary>The data store model to work with.</summary>
        private GridTable gridTable;

        /// <summary>The sheet widget.</summary>
        private SheetWidget grid;

        /// <summary></summary>
        private int selectedRow = -1;

        /// <summary></summary>
        private int selectedColumn = -1;

        /// <summary>The container that houses the sheet.</summary>
        private ContainerView sheetContainer;

        /// <summary>The popup context menu helper.</summary>
        private ContextMenuHelper contextMenuHelper;

        /// <summary>The popup context menu.</summary>
        private MenuView contextMenu;

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Delegate for a CellChanged event.</summary>
        /// <param name="dataProvider">The data provider.</param>
        /// <param name="colIndex">The index of the column that was changed.</param>
        /// <param name="rowIndex">The index of the row that was changed.</param>
        public delegate void CellChangedDelegate(ISheetDataProvider dataProvider, int colIndex, int rowIndex);

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
            try
            {
                gridTable = (model as GridTable);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

            if (v as ContainerView != null)
            {
                sheetContainer = v as ContainerView;
            }
            else
            {
                sheetContainer = new ContainerView(v as ViewBase);
            }
            
            this.explorerPresenter = parentPresenter;

            grid = new SheetWidget();
            grid.Sheet = new Sheet();

            grid.Sheet.DataProvider = null;
            grid.Sheet.CellSelector = new MultiCellSelect(grid.Sheet, grid);
            grid.Sheet.CellEditor = new CellEditor(grid.Sheet, grid);
            grid.Sheet.ScrollBars = new SheetScrollBars(grid.Sheet, grid);
            grid.Sheet.CellPainter = new DefaultCellPainter(grid.Sheet, grid);

            if (gridTable.HasUnits())
                grid.Sheet.NumberFrozenRows = 2;
            else
                grid.Sheet.NumberFrozenRows = 1;

            sheetContainer.Add(grid.Sheet.ScrollBars.MainWidget);
            grid.Sheet.RedrawNeeded += OnRedraw;

            Refresh();

            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
            contextMenuHelper = new ContextMenuHelper(grid);
            contextMenu = new MenuView();
            contextMenuHelper.ContextMenu += OnContextMenuPopup;
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            contextMenuHelper.ContextMenu -= OnContextMenuPopup;

            if (grid.Sheet.DataProvider is DataTableProvider dataProvider)
                dataProvider.CellChanged -= OnCellChanged;

            SaveGridToModel();

            //base.Detach();
            CleanupSheet();
        }

        /// <summary>
        /// User has changed a cell.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="colIndex">The index of the column that was changed.</param>
        /// <param name="rowIndex">The index of the row that was changed.</param>
        private void OnCellChanged(ISheetDataProvider sender, int colIndex, int rowIndex)
        {
            if (CellChanged != null)
            {
                try
                {
                    SaveGridToModel();
                    CellChanged.Invoke(sender, colIndex, rowIndex);
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

            if (selectedRow != row || selectedColumn != column) {
                selectedRow = row;
                selectedColumn = column;
                if (SelectedCellChanged != null) 
                    SelectedCellChanged.Invoke(selectedRow, selectedColumn);
            }
        }

        /// <summary>
        /// User has right clicked - display popup menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnContextMenuPopup(object sender, ContextMenuEventArgs e)
        {
            if (grid.Sheet.CellHitTest((int)e.X, (int)e.Y, out int columnIndex, out int rowIndex))
            {
                var menuItems = new List<MenuDescriptionArgs>();
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
                else
                {
                    var menuItem = new MenuDescriptionArgs()
                    {
                        Name = "Copy",
                        ShortcutKey = "Ctrl+C"
                    };
                    menuItem.OnClick += OnCopy;
                    menuItems.Add(menuItem);
                    menuItem = new MenuDescriptionArgs()
                    {
                        Name = "Paste",
                        ShortcutKey = "Ctrl+V"
                    };
                    menuItem.OnClick += OnPaste;
                    menuItems.Add(menuItem);
                }

                if (menuItems.Count > 0)
                {
                    contextMenu.Populate(menuItems);
                    contextMenu.Show();
                }
            }
        }

        /// <summary>
        /// User has selected paste.
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

        /// <summary>Refresh the grid.</summary>
        public void Refresh()
        {
            if (grid.Sheet.DataProvider != null)
                (grid.Sheet.DataProvider as DataTableProvider).CellChanged -= OnCellChanged;

            DataTable data = gridTable.Data;

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
            DataTableProvider dataProvider = new DataTableProvider(data, units);

            grid.Sheet.RowCount = grid.Sheet.NumberFrozenRows + data.Rows.Count + 1;
            grid.Sheet.DataProvider = dataProvider;

            dataProvider.CellChanged += OnCellChanged;

            UpdateScrollBars();
        }

        public int NumRows()
        {
            var provider = grid.Sheet.DataProvider as DataTableProvider;
            return grid.Sheet.DataProvider.RowCount - grid.Sheet.NumberFrozenRows;
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
            if (grid.Sheet.DataProvider != null)
            {
                var data = (grid.Sheet.DataProvider as DataTableProvider).Data;
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(gridTable, "Data", data));
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
            int row_heights = grid.Sheet.RowHeight * grid.Sheet.RowCount;
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
    }
}