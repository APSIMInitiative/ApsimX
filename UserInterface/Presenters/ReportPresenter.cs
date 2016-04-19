// -----------------------------------------------------------------------
// <copyright file="ReportPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Factorial;
    using Models.Report;
    using System;
    using System.Data;
    using EventArguments;
    using Interfaces;
    using Views;

    class ReportPresenter : IPresenter
    {
        private Report report;
        private IReportView view;
        private ExplorerPresenter explorerPresenter;
        private DataStore dataStore;
        private DataStorePresenter dataStorePresenter;

        /// <summary>Attach the model (report) and the view (IReportView)</summary>
        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            this.report = Model as Report;
            this.explorerPresenter = explorerPresenter;
            this.view = View as IReportView;

            this.view.VariableList.Lines = report.VariableNames;
            this.view.EventList.Lines = report.EventNames;
            this.view.VariableList.ContextItemsNeeded += OnNeedVariableNames;
            this.view.EventList.ContextItemsNeeded += OnNeedEventNames;
            this.view.VariableList.TextHasChangedByUser += OnVariableNamesChanged;
            this.view.EventList.TextHasChangedByUser += OnEventNamesChanged;
            this.explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;

            Simulations simulations = Apsim.Parent(report, typeof(Simulations)) as Simulations;
            dataStore = Apsim.Child(simulations, typeof(DataStore)) as DataStore;
            this.view.VariableList.SetSyntaxHighlighter("Report");

            dataStorePresenter = new DataStorePresenter();
            dataStorePresenter.Attach(dataStore, this.view.DataStoreView, explorerPresenter);
            this.view.DataStoreView.TableList.SelectedValue = this.report.Name;

            Simulation simulation = Apsim.Parent(report, typeof(Simulation)) as Simulation;
            if (simulation != null)
            {
                if (simulation.Parent is Experiment)
                    dataStorePresenter.ExperimentFilter = simulation.Parent as Experiment;
                else
                    dataStorePresenter.SimulationFilter = simulation;
                dataStorePresenter.PopulateGrid();
            }

        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.view.VariableList.ContextItemsNeeded -= OnNeedVariableNames;
            this.view.EventList.ContextItemsNeeded -= OnNeedEventNames;
            this.view.VariableList.TextHasChangedByUser -= OnVariableNamesChanged;
            this.view.EventList.TextHasChangedByUser -= OnEventNamesChanged;
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            dataStorePresenter.Detach();
        }

        /// <summary>The view is asking for variable names.</summary>
        void OnNeedVariableNames(object Sender, NeedContextItemsArgs e)
        {
            e.AllItems.AddRange(NeedContextItemsArgs.ExamineModelForNames(report, e.ObjectName, true, true, false));
        }

        /// <summary>The view is asking for event names.</summary>
        void OnNeedEventNames(object Sender, NeedContextItemsArgs e)
        {
            e.AllItems.AddRange(NeedContextItemsArgs.ExamineModelForNames(report, e.ObjectName, false, false, true));
        }

        /// <summary>The variable names have changed in the view.</summary>
        void OnVariableNamesChanged(object sender, EventArgs e)
        {
            explorerPresenter.CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(OnModelChanged);
            explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(report, "VariableNames", view.VariableList.Lines));
            explorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(OnModelChanged);
        }

        /// <summary>The event names have changed in the view.</summary>
        void OnEventNamesChanged(object sender, EventArgs e)
        {
            explorerPresenter.CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(OnModelChanged);
            explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(report, "EventNames", view.EventList.Lines));
            explorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(OnModelChanged);
        }

        /// <summary>The model has changed so update our view.</summary>
        void OnModelChanged(object changedModel)
        {
            if (changedModel == report)
            {
                view.VariableList.Lines = report.VariableNames;
                view.EventList.Lines = report.EventNames;
            }
        }

    }
}
