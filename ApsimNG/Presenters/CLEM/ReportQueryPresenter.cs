﻿using Gtk;
using Models.CLEM.Reporting;
using Models.Storage;
using System;
using System.Data;
using UserInterface.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Combines a <see cref="PropertyPresenter"/> and <see cref="SheetWidget"/> to customise and display
    /// a pivot table for a report
    /// </summary>
    class ReportQueryPresenter : IPresenter, ICLEMPresenter, IRefreshPresenter
    {
        /// <summary>
        /// Displays the pivoted table
        /// </summary>
        private SheetWidget grid;

        /// <summary>
        /// Displays the pivoted table
        /// </summary>
        private ContainerView container;

        /// <summary>
        /// The pivot model
        /// </summary>
        private ReportQuery query;

        /// <summary>
        /// The CLEM view
        /// </summary>
        private CLEMView clem;

        /// <summary>
        /// Attach the model and view to the presenter
        /// </summary>
        /// <param name="model">The model to attach</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The presenter to attach to</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            // This code is not reached, the usual functionality is performed in            
            // the CLEMPresenter.AttachExtraPresenters() method
        }

        /// <inheritdoc/>
        public void AttachExtraPresenters(CLEMPresenter clemPresenter)
        {
            try
            {
                // Create the grid to display data in
                container = new ContainerView(clemPresenter.View as ViewBase);
                grid = new SheetWidget();
                grid.Sheet = new Sheet();
                grid.Sheet.DataProvider = new DataTableProvider(new DataTable());
                grid.Sheet.CellSelector = new MultiCellSelect(grid.Sheet, grid);
                grid.Sheet.ScrollBars = new SheetScrollBars(grid.Sheet, grid);
                grid.Sheet.CellPainter = new DefaultCellPainter(grid.Sheet, grid);
                container.Add(grid.Sheet.ScrollBars.MainWidget);

                clem = clemPresenter.View as CLEMView;
                query = clemPresenter.ClemModel as ReportQuery;

                var store = query.FindInScope<IDataStore>();

                // Attach the tab
                clem.AddTabView("Data", container);
                clemPresenter.PresenterList.Add("Data", this);                
            }
            catch (Exception err)
            {
                clemPresenter.ExplorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <inheritdoc/>
        public void Detach()
        { }

        /// <inehritdoc/>
        public void Refresh() {
            grid.Sheet.DataProvider = new DataTableProvider(query.RunQuery());
        }
    }
}
