using Models.Core;
using Models.Core.Run;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Timers;
using UserInterface.Presenters;
using Utility;

namespace UserInterface.Commands
{
    public sealed class RunCommand : IDisposable
    {
        /// <summary>The name of the job</summary>
        private string jobName;

        /// <summary>The collection of jobs to run</summary>
        private IRunner jobRunner;

        /// <summary>The explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The timer we use to update the progress bar.</summary>
        private Timer timer = null;

        /// <summary>
        /// Indicates the runs were aborted rather than allowed to run to completion
        /// </summary>
        private Boolean aborted = false;

        /// <summary>List of all errors encountered</summary>
        private List<Exception> errors = new List<Exception>();

        /// <summary>Constructor</summary>
        /// <param name="name">Name of the job to be displayed in the UI..</param>
        /// <param name="runner">Runner which will run the job.</param>
        /// <param name="presenter">The explorer presenter.</param>
        [Newtonsoft.Json.JsonConstructor]
        public RunCommand(string name, IRunner runner, ExplorerPresenter presenter)
        {
            this.jobName = name;
            this.jobRunner = runner;
            this.explorerPresenter = presenter;
            this.explorerPresenter.MainPresenter.AddStopHandler(OnStopSimulation);

            // Ensure that errors are displayed in GUI live as they occur.
            object errorMutex = new object();
            runner.ErrorHandler = e =>
            {
                lock (errorMutex)
                    explorerPresenter.MainPresenter.ShowError(e, false);
            };

            jobRunner.AllSimulationsCompleted += OnAllJobsCompleted;
        }

        /// <summary>Is this instance currently running APSIM.</summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary>Perform the command</summary>
        public void Do()
        {
            explorerPresenter.MainPresenter.ClearStatusPanel();
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
            // Manually fire of an OnTimerTick event.
            OnTimerTick(this, null);
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
            if (errors.Count == 0 && !aborted)
                explorerPresenter.MainPresenter.ShowMessage(string.Format("{0} complete [{1} sec]", jobName, e.ElapsedTime.TotalSeconds.ToString("#.00")), Simulation.MessageType.Information, false);
            // We don't need to display error messages now - they are displayed as they occur.

#if NET6_0_OR_GREATER
            if (!Configuration.Settings.Muted && OperatingSystem.IsWindows())
#else
            if (!Configuration.Settings.Muted)
#endif
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
                        player.SoundLocation = Configuration.Settings.SimulationCompleteWavFileName;
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
            // Any error messages will already be onscreen, as they are
            // rendered as they occur.
            explorerPresenter.MainPresenter.ShowMessage($"{jobName} aborted", Simulation.MessageType.Information, false);
            aborted = true;
        }

        /// <summary>
        /// Clean up at the end of a set of runs. Stops the job manager, timers, etc.
        /// </summary>
        private void Stop()
        {
            explorerPresenter.MainPresenter.HideProgressBar();
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
                explorerPresenter.MainPresenter.ShowProgressMessage($"{jobName} running ({jobRunner.Status})");
                explorerPresenter.MainPresenter.ShowProgress(progress);
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
