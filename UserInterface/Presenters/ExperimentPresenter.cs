using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Factorial;
using UserInterface.Views;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Core.Runners;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Connects a Experiment model with an readonly memo view.
    /// </summary>
    public class ExperimentPresenter : IPresenter
    {
        private Experiment Experiment;
        private IMemoView ListView;
        private ExplorerPresenter ExplorerPresenter;

        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            Experiment = Model as Experiment;
            ListView = View as IMemoView;
            ExplorerPresenter = explorerPresenter;

            ListView.MemoLines = Experiment.Names();
            ListView.LabelText = "Listed below are names of the " + ListView.MemoLines.Length.ToString() + " simulations that this experiment will create";
            ListView.ReadOnly = true;
            ListView.AddContextAction("Run APSIM", OnRunApsimClick);
        }

        public void Detach()
        {

        }

        /// <summary>
        /// User has clicked RunAPSIM
        /// </summary>
        private void OnRunApsimClick(object sender, EventArgs e)
        {
            try
            {
                List<JobManager.IRunnable> jobs = new List<JobManager.IRunnable>();
                Simulation simulation = Experiment.CreateSpecificSimulation(ListView.MemoLines[ListView.CurrentPosition.Y]);
                jobs.Add(simulation);
                jobs.Add(new RunAllCompletedEvent(ExplorerPresenter.ApsimXFile));

                Commands.RunCommand run = new Commands.RunCommand(jobs, 
                                                                  simulation.Name,
                                                                  ExplorerPresenter);
                run.Do(null);
            }
            catch (Exception err)
            {
                ExplorerPresenter.MainPresenter.ShowMessage(err.Message, Models.DataStore.ErrorLevel.Error);
            }
        }

    }
}
