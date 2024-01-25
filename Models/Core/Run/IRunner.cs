using System;
using System.Collections.Generic;
using static Models.Core.Run.Runner;

namespace Models.Core.Run
{
    /// <summary>
    /// An interface for a class which runs things. This is used to abstract
    /// runner implementations away from the GUI.
    /// </summary>
    public interface IRunner
    {
        /// <summary>
        /// If provided, this will be invoked whenever an error occurs.
        /// </summary>
        Action<Exception> ErrorHandler { get; set; }

        /// <summary>
        /// Invoked when all jobs are completed.
        /// </summary>
        event EventHandler<AllJobsCompletedArgs> AllSimulationsCompleted;

        /// <summary>
        /// Gets the aggregate progress of all jobs as a real number in range [0, 1].
        /// </summary>
        double Progress { get; }

        /// <summary>
        /// Current status of the running jobs.
        /// </summary>
        string Status { get; }

        /// <summary>
        /// Run all simulations.
        /// </summary>
        /// <returns>A list of exception or null if no exceptions thrown.</returns>
        List<Exception> Run();

        /// <summary>
        /// Stop any running jobs.
        /// </summary>
        void Stop();
    }
}
