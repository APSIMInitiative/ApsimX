namespace UserInterface.Presenters
{
    using EventArguments;
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
        SingleCellSelect cellSelector;

        ///// <summary>The sheet scrollbars</summary>
        //SheetScrollBars scrollbars;

        /// <summary>The data provider for the sheet</summary>
        DataTableProvider dataProvider;

        /// <summary>The container that houses the sheet.</summary>
        private ContainerView sheetContainer;

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
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
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

                dataProvider = new DataTableProvider(modelWithData.GetData());

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

        /// <summary>Clean up the sheet components.</summary>
        private void CleanupSheet()
        {
            if (cellSelector != null)
            {
                cellSelector.Cleanup();
                //scrollbars.Cleanup();
            }
        }
    }
}