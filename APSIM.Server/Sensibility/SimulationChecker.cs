using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.Core.Run;
using System.Threading.Tasks;
using System.Threading;

namespace APSIM.Server.Sensibility
{
    /// <summary>
    /// This class will verify that an .apsimx file correctly zeroes/resets its
    /// state between runs.
    /// </summary>
    public class SimulationChecker : IRunner
    {
        /// <summary>
        /// Blocking wait until running complete?
        /// </summary>
        private readonly bool wait;

        /// <summary>
        /// The runner instance.
        /// </summary>
        private readonly Runner runner;

        /// <summary>
        /// Have we finished the first run?
        /// </summary>
        private bool finishedFirstRun = false;

        private CancellationTokenSource cts = new CancellationTokenSource();

        /// <summary>
        /// Create a new <see cref="SimulationChecker"/> instance.
        /// </summary>
        /// <param name="wait">Blocking wait until running completes?</param>
        public SimulationChecker(IModel model, bool wait)
        {
            this.wait = wait;
            runner = new Runner(model, runPostSimulationTools: false, runTests: false, wait: true);
            runner.Use(new SimulationCheckerJobRunner());
        }

        /// <inheritdoc />
        public Action<Exception> ErrorHandler { get => runner.ErrorHandler; set => runner.ErrorHandler = value; }

        /// <inheritdoc />
        public double Progress => finishedFirstRun ? 1 : runner.Progress;

        /// <summary>
        /// Custom status message. If null, the job runner's internal 
        /// status will be retrieved.
        /// </summary>
        private string status;

        /// <inheritdoc />
        public string Status => status ?? runner.Status;

        /// <inheritdoc />
        public event EventHandler<Runner.AllJobsCompletedArgs> AllSimulationsCompleted;

        /// <summary>
        /// Verify that an .apsimx file correctly zeroes/resets its state
        /// between runs.
        /// </summary>
        /// <param name="model">
        /// The model(s) to be checked. This can be any kind of model; this
        /// model and all descendant simulations will be checked.
        /// </param>
        public List<Exception> Run()
        {
            Task<List<Exception>> task = Task.Run(() => Check(cts.Token));
            if (wait)
            {
                task.Wait();
                return task.Result;
            }
            else
                return new List<Exception>();
        }

        /// <summary>
        /// Verify that an .apsimx file correctly zeroes/resets its state
        /// between runs.
        /// 
        /// This method will block.
        /// </summary>
        /// <param name="model">
        /// The model(s) to be checked. This can be any kind of model; this
        /// model and all descendant simulations will be checked.
        /// </param>
        public List<Exception> Check(CancellationToken cancelToken)
        {
            status = null;
            finishedFirstRun = false;
            DateTime startTime = DateTime.Now;

            List<Exception> errors = runner.Run();

            finishedFirstRun = true;

            if (errors.Any())
                return errors;

            if (cancelToken.IsCancellationRequested)
                return new List<Exception>();

            status = "Verifying that simulation state has been reset";
            errors = runner.Run();
            DateTime finishTime = DateTime.Now;

            Runner.AllJobsCompletedArgs args = new Runner.AllJobsCompletedArgs();
            args.AllExceptionsThrown = errors;
            args.ElapsedTime = finishTime - startTime;
            AllSimulationsCompleted?.Invoke(this, args);

            return errors;
        }

        /// <inheritdoc />
        public void Stop()
        {
            cts.Cancel();
            runner.Stop();
        }
    }
}
