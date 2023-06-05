using System;
using APSIM.Shared.JobRunning;

namespace Models.Core.Run
{
    [Serializable]
    class EmptyJob : IRunnable
    {
        /// <summary>
        /// Prepare the job for running.
        /// </summary>
        public void Prepare()
        {
            // Do nothing.
        }

        /// <summary>Called to start the job. Can throw on error.</summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(System.Threading.CancellationTokenSource cancelToken)
        {
            //do nothing
        }

        /// <summary>
        /// Cleanup the job after running it.
        /// </summary>
        public void Cleanup()
        {
            // Do nothing.
        }

        /// <summary>
        /// Name of the job.
        /// </summary>
        public string Name { get { return "Empty Job"; } }

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress { get { return 1; } }
    }
}
