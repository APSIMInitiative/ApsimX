namespace UserInterface.Commands
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Presenters;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Media;
    using System.Timers;

    class RunCommand : ICommand
    {
        /// <summary>The model that was right clicked on by user.</summary>
        private Model modelClicked;

        /// <summary>The top level simulations object.</summary>
        private Simulations simulations;

        /// <summary>The job manager running the simulations.</summary>
        private JobManager jobManager;

        /// <summary>The explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The stop watch we can use to time the runs.</summary>
        private Timer timer = null;

        /// <summary>The stop watch we can use to time the runs.</summary>
        private Stopwatch stopwatch = new Stopwatch();

        /// <summary>A flag indicated whether we should show number of sims running.</summary>
        private bool showNumberRunning = true;

        /// <summary>Retuns true if simulations are running.</summary>
        public bool IsRunning { get; set; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="simulations">The top level simulations object.</param>
        /// <param name="simulation">The simulation object clicked on.</param>
        /// <param name="presenter">The explorer presenter.</param>
        public RunCommand(Simulations simulations, Model simulation, ExplorerPresenter presenter)
        {
            this.simulations = simulations;
            this.modelClicked = simulation;
            this.explorerPresenter = presenter;

            jobManager = new JobManager();
            jobManager.AllJobsCompleted += OnComplete;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            IsRunning = true;

            if (!DuplicatesFound())
            {
                stopwatch.Start();

                JobManager.IRunnable job = Runner.ForSimulations(simulations, modelClicked, false);

                jobManager.AddJob(job);
                jobManager.AllJobsCompleted += OnComplete;
                jobManager.Start(waitUntilFinished: false);

                showNumberRunning = true;

                timer = new Timer();
                timer.Interval = 1000;
                timer.AutoReset = true;
                timer.Elapsed += OnTimerTick;
                timer.Start();
            }
        }

        /// <summary>
        /// Return true if duplications were found.
        /// </summary>
        /// <returns></returns>
        private bool DuplicatesFound()
        {
            List<IModel> allSims = Apsim.ChildrenRecursively(simulations, typeof(Simulation));
            List<string> allSimNames = allSims.Select(s => s.Name).ToList();
            var duplicates = allSimNames
                .GroupBy(i => i)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            if (duplicates.ToList().Count > 0)
            {
                string errorMessage = "Duplicate simulation names found " + StringUtilities.BuildString(duplicates.ToArray(), ", ");
                explorerPresenter.MainPresenter.ShowMessage(errorMessage, Models.DataStore.ErrorLevel.Error);
                return true;
            }
            return false;
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
            timer.Stop();
            stopwatch.Stop();

            string errorMessage = GetErrorsFromSimulations();
            if (errorMessage == null)
                explorerPresenter.MainPresenter.ShowMessage(modelClicked.Name + " complete "
                        + " [" + stopwatch.Elapsed.TotalSeconds.ToString("#.00") + " sec]", Models.DataStore.ErrorLevel.Information);
            else
                explorerPresenter.MainPresenter.ShowMessage(errorMessage, Models.DataStore.ErrorLevel.Error);

            SoundPlayer player = new SoundPlayer();
            if (DateTime.Now.Month == 12 && DateTime.Now.Day == 25)
                player.Stream = Properties.Resources.notes;
            else
                player.Stream = Properties.Resources.success;
            player.Play();
            IsRunning = false;
        }

        /// <summary>
        /// This gets called everytime a simulation completes. When all are done then
        /// invoke each model's OnAllCompleted method.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private string GetErrorsFromSimulations()
        {
            string errorMessage = null;
            foreach (JobManager.IRunnable job in jobManager.CompletedJobs)
            {
                if (job.ErrorMessage != null)
                    errorMessage += job.ErrorMessage + Environment.NewLine;
            }

            return errorMessage;
        }

        /// <summary>
        /// The timer has ticked. Update the progress bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            int numSimulationsRun = jobManager.CompletedJobs.Count;
            int numSimulationsToRun = jobManager.JobCount + numSimulationsRun;

            // One job will be the simulations object we added above. We don't want
            // to count this in the list of simulations being run, hence the -1 below.
            if (numSimulationsToRun > 1)
            {
                if (showNumberRunning)
                {
                    showNumberRunning = false;
                    explorerPresenter.MainPresenter.ShowMessage(modelClicked.Name + " running (" + (numSimulationsToRun-1) + ")", Models.DataStore.ErrorLevel.Information);
                }

                double percent = numSimulationsRun * 1.0 / numSimulationsToRun * 100.0;
                explorerPresenter.MainPresenter.ShowProgress(Convert.ToInt32(percent));
                if (jobManager.JobCount == 0)
                    timer.Stop();
            }
        }
    }
}
