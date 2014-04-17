using UserInterface.Views;
using Models.Core;
using System.Media;
using System;

namespace UserInterface.Commands
{
    class RunCommand : ICommand
    {
        private Model ModelClicked;
        private Simulations Simulations;
        private Utility.JobManager JobManager;
        private IExplorerView View;
        public bool ok { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RunCommand(Simulations Simulations, Model Simulation, IExplorerView view)
        {
            this.Simulations = Simulations;
            this.ModelClicked = Simulation;
            this.View = view;

            JobManager = new Utility.JobManager();
            JobManager.OnComplete += OnComplete;

        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            if (View != null)
                View.ShowMessage(ModelClicked.Name + " running...", Models.DataStore.ErrorLevel.Information);

            if (ModelClicked is Simulations)
                JobManager.AddJob(ModelClicked as Simulations);
            else
            {
                foreach (Simulation simulation in Simulations.FindAllSimulationsToRun(ModelClicked))
                    JobManager.AddJob(simulation);
            }
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
            if (View != null && e.ErrorMessage != null)
                View.ShowMessage(e.ErrorMessage, Models.DataStore.ErrorLevel.Error);
            if (JobManager.AllJobsFinished)
            {
                if (JobManager.SomeHadErrors)
                    View.ShowMessage(ModelClicked.Name + " complete with errors", Models.DataStore.ErrorLevel.Error);
                else
                    View.ShowMessage(ModelClicked.Name + " complete", Models.DataStore.ErrorLevel.Information);

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
