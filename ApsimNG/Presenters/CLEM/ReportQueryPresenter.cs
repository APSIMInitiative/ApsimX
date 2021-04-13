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
        private class TextInputPresenter : IPresenter
        {
            public void Attach(object model, object view, ExplorerPresenter explorerPresenter){}
            public void Detach(){}
        }

        /// <summary>
        /// The GridView
        /// </summary>
        private TextInputView sqlView;

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

                // Create the SQL presenter
                sqlView = new TextInputView(clemPresenter.view as ViewBase);
                sqlView.Text = query.SQL;
                sqlView.WrapText = false;
                sqlView.Changed += TextChanged;

                var sqlPresenter = new TextInputPresenter();
                sqlPresenter.Attach(null, sqlView, clemPresenter.explorerPresenter);

                // Create the Data presenter
                gridView = new GridView(clemPresenter.view as ViewBase);

                var gridPresenter = new GridPresenter();
                gridPresenter.Attach(null, gridView, clemPresenter.explorerPresenter);

                // Attach the tabs
                clem.AddTabView("SQL", sqlView);
                clemPresenter.presenterList.Add("SQL", this);

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
        {
            sqlView.Changed -= TextChanged;
        }

        private void TextChanged(object sender, EventArgs e) => query.SQL = sqlView.Text;

        /// <inehritdoc/>
        public void Refresh()
        {
            query.SQL = sqlView.Text;
            gridView.DataSource = query.RunQuery();
        }
    }
}
