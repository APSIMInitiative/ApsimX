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
            JobManager.OnComplete += OnComplete;
            if (!(ModelClicked is Simulation) && !(ModelClicked is Experiment) && !(ModelClicked is Simulations))
            {
                Model simulation = ModelClicked.Find(typeof(Simulation));
                if (simulation == null)
                {
                    simulation = ModelClicked.Find(typeof(Experiment));
                }
                if (simulation == null)
                {
                    simulation = ModelClicked.Find(typeof(Simulations));
                }
                ModelClicked = simulation;
            }

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
            else if (ModelClicked is Simulation)
            {
                Simulation simulation = ModelClicked as Simulation;
                try
                {
                    simulation.Run(null, null);
                    OnComplete(null, new Utility.JobManager.JobCompleteArgs() { PercentComplete = 100 });
                    foreach (Model model in Simulations.Children.AllRecursively)
                        model.OnAllSimulationsCompleted();
                }
                catch (Exception err)
                {
                    OnComplete(null, new Utility.JobManager.JobCompleteArgs() { ErrorMessage = err.Message, PercentComplete = 100 });
                }
                return;
            }
            else
                Simulations.SimulationToRun = ModelClicked;
           
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
                Timer.Stop();

                if (JobManager.SomeHadErrors)
                    ExplorerPresenter.ShowMessage(ModelClicked.Name + " complete with errors", Models.DataStore.ErrorLevel.Error);
                else
                    ExplorerPresenter.ShowMessage(ModelClicked.Name + " complete "
                        + " [" + Timer.Elapsed.TotalSeconds.ToString("#.00") + " sec]", Models.DataStore.ErrorLevel.Information);

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
}
