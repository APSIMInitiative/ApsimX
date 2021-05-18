using Models.CLEM.Reporting;
using System;
using UserInterface.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Combines a <see cref="PropertyPresenter"/> and <see cref="GridView"/> to customise and display
    /// a pivot table for a report
    /// </summary>
    class ReportPivotPresenter : IPresenter, ICLEMPresenter, IRefreshPresenter
    {
        /// <summary>
        /// The GridView
        /// </summary>
        private GridView gridView;

        /// <summary>
        /// The pivot model
        /// </summary>
        private ReportPivot pivot;

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
                gridView = new GridView(clemPresenter.view as ViewBase);
                GridPresenter gridPresenter = new GridPresenter();

                // Generate the table using the model
                pivot = clemPresenter.clemModel as ReportPivot;
                gridView.DataSource = pivot.GenerateTable();
                gridPresenter.Attach(null, gridView, clemPresenter.explorerPresenter);

                // Attach the view to display data

                clem = clemPresenter.view as CLEMView;
                clem.AddTabView("Data", gridView);
                clemPresenter.presenterList.Add("Data", this);

                //clem.TabSelected += Refresh;
            }
            catch (Exception err)
            {
                clemPresenter.explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <inheritdoc/>
        public void Detach()
        { }

        /// <inheritdoc/>
        public void Refresh() => gridView.DataSource = pivot.GenerateTable();
    }
}
