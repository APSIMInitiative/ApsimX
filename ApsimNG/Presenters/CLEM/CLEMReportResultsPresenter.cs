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
        ///// <summary>
        ///// The GridView
        ///// </summary>
        //private GridView gridView;

        /// <summary>
        /// The data storage
        /// </summary>
        private IDataStore dataStore;

        ///// <summary>
        ///// The pivot model
        ///// </summary>
        //private ReportPivot pivot;

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
                Report report = clemPresenter.model as Report;

                ReportView rv = new ReportView(clemPresenter.view as ViewBase);
                ViewBase reportView = new ViewBase(rv, "ApsimNG.Resources.Glade.DataStoreView.glade");

                DataStorePresenter dataStorePresenter = new DataStorePresenter(new string[] { report.Name });

                Simulations simulations = report.FindAncestor<Simulations>();
                if (simulations != null)
                {
                    dataStore = simulations.FindChild<IDataStore>();
                }

                Simulation simulation = report.FindAncestor<Simulation>();
                Experiment experiment = report.FindAncestor<Experiment>();
                Zone paddock = report.FindAncestor<Zone>();

                // Only show data which is in scope of this report.
                // E.g. data from this zone and either experiment (if applicable) or simulation.
                if (paddock != null)
                {
                    dataStorePresenter.ZoneFilter = paddock;
                }

                if (experiment != null)
                {
                    dataStorePresenter.ExperimentFilter = experiment;
                }
                else if (simulation != null)
                {
                    dataStorePresenter.SimulationFilter = simulation;
                }

                dataStorePresenter.Attach(dataStore, reportView, clemPresenter.explorerPresenter);

                // Attach the view to display data
                clem = clemPresenter.view as CLEMView;
                clem.AddTabView("Data", reportView);
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
        public void Refresh() { } // => gridView.DataSource = pivot.GenerateTable();
    }
}
