using UserInterface.Views;
using Models.Core;
using System.Media;
using System;
using UserInterface.Presenters;
using System.Diagnostics;
using System.Threading;
using Models.Factorial;

namespace UserInterface.Commands
{
    class RunCommand : ICommand
    {
        private Model ModelClicked;
        private Simulations Simulations;
        private Utility.JobManager JobManager;
        private ExplorerPresenter ExplorerPresenter;
        private Stopwatch Timer = new Stopwatch(); 
        public bool ok { get; set; }
        public bool IsRunning { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RunCommand(Simulations Simulations, Model Simulation, ExplorerPresenter presenter)
        {
            this.Simulations = Simulations;
            this.ModelClicked = Simulation;
            this.ExplorerPresenter = presenter;

            JobManager = new Utility.JobManager();
            JobManager.AllJobsCompleted += OnComplete;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            IsRunning = true;

            if (ExplorerPresenter != null)
                ExplorerPresenter.ShowMessage(ModelClicked.Name + " running...", Models.DataStore.ErrorLevel.Information);

            Timer.Start();

            if (ModelClicked is Simulations)
            {
                Simulations.SimulationToRun = null;  // signal that we want to run all simulations.
            }
            else
                Simulations.SimulationToRun = ModelClicked;
           
            JobManager.AddJob(Simulations);
            JobManager.AllJobsCompleted += OnComplete;
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
        private void OnComplete(object sender, EventArgs e)
        {
            Timer.Stop();

            string errorMessage = string.Empty;

            for (int j = 0; j < JobManager.CountOfJobs; j++)
                errorMessage += JobManager.GetJobErrorMessage(j);
            if (errorMessage == string.Empty)
                ExplorerPresenter.ShowMessage(ModelClicked.Name + " complete "
                        + " [" + Timer.Elapsed.TotalSeconds.ToString("#.00") + " sec]", Models.DataStore.ErrorLevel.Information);
            else
                ExplorerPresenter.ShowMessage(errorMessage, Models.DataStore.ErrorLevel.Error);

            SoundPlayer player = new SoundPlayer();
            if (DateTime.Now.Month == 12)
                player.Stream = Properties.Resources.notes;
            else
                player.Stream = Properties.Resources.success;
            player.Play();
            IsRunning = false;
        }
    }
}
