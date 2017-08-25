// -----------------------------------------------------------------------
// <copyright file="ExperimentPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Factorial;
    using Views;

    /// <summary>
    /// Connects a Experiment model with an readonly memo view.
    /// </summary>
    public class ExperimentPresenter : IPresenter
    {
        /// <summary>
        /// Experiment object
        /// </summary>
        private Experiment experiment;

        /// <summary>
        /// The item list
        /// </summary>
        private IMemoView listView;

        /// <summary>
        /// The presenter
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view</param>
        /// <param name="explorerPresenter">The presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.experiment = model as Experiment;
            this.listView = view as IMemoView;
            this.explorerPresenter = explorerPresenter;

            string[] allNames = this.experiment.Names();
            this.listView.MemoLines = allNames;
            this.listView.LabelText = "Listed below are names of the " + allNames.Length.ToString() + " simulations that this experiment will create";
            this.listView.ReadOnly = true;
            this.listView.AddContextAction("Run APSIM", this.OnRunApsimClick);
        }

        /// <summary>
        /// Detach the presenter
        /// </summary>
        public void Detach()
        {
        }

        /// <summary>
        /// User has clicked RunAPSIM
        /// </summary>
        /// <param name="sender">Sender button</param>
        /// <param name="e">Event details</param>
        private void OnRunApsimClick(object sender, EventArgs e)
        {
            try
            {
                Simulation simulation = this.experiment.CreateSpecificSimulation(this.listView.MemoLines[this.listView.CurrentPosition.Y]);
                JobManager.IRunnable job = Runner.ForSimulations(this.explorerPresenter.ApsimXFile, simulation, false);

                Commands.RunCommand run = new Commands.RunCommand(
                                                                  job, 
                                                                  simulation.Name,
                                                                  this.explorerPresenter,
                                                                  false);
                run.Do(null);
            }
            catch (Exception err)
            {
                this.explorerPresenter.MainPresenter.ShowMessage(err.Message, Models.DataStore.ErrorLevel.Error);
            }
        }
    }
}
