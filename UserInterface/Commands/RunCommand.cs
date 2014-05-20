using UserInterface.Views;
using Models.Core;
using System.Media;
using System;
using UserInterface.Presenters;

namespace UserInterface.Commands
{
    class RunCommand : ICommand
    {
        private Model ModelClicked;
        private Simulations Simulations;
        private Utility.JobManager JobManager;
        private ExplorerPresenter ExplorerPresenter;
        public bool ok { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RunCommand(Simulations Simulations, Model Simulation, ExplorerPresenter presenter)
        {
            this.Simulations = Simulations;
            this.ModelClicked = Simulation;
            this.ExplorerPresenter = presenter;

            JobManager = new Utility.JobManager();
            JobManager.OnComplete += OnComplete;

        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            if (ExplorerPresenter != null)
                ExplorerPresenter.ShowMessage(ModelClicked.Name + " running...", Models.DataStore.ErrorLevel.Information);

            if (ModelClicked is Simulations)
            {
                Simulations.SimulationToRun = null;  // signal that we want to run all simulations.
            }
            else
            {
                Simulations.SimulationToRun = ModelClicked;
            }

            JobManager.AddJob(Simulations);
            JobManager.Start(waitUntilFinished: false);
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
        }

        /// <summary>
        /// Called whenever a job completes.
        /// </summary>
        private void OnComplete(object sender, Utility.JobManager.JobCompleteArgs e)
        {
            if (ExplorerPresenter != null && e.ErrorMessage != null)
                ExplorerPresenter.ShowMessage(e.ErrorMessage, Models.DataStore.ErrorLevel.Error);
            if (e.PercentComplete == 100)
            {
                if (JobManager.SomeHadErrors)
                    ExplorerPresenter.ShowMessage(ModelClicked.Name + " complete with errors", Models.DataStore.ErrorLevel.Error);
                else
                    ExplorerPresenter.ShowMessage(ModelClicked.Name + " complete", Models.DataStore.ErrorLevel.Information);

                SoundPlayer player = new SoundPlayer();
                if (DateTime.Now.Month == 12)
                    player.Stream = Properties.Resources.notes;
                else
                    player.Stream = Properties.Resources.success;
                player.Play();
            }
        }

    }
}
