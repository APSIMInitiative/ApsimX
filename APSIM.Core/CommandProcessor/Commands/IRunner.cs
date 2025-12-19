namespace APSIM.Core;

/// <summary>
/// An interface for a class which runs APSIM. This is used to abstract
/// runner implementations.
/// </summary>
public interface IRunner
{
    /// <summary>
    /// If provided, this will be invoked whenever an error occurs.
    /// </summary>
    Action<Exception> ErrorHandler { get; set; }


    /// <summary>Arguments for all jobs completed event.</summary>
    public class AllJobsCompletedArgs
    {
        /// <summary>The exception thrown by the job. Can be null for no exception.</summary>
        public List<Exception> AllExceptionsThrown { get; set; }

        /// <summary>Amount of time all jobs took to run.</summary>
        public TimeSpan ElapsedTime { get; set; }
    }

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
    /// Run APSIM for all simulations relative to a model.
    /// </summary>
    /// <param name="relativeTo">The model that defines the scope for running APSIM.</param>
    void Run(INodeModel relativeTo);

    /// <summary>
    /// Stop any running jobs.
    /// </summary>
    void Stop();
}
