namespace UserInterface.Presenters
{
    using EventArguments;
    using global::UserInterface.Interfaces;
    using Models.Core;
    using Models.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Views;

    /// <summary>A generic grid presenter for displaying tabular data and allowing editing.</summary>
    public class NewGridPresenter : IPresenter
    {
        /// <summary>The data store model to work with.</summary>
        private ITabularData modelWithData;

        /// <summary>The sheet.</summary>
        private Sheet sheet;

        /// <summary>The sheet widget.</summary>
        private SheetWidget sheetWidget;

        /// <summary>The sheet cell selector.</summary>
        private SingleCellSelect cellSelector;

        ///// <summary>The sheet scrollbars</summary>
        //SheetScrollBars scrollbars;

        /// <summary>The data provider for the sheet</summary>
        private DataTable data;

        /// <summary>The container that houses the sheet.</summary>
        private ContainerView sheetContainer;

        /// <summary>The popup context menu helper.</summary>
        private ContextMenuHelper contextMenuHelper;

        /// <summary>The popup context menu.</summary>
        private MenuView contextMenu;

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        private ViewBase view = null;

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
            modelWithData = model as ITabularData;
            view = v as ViewBase;
            this.explorerPresenter = explorerPresenter;

            sheetContainer = view.GetControl<ContainerView>("grid");
            PopulateGrid();

            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
            contextMenuHelper = new ContextMenuHelper(sheetWidget);
            contextMenu = new MenuView();
            contextMenuHelper.ContextMenu += OnContextMenuPopup;
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            contextMenuHelper.ContextMenu -= OnContextMenuPopup;

            explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(modelWithData, "TabularData", data));

            //base.Detach();
            view.Dispose();
            CleanupSheet();
        }

        /// <summary>Populate the grid control with data.</summary>
        public void PopulateGrid()
        {
            // Create sheet control
            try
            {
                // Cleanup existing sheet instances before creating new ones.
                CleanupSheet();

                data = modelWithData.TabularData;
                var dataProvider = new DataTableProvider(data);

                sheet = new Sheet()
                {
                    DataProvider = dataProvider,
                    NumberFrozenRows = 2,
                    NumberFrozenColumns = 1,
                    RowCount = 50
                };
                sheetWidget = new SheetWidget(sheet);
                cellSelector = new SingleCellSelect(sheet, sheetWidget);
                var sheetEditor = new SheetEditor(sheet, sheetWidget)
                {
                    Selection = cellSelector
                };

                cellSelector.Editor = sheetEditor;
                var scrollbars = new SheetScrollBars(sheet, sheetWidget);
                sheet.CellPainter = new DefaultCellPainter(sheet, sheetWidget, sheetEditor, sheetSelection: cellSelector);
                sheetContainer.Add(scrollbars.MainWidget);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err.ToString());
            }
        }

        /// <summary>
        /// User has right clicked - display popup menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnContextMenuPopup(object sender, ContextMenuEventArgs e)
        {
            if (sheet.CellHitTest((int)e.X, (int)e.Y, out int columnIndex, out int rowIndex))
            {
                if (rowIndex == 1)
                {
                    var menuItems = new List<MenuDescriptionArgs>();

                    foreach (string units in modelWithData.GetUnits(columnIndex))
                    {
                        var menuItem = new MenuDescriptionArgs()
                        {
                            Name = units,
                        };
                        //menuItem.OnClick += OnContextMenuItemClick;
                        menuItem.OnClick += (s, e) => { modelWithData.SetUnits(columnIndex, menuItem.Name); Refresh(); };
                        menuItems.Add(menuItem);
                    }
                    if (menuItems.Count > 0)
                    {
                        contextMenu.Populate(menuItems);
                        contextMenu.Show();
                    }
                }
            }
        }

        /// <summary>Refresh the grid.</summary>
        private void Refresh()
        {
            data = modelWithData.TabularData;
            sheet.DataProvider = new DataTableProvider(data);
            sheet.Refresh();
        }

        /// <summary>
        /// User has clicked a menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnContextMenuItemClick(object sender, EventArgs e)
        {
            //var menuItem = sender as menuItem;
            //modelWithData.SetUnits()
        }

        /// <summary>Clean up the sheet components.</summary>
        private void CleanupSheet()
        {
            if (cellSelector != null)
            {
                cellSelector.Cleanup();
                //scrollbars.Cleanup();
            }
        }

        /// <summary>
        /// The mode has changed (probably via undo/redo).
        /// </summary>
        /// <param name="changedModel">The model with changes</param>
        private void OnModelChanged(object changedModel)
        {
            this.PopulateGrid();
        }
    }
}