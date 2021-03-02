namespace UserInterface.Commands
{
    using Models.Core;
    using Models.Core.Run;
    using Presenters;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Media;
    using System.Timers;
    using Utility;

    public sealed class RunCommand : IDisposable
    {
        /// <summary>The name of the job</summary>
        private string jobName;

        /// <summary>The collection of jobs to run</summary>
        private Runner jobRunner;

        /// <summary>The explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The timer we use to update the progress bar.</summary>
        private Timer timer = null;

        /// <summary>List of all errors encountered</summary>
        private List<Exception> errors = new List<Exception>();

        /// <summary>Constructor</summary>
        /// <param name="name">Name of the job to be displayed in the UI..</param>
        /// <param name="runner">Runner which will run the job.</param>
        /// <param name="presenter">The explorer presenter.</param>
        public RunCommand(string name, Runner runner, ExplorerPresenter presenter)
        {
            this.jobName = name;
            this.jobRunner = runner;
            this.explorerPresenter = presenter;
            this.explorerPresenter.MainPresenter.AddStopHandler(OnStopSimulation);

            jobRunner.AllSimulationsCompleted += OnAllJobsCompleted;
        }

        /// <summary>Is this instance currently running APSIM.</summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary>Perform the command</summary>
        public void Do()
        {
            IsRunning = true;
            jobRunner.Run();

            if (IsRunning)
            {
                timer = new Timer();
                timer.Interval = 1000;
                timer.AutoReset = true;
                timer.Elapsed += OnTimerTick;
                timer.Start();
            }
        }

        /// <summary>All jobs have completed</summary>
        private void OnAllJobsCompleted(object sender, Runner.AllJobsCompletedArgs e)
        {
            IsRunning = false;
            if (timer != null)
                timer.Elapsed -= OnTimerTick;

            if (e.AllExceptionsThrown != null)
                errors.AddRange(e.AllExceptionsThrown);
            try
            {
                Stop();
            }
            catch
            {
                // We could display the error message, but we're about to display output to the user anyway.
            }
            if (errors.Count == 0)
                explorerPresenter.MainPresenter.ShowMessage(string.Format("{0} complete [{1} sec]", jobName, e.ElapsedTime.TotalSeconds.ToString("#.00")), Simulation.MessageType.Information);
            else
                explorerPresenter.MainPresenter.ShowError(errors);

            if (!Configuration.Settings.Muted)
            {
                // Play a completion sound.
                SoundPlayer player = new SoundPlayer();
                if (errors.Count > 0)
                {
                    if (File.Exists(Configuration.Settings.SimulationCompleteWithErrorWavFileName))
                        player.SoundLocation = Configuration.Settings.SimulationCompleteWithErrorWavFileName;
                    else
                        player.Stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.Sounds.Fail.wav");
                }
                else
                {
                    if (File.Exists(Configuration.Settings.SimulationCompleteWavFileName))
                        player.SoundLocation = Configuration.Settings.SimulationCompleteWithErrorWavFileName;
                    else
                        player.Stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.Sounds.Success.wav");
                }

                player.Play();
            }
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
            if (timer != null)
            {
                timer.Stop();
                timer.Elapsed -= OnTimerTick;
            }
            jobRunner?.Stop();
            jobRunner = null;
            IsRunning = false;
        }

        /// <summary>
        /// The timer has ticked. Update the progress bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            if (jobRunner == null)
            {
                timer?.Stop();
                timer.Elapsed -= OnTimerTick;
            }
            else //if (jobRunner?.TotalNumberOfSimulations > 0)
            {
                double progress = jobRunner?.Progress ?? 0;
                explorerPresenter.MainPresenter.ShowMessage($"{jobName} running ({jobRunner.Status})", Simulation.MessageType.Information);
                explorerPresenter.MainPresenter.ShowProgress(Convert.ToInt32(progress * 100, CultureInfo.InvariantCulture));
            }
            //else if (jobRunner != null)
            //    explorerPresenter.MainPresenter.ShowProgress(Convert.ToInt32(jobRunner.Progress * 100, CultureInfo.InvariantCulture));
        }

        public void Dispose()
        {
            if (timer != null)
                timer.Dispose();
        }
    }
}
