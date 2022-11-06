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
        /// Displays the pivoted table
        /// </summary>
        private GridView gridView;

        /// <summary>
        /// Displays the SQL
        /// </summary>
        private TextInputView sqlView;

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
                gridView = new GridView(clemPresenter.View as ViewBase);
                GridPresenter gridPresenter = new GridPresenter();

                // Create the SQL display
                sqlView = new TextInputView(clemPresenter.View as ViewBase);

                // Generate the table using the model
                pivot = clemPresenter.ClemModel as ReportPivot;
                gridPresenter.Attach(null, gridView, clemPresenter.ExplorerPresenter);

                // Attach the views to display data
                clem = clemPresenter.View as CLEMView;

                clem.AddTabView("Data", gridView);
                clemPresenter.PresenterList.Add("Data", this);

                clem.AddTabView("SQL", sqlView);
                clemPresenter.PresenterList.Add("SQL", this);
            }
            catch (Exception err)
            {
                clemPresenter.ExplorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <inheritdoc/>
        public void Detach()
        { }

        /// <inheritdoc/>
        public void Refresh()
        {
            gridView.DataSource = pivot.GenerateTable();
            sqlView.Text = pivot.SQL;
        }
    }
}
