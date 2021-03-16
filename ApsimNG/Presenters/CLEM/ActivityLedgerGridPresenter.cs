namespace UserInterface.Presenters
{
    using global::UserInterface.Interfaces;
    using Models;
    using Models.CLEM.Reporting;
    using Models.Core;
    using Models.Factorial;
    using Models.Storage;
    using System;
    using System.IO;
    using Views;

    /// <summary>A data store presenter connecting a data store model with a data store view</summary>
    public class ActivityLedgerGridPresenter : IPresenter, IRefreshPresenter, ICLEMPresenter
    {
        /// <summary>The data store model to work with.</summary>
        private IDataStore dataStore;

        /// <summary>The display grid store view to work with.</summary>
        public IActivityLedgerGridView Grid { get; set; }

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        public string ModelName { get; set; }

        public Report ModelReport { get; set; }

        /// <summary>
        /// The name of the simulation to display
        /// </summary>
        public string SimulationName { get; set; }

        /// <summary>
        /// The name of the simulation to display
        /// </summary>
        public string ZoneName { get; set; }

        private bool CreateHtml = false;

        /// <summary>
        /// Attach inherited class additional presenters is needed
        /// </summary>
        public void AttachExtraPresenters(CLEMPresenter clemPresenter)
        {
            //UI Results
            try
            {
                ActivityLedgerGridView ledgerView = new ActivityLedgerGridView(clemPresenter.view as ViewBase);
                ReportView rv = new ReportView(clemPresenter.view as ViewBase);
                ViewBase reportView = new ViewBase(rv, "ApsimNG.Resources.Glade.DataStoreView.glade");

                Model report = clemPresenter.model as Model;

                Simulations simulations = report.FindAncestor<Simulations>();
                if (simulations != null)
                {
                    dataStore = simulations.FindChild<IDataStore>();
                }

                DataStorePresenter dataStorePresenter = new DataStorePresenter();
                ActivityLedgerGridPresenter activityGridPresenter = new ActivityLedgerGridPresenter();
                Simulation simulation = report.FindAncestor<Simulation>();
                Zone paddock = report.FindAncestor<Zone>();

                if (paddock != null)
                {
                    dataStorePresenter.ZoneFilter = paddock;
                }
                if (simulation != null)
                {
                    if (simulation.Parent is Experiment)
                    {
                        dataStorePresenter.ExperimentFilter = simulation.Parent as Experiment;
                    }
                    else
                    {
                        dataStorePresenter.SimulationFilter = simulation;
                    }
                }

                dataStorePresenter.Attach(dataStore, reportView, clemPresenter.explorerPresenter);
                activityGridPresenter.CreateHtml = (clemPresenter.model as ReportActivitiesPerformed).CreateHTML;
                activityGridPresenter.ModelReport = report as Report;
                activityGridPresenter.ModelName = report.Name;
                activityGridPresenter.SimulationName = simulation.Name;
                activityGridPresenter.ZoneName = paddock.Name;
                activityGridPresenter.Attach(dataStore, ledgerView, clemPresenter.explorerPresenter);
                dataStorePresenter.tableDropDown.SelectedValue = report.Name;

                (clemPresenter.view as CLEMView).AddTabView("Display", ledgerView);
                clemPresenter.presenterList.Add("Display", activityGridPresenter);

                (clemPresenter.view as CLEMView).AddTabView("Data", reportView);
                clemPresenter.presenterList.Add("Data", dataStorePresenter);
            }
            catch (Exception err)
            {
                this.explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The data store model to work with.</param>
        /// <param name="view">Data store view to work with.</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            dataStore = model as IDataStore;
            this.Grid = view as ActivityLedgerGridView;
            this.explorerPresenter = explorerPresenter;
            this.Grid.ReadOnly = true;

            // save the html version as soon as this report is selected
            // do not create the UI grid version until the user selectes the Display tab
            if (CreateHtml)
            {
                (ModelReport as ReportActivitiesPerformed).CreateDataTable(dataStore, Path.GetDirectoryName(this.explorerPresenter.ApsimXFile.FileName), Utility.Configuration.Settings.DarkTheme);
            }
        }

        public void Refresh()
        {
            // now get report model to create data as we need to generate the HTML report independent of ApsimNG
            Grid.DataSource = (ModelReport as ReportActivitiesPerformed).CreateDataTable(dataStore, Path.GetDirectoryName(this.explorerPresenter.ApsimXFile.FileName), Utility.Configuration.Settings.DarkTheme);
            this.Grid.LockLeftMostColumns(1);  // lock activity name.
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
        }

        /// <summary>The selected table has changed.</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnTableSelected(object sender, EventArgs e)
        {
            Refresh();
        }

        /// <summary>The column filter has changed.</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnColumnFilterChanged(object sender, EventArgs e)
        {
            Refresh();
        }
    }
}
