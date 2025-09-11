using Models;
using Models.Core;
using Models.Factorial;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Interfaces;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    class CLEMReportResultsPresenter : IPresenter, ICLEMPresenter, IRefreshPresenter
    {
        /// <summary>
        /// The data storage
        /// </summary>
        private IDataStore dataStore;

        /// <summary>
        /// The CLEM view
        /// </summary>
        private CLEMView clem;

        private ReportView rv = null;

        private DataStorePresenter dataStorePresenter = null;

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
                bool parentOfReport = false;
                Report report = clemPresenter.Model as Report;
                if(report is null)
                {
                    report = clemPresenter.Model.Node.FindChild<Report>();
                    parentOfReport = true;
                }

                rv = new ReportView(clemPresenter.View as ViewBase);
                ViewBase reportView = new ViewBase(rv, "ApsimNG.Resources.Glade.DataStoreView.glade");

                dataStorePresenter = new DataStorePresenter(new string[] { (parentOfReport)? (clemPresenter.Model as IModel).Name:report.Name });

                Simulations simulations = report.Node.FindParent<Simulations>(recurse: true);
                if (simulations != null)
                    dataStore = simulations.Node.FindChild<IDataStore>();

                Simulation simulation = report.Node.FindParent<Simulation>(recurse: true);
                Experiment experiment = report.Node.FindParent<Experiment>(recurse: true);
                Zone paddock = report.Node.FindParent<Zone>(recurse: true);

                IModel zoneAnscestor = report.Node.FindParent<Zone>(recurse: true);

                // Only show data which is in scope of this report.
                // E.g. data from this zone and either experiment (if applicable) or simulation.
                if (paddock != null)
                    dataStorePresenter.ZoneFilter = paddock;
                if (zoneAnscestor is null & experiment != null)
                    // allows the inner reports of the base simulation to be displayed
                    // when an experiment is being undertaken
                    // otherwise reports are considered child of experiment and will only display experiment results.
                    dataStorePresenter.ExperimentFilter = experiment;
                else if (simulation != null)
                    dataStorePresenter.SimulationFilter = simulation;

                dataStorePresenter.Attach(dataStore, reportView, clemPresenter.ExplorerPresenter);

                // Attach the view to display data
                clem = clemPresenter.View as CLEMView;
                clem.AddTabView("Data", reportView);
                clemPresenter.PresenterList.Add("Data", this);
            }
            catch (Exception err)
            {
                clemPresenter.ExplorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <inheritdoc/>
        public void Detach()
        {
            dataStorePresenter?.Detach();
            rv?.Dispose();
            clem?.Dispose();
        }

        /// <inheritdoc/>
        public void Refresh() { }
    }
}
