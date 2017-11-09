// -----------------------------------------------------------------------
// <copyright file="ExperimentPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Linq;
    using Models.Core;
    using Models.Factorial;
    using Views;

    /// <summary>
    /// Connects a Experiment model with an readonly memo view.
    /// </summary>
    public class ExperimentPresenter : IPresenter
    {
        /// <summary>
        /// Link to the storage
        /// </summary>
        [Link]
        private IStorageWriter storageWriter = null;

        /// <summary>
        /// The experiment object
        /// </summary>
        private Experiment experiment;

        /// <summary>
        /// The listview view
        /// </summary>
        private IMemoView listView;

        /// <summary>
        /// The explorer presenter
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the model and view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view</param>
        /// <param name="explorerPresenter">The explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            experiment = model as Experiment;
            listView = view as IMemoView;
            this.explorerPresenter = explorerPresenter;

            string[] allNames = experiment.GetSimulationNames().ToArray();
            listView.MemoLines = allNames;
            listView.LabelText = "Listed below are names of the " + allNames.Length.ToString() + " simulations that this experiment will create";
            listView.ReadOnly = true;
            listView.AddContextAction("Run APSIM", OnRunApsimClick);
        }

        /// <summary>
        /// Detach method
        /// </summary>
        public void Detach()
        {
        }

        /// <summary>
        /// User has clicked RunAPSIM
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void OnRunApsimClick(object sender, EventArgs e)
        {
            try
            {
                Simulation simulation = experiment.CreateSpecificSimulation(listView.MemoLines[listView.CurrentPosition.Y]);
                Commands.RunCommand run = new Commands.RunCommand(
                                                                  simulation,
                                                                  explorerPresenter,
                                                                  false,
                                                                  storageWriter);
                run.Do(null);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowMessage(err.Message, Simulation.ErrorLevel.Error);
            }
        }
    }
}
