using APSIM.Shared.Graphing;
using Atk;
using Gtk;
using Gtk.Sheet;
using Models.CLEM.Reporting;
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
    class ReportPivotPresenter : IPresenter, ICLEMPresenter, IRefreshPresenter
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
                container = new ContainerView(clemPresenter.View as ViewBase);
                grid = new SheetWidget(container.Widget,
                                       dataProvider: new DataTableProvider(new DataTable(), isReadOnly: true),
                                       multiSelect: true,
                                       onException: (err) => ViewBase.MasterView.ShowError(err));

                // Create the SQL display
                sqlView = new TextInputView(clemPresenter.View as ViewBase);

                // Generate the table using the model
                pivot = clemPresenter.ClemModel as ReportPivot;

                // Attach the views to display data
                clem = clemPresenter.View as CLEMView;

                clem.AddTabView("Data", container);
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
            grid.SetDataProvider(new DataTableProvider(pivot.GenerateTable(), isReadOnly: true));
            sqlView.Text = pivot.SQL;
        }
    }
}
