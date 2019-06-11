
namespace Models.Core.Run
{
    using APSIM.Shared.Utilities;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// An class for encapsulating a list of simulations that are ready
    /// to be run. An instance of this class can be used with a job runner.
    /// </summary>
    public class Runner
    {
        /// <summary>Top level model.</summary>
        private Simulations rootModel;

        /// <summary>The model to use to search for simulations to run.</summary>
        private IModel relativeTo;

        /// <summary>The model to use to search for simulations to run.</summary>
        private List<Exception> errors = new List<Exception>();

        /// <summary>An enumerated type for specifying how a series of simulations are run.</summary>
        public enum RunTypeEnum
        {
            /// <summary>Run using a single thread - each job synchronously.</summary>
            SingleThreaded,

            /// <summary>Run using multiple cores - each job asynchronously.</summary>
            MultiThreaded,

            /// <summary>Run using multiple, separate processes - each job asynchronously.</summary>
            MultiProcess
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="relativeTo">The model to use to search for simulations to run.</param>
        public Runner(IModel relativeTo)
        {
            this.relativeTo = relativeTo;
            rootModel = Apsim.Parent(relativeTo, typeof(Simulations)) as Simulations;
        }

        /// <summary>
        /// Run all simulations.
        /// </summary>
        /// <param name="runType">How should the simulations be run?</param>
        /// <param name="wait">Wait until all simulations are complete?</param>
        /// <param name="verbose">Produce verbose output?</param>
        /// <param name="numberOfProcessors">Number of CPU processes to use. -1 indicates all processes.</param>
        /// <param name="runTests">Run all test models?</param>
        /// <returns>A list of exception or null if no exceptions thrown.</returns>
        public List<Exception> Run(RunTypeEnum runType, bool wait = true, bool verbose = false, int numberOfProcessors = -1, bool runTests = false)
        {
            errors.Clear();

            Apsim.ParentAllChildren(rootModel);
            Apsim.ChildrenRecursively(rootModel).ForEach(m => m.OnCreated());
            IJobManager jobManager = Models.Core.Runners.Runner.ForSimulations(rootModel, relativeTo, false);
            IJobRunner jobRunner = new JobRunnerSync();
            jobRunner.JobCompleted += OnJobCompleded;
            jobRunner.Run(jobManager, wait: true);
            jobRunner.JobCompleted -= OnJobCompleded;

            var storage = Apsim.Find(rootModel, typeof(IDataStore)) as IDataStore;
            storage.Writer.Stop();

            return errors;
        }


        /// <summary>Job has completed</summary>
        private void OnJobCompleded(object sender, JobCompleteArgs e)
        {
            lock (this)
            {
                if (e.exceptionThrowByJob != null)
                    errors.Add(e.exceptionThrowByJob);
            }
        }
    }
}
