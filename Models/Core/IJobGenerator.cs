namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using System.Collections.Generic;

    /// <summary>An interface for something that can generate jobs to run</summary>
    public interface IJobGenerator
    {
        /// <summary>Gets the next job to run</summary>
        IRunnable NextJobToRun();

        /// <summary>Gets a list of simulation names</summary>
        IEnumerable<string> GetSimulationNames();
    }
}
