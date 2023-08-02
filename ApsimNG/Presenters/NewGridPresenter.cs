namespace UserInterface.Presenters
{
    using DocumentFormat.OpenXml.Drawing.Charts;
    using EventArguments;
    using global::UserInterface.Interfaces;
    using Models.Core;
    using Models.Interfaces;
    using Models.Soils;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Views;

    /// <summary>A generic grid presenter for displaying tabular data and allowing editing.</summary>
    public class NewGridPresenter : IPresenter
    {
        /// <summary>The data store model to work with.</summary>
        private TabularData tabularData;

        /// <summary>The sheet widget.</summary>
        private SheetWidget grid;

        /// <summary>The container that houses the sheet.</summary>
        private ContainerView sheetContainer;

        /// <summary>The popup context menu helper.</summary>
        private ContextMenuHelper contextMenuHelper;

        /// <summary>The popup context menu.</summary>
        private MenuView contextMenu;

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        private ViewBase view = null;

        /// <summary>Delegate for a CellChanged event.</summary>
        /// <param name="dataProvider">The data provider.</param>
        /// <param name="colIndex">The index of the column that was changed.</param>
        /// <param name="rowIndex">The index of the row that was changed.</param>
        public delegate void CellChangedDelegate(ISheetDataProvider dataProvider, int colIndex, int rowIndex);

        /// <summary>An event invoked when a cell changes.</summary>
        public event CellChangedDelegate CellChanged;

        /// <summary>Default constructor</summary>
        public NewGridPresenter()
        {
        }
        
        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The data store model to work with.</param>
        /// <param name="v">Data store view to work with.</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter explorerPresenter)
        {
            try
            {
                tabularData = (model as ITabularData).GetTabularData();
            }
            catch (Exception ex)
            {
                if (ex.Data["tableData"] != null)
                {
                    tabularData = ex.Data["tableData"] as TabularData;
                    explorerPresenter.MainPresenter.ShowMsgDialog(ex.Message, "Warning", Gtk.MessageType.Warning, Gtk.ButtonsType.Ok);
                }
                else
                {
                    throw new Exception(ex.Message, ex);
                }
            }
           
            view = v as ViewBase;
            this.explorerPresenter = explorerPresenter;

            sheetContainer = view.GetControl<ContainerView>("grid");
            Populate();

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
            view.Dispose();
            CleanupSheet();
        }

        /// <summary>Populate the grid control with data.</summary>
        private void Populate()
        {
            // Create sheet control
            try
            {
                // Cleanup existing sheet instances before creating new ones.
                CleanupSheet();

                var dataProvider = new DataTableProvider(tabularData.Data);

                grid = new SheetWidget();
                grid.Sheet = new Sheet();
                grid.Sheet.NumberFrozenRows = 2;
                grid.Sheet.NumberFrozenColumns = 1;
                grid.Sheet.RowCount = 50;
                grid.Sheet.DataProvider = dataProvider;
                grid.Sheet.CellSelector = new MultiCellSelect(grid.Sheet, grid);
                grid.Sheet.CellEditor = new CellEditor(grid.Sheet, grid);
                grid.Sheet.ScrollBars = new SheetScrollBars(grid.Sheet, grid);
                grid.Sheet.CellPainter = new DefaultCellPainter(grid.Sheet, grid);
                sheetContainer.Add(grid.Sheet.ScrollBars.MainWidget);

                dataProvider.CellChanged += OnCellChanged;
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err.ToString());
            }
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
                    foreach (string units in tabularData.GetUnits(columnIndex))
                    {
                        var menuItem = new MenuDescriptionArgs()
                        {
                            Name = units,
                        };
                        menuItem.OnClick += (s, e) => { tabularData.SetUnits(columnIndex, menuItem.Name); SaveGridToModel(); Refresh(); };
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
            if (grid.Sheet.DataProvider is DataTableProvider dataProvider)
                dataProvider.CellChanged -= OnCellChanged;

            dataProvider = new DataTableProvider(tabularData.Data);
            grid.Sheet.DataProvider = dataProvider;
            grid.Sheet.Refresh();

            dataProvider.CellChanged += OnCellChanged;
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
            var dataTableProvider = grid.Sheet.DataProvider as DataTableProvider;
            if (dataTableProvider != null)
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(tabularData, "Data", dataTableProvider.Data));
        }
    }
}