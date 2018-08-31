namespace UserInterface.Commands
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Runners;
    using Presenters;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Media;
    using System.Timers;

    class RunCommand : ICommand
    {
        /// <summary>The name of the job</summary>
        private string jobName;

        /// <summary>The collection of jobs to run</summary>
        private RunOrganiser jobManager;

        /// <summary>The runner to use to run the jobs</summary>
        private IJobRunner jobRunner;

        /// <summary>The explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The stop watch we can use to time the runs.</summary>
        private Timer timer = null;

        /// <summary>The stop watch we can use to time the runs.</summary>
        private Stopwatch stopwatch = new Stopwatch();

        /// <summary>List of all errors encountered</summary>
        private List<Exception> errors = new List<Exception>();

        /// <summary>Number of simulations that have run</summary>
        private int numSimulationsRun = 0;

        /// <summary>Retuns true if simulations are running.</summary>
        public bool IsRunning { get; set; }

        public event EventHandler Finished;

        /// <summary>Constructor</summary>
        /// <param name="model">The model the user has selected to run</param>
        /// <param name="presenter">The explorer presenter.</param>
        /// <param name="multiProcess">Use the multi-process runner?</param>
        /// <param name="storage">A storage writer where all data should be stored</param>
        public RunCommand(IModel model, ExplorerPresenter presenter, bool multiProcess, IStorageWriter storage)
        {
            this.jobName = model.Name;
            this.explorerPresenter = presenter;
            this.explorerPresenter.MainPresenter.AddStopHandler(OnStopSimulation);
            jobManager = Runner.ForSimulations(explorerPresenter.ApsimXFile, model, false);

            if (multiProcess)
                jobRunner = new JobRunnerMultiProcess(storage);
            else
                jobRunner = new JobRunnerAsync();
            jobRunner.JobCompleted += OnJobCompleded;
            jobRunner.AllJobsCompleted += OnAllJobsCompleted;
        }

        /// <summary>Perform the command</summary>
        public void Do(CommandHistory CommandHistory)
        {
            IsRunning = true;

            stopwatch.Start();
                
            jobRunner.Run(jobManager, wait: false);

            timer = new Timer();
            timer.Interval = 1000;
            timer.AutoReset = true;
            timer.Elapsed += OnTimerTick;
            timer.Start();
        }

        /// <summary>Undo the command</summary>
        public void Undo(CommandHistory CommandHistory)
        {
        }

        /// <summary>Job has completed</summary>
        private void OnJobCompleded(object sender, JobCompleteArgs e)
        {
            lock (this)
            {
                numSimulationsRun++;
                if (e.exceptionThrowByJob != null)
                    errors.Add(e.exceptionThrowByJob);
            }
        }

        /// <summary>All jobs have completed</summary>
        private void OnAllJobsCompleted(object sender, AllCompletedArgs e)
        {
            if (e.exceptionThrown != null)
                errors.Add(e.exceptionThrown);

            Stop();
            if (errors.Count == 0)
                explorerPresenter.MainPresenter.ShowMessage(jobName + " complete "
                        + " [" + stopwatch.Elapsed.TotalSeconds.ToString("#.00") + " sec]", Simulation.MessageType.Information);
            else
            {
                string errorMessage = null;
                errors.ForEach(error => errorMessage += error.ToString() + Environment.NewLine
                                                     +  "----------------------------------------------" + Environment.NewLine);
                explorerPresenter.MainPresenter.ShowError(errors);
            }

            SoundPlayer player = new SoundPlayer();
            if (DateTime.Now.Month == 12 && DateTime.Now.Day == 25)
                player.Stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.notes.wav");
            else
                player.Stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.success.wav");
            player.Play();

            Finished?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handles a signal that we want to abort the set of simulations.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">Event arguments. Shouldn't be anything of interest</param>
        private void OnStopSimulation(object sender, EventArgs e)
        {
            Stop();
            string msg = jobName + " aborted";
            if (errors.Count == 0)
                explorerPresenter.MainPresenter.ShowMessage(msg, Simulation.MessageType.Information);
            else
            {
                explorerPresenter.MainPresenter.ShowError(errors);
            }
        }

        /// <summary>
        /// Clean up at the end of a set of runs. Stops the job manager, timers, etc.
        /// </summary>
        private void Stop()
        {
            this.explorerPresenter.MainPresenter.RemoveStopHandler(OnStopSimulation);
            timer.Stop();
            stopwatch.Stop();
            jobRunner.Stop();

            IsRunning = false;
            jobManager = null;
            jobRunner = null;
        }

        /// <summary>
        /// The timer has ticked. Update the progress bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            int numSimulations = 0;
            if (jobManager.SimulationNamesBeingRun != null)
                numSimulations = jobManager.SimulationNamesBeingRun.Count;

            double numberComplete = 0.0;
            if (jobManager.SimClocks != null)
            {
                foreach(Models.IClock clock in jobManager.SimClocks)
                {
                    if (clock != null)
                        numberComplete += clock.FractionComplete;
                }
            }
            else
            {
                numberComplete = numSimulationsRun;
            }

            double percentComplete = (numberComplete / numSimulations) * 100.0;

            if (numSimulations > 0)
            {
                explorerPresenter.MainPresenter.ShowMessage(jobName + " running (" +
                         numSimulationsRun + " of " +
                         (numSimulations) + " completed)", Simulation.MessageType.Information);

                explorerPresenter.MainPresenter.ShowProgress(Convert.ToInt32(percentComplete));
            }
        }
    }
}
