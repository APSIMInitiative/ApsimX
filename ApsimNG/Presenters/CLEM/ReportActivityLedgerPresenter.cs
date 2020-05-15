
namespace UserInterface.Presenters
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Factorial;
    using System;
    using System.Data;
    using EventArguments;
    using Interfaces;
    using Views;
    using Models.Storage;

    class ReportActivityLedgerPresenter : IPresenter
    {
        private Report report;
        private IReportActivityLedgerView view;
        private ExplorerPresenter explorerPresenter;
        private IDataStore dataStore;
        private DataStorePresenter dataStorePresenter;

        private ActivityLedgerGridPresenter activityGridPresenter;

        /// <summary>Attach the model (report) and the view (IReportView)</summary>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.report = model as Report;
            this.explorerPresenter = explorerPresenter;
            this.view = view as IReportActivityLedgerView;

            this.explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;

            Simulations simulations = Apsim.Parent(report, typeof(Simulations)) as Simulations;
            if (simulations != null)
            {
                dataStore = Apsim.Child(simulations, typeof(IDataStore)) as IDataStore;
            }

            dataStorePresenter = new DataStorePresenter();
            activityGridPresenter = new ActivityLedgerGridPresenter();
            Simulation simulation = Apsim.Parent(report, typeof(Simulation)) as Simulation;
            Zone paddock = Apsim.Parent(report, typeof(Zone)) as Zone;

            if (paddock != null)
                dataStorePresenter.ZoneFilter = paddock;
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

            dataStorePresenter.Attach(dataStore, this.view.DataStoreView, explorerPresenter);
            activityGridPresenter.ModelName = this.report.Name;
            activityGridPresenter.SimulationName = simulation.Name;
            activityGridPresenter.ZoneName = paddock.Name;
            activityGridPresenter.Attach(dataStore, this.view.DisplayView, explorerPresenter);
            dataStorePresenter.tableDropDown.SelectedValue = this.report.Name;
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            dataStorePresenter.Detach();
        }

        /// <summary>The view is asking for variable names.</summary>
        void OnNeedVariableNames(object sender, NeedContextItemsArgs e)
        {
            e.AllItems.AddRange(NeedContextItemsArgs.ExamineModelForNames(report, e.ObjectName, true, true, false));
        }

        /// <summary>The view is asking for event names.</summary>
        void OnNeedEventNames(object sender, NeedContextItemsArgs e)
        {
            e.AllItems.AddRange(NeedContextItemsArgs.ExamineModelForNames(report, e.ObjectName, false, false, true));
        }

        /// <summary>The variable names have changed in the view.</summary>
        void OnVariableNamesChanged(object sender, EventArgs e)
        {
            explorerPresenter.CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(OnModelChanged);
            explorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(OnModelChanged);
        }

        /// <summary>The event names have changed in the view.</summary>
        void OnEventNamesChanged(object sender, EventArgs e)
        {
            explorerPresenter.CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(OnModelChanged);
            explorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(OnModelChanged);
        }

        /// <summary>The model has changed so update our view.</summary>
        void OnModelChanged(object changedModel)
        {
        }
    }
}
