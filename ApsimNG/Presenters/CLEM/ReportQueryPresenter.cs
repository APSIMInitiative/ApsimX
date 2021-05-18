using Models.CLEM.Reporting;
using Models.Storage;
using System;
using UserInterface.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Combines a <see cref="PropertyPresenter"/> and <see cref="GridView"/> to customise and display
    /// a pivot table for a report
    /// </summary>
    class ReportQueryPresenter : IPresenter, ICLEMPresenter, IRefreshPresenter
    {
        /// <summary>
        /// The GridView
        /// </summary>
        private GridView gridView;

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
                clem = clemPresenter.view as CLEMView;
                query = clemPresenter.clemModel as ReportQuery;

                var store = query.FindInScope<IDataStore>();

                // Create the Data presenter
                gridView = new GridView(clemPresenter.view as ViewBase);

                var gridPresenter = new GridPresenter();
                gridPresenter.Attach(null, gridView, clemPresenter.explorerPresenter);

                // Attach the tab
                clem.AddTabView("Data", gridView);
                clemPresenter.presenterList.Add("Data", this);                
            }
            catch (Exception err)
            {
                clemPresenter.explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <inheritdoc/>
        public void Detach()
        { }

        /// <inehritdoc/>
        public void Refresh() => gridView.DataSource = query.RunQuery();
    }
}
