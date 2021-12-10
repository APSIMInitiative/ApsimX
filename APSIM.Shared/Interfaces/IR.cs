using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APSIM.Shared.Interfaces
{
    /// <summary>
    /// An interface to an R client.
    /// </summary>
    public interface IR
    {
        /// <summary>
        /// Run an R script asynchronously. Throws if an error occurs.
        /// </summary>
        /// <param name="scriptPath">Path to the R script.</param>
        /// <param name="arguments">Arguments to be passed to the R script.</param>
        /// <param name="cancelToken">Cancellation token, used to cancel script execution.</param>
        Task RunScriptAsync(string scriptPath, IEnumerable<string> arguments, CancellationToken cancelToken);
    }
}
