using APSIM.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APSIM.Shared.Documentation.Extensions;

namespace APSIM.Shared.Containers
{
    /// <summary>
    /// Runs R code inside a docker container.
    /// </summary>
    public class RDocker : IR
    {
        /// <summary>
        /// Name of an environment variable, which, if set to 1, will cause
        /// apsim to /not/ run R code through docker.
        /// </summary>
        private const string skipDockerVariable = "APSIM_NO_DOCKER";

        /// <summary>
        /// Name of the docker image which will be run.
        /// </summary>
        private const string apsimCompleteImageName = "apsiminitiative/apsimng-complete:latest";

        /// <summary>
        /// Callback to be invoked when stdout is received from a container.
        /// </summary>
        private readonly Action<string> outputHandler;

        /// <summary>
        /// Handler for warnings from docker.
        /// </summary>
        private Action<string> warningHandler;

        /// <summary>
        /// Callback to be invoked when stderr is received from a container.
        /// </summary>
        private Action<string> errorHandler;

        /// <summary>
        /// Create a new <see cref="RDocker"/> instance.
        /// </summary>
        /// <param name="outputHandler">Handler for receiving stdout from the container.</param>
        /// <param name="warningHandler">Handler for warnings from docker.</param>
        /// <param name="errorHandler">Callback for stderr from the container.</param>
        public RDocker(Action<string> outputHandler = null, Action<string> warningHandler = null, Action<string> errorHandler = null)
        {
            this.outputHandler = outputHandler;
            this.warningHandler = warningHandler;
            this.errorHandler = errorHandler;
        }

        /// <summary>
        /// Run an R script asynchronously. Throws if an error occurs.
        /// </summary>
        /// <param name="scriptPath">Path to the R script. Note that all other files required by the R script must live in the same directory tree as the script.</param>
        /// <param name="arguments">Arguments to be passed to the R script.</param>
        /// <param name="cancelToken">Cancellation token, used to cancel script execution.</param>
        public async Task RunScriptAsync(string scriptPath, IEnumerable<string> arguments, CancellationToken cancelToken)
        {
            string hostScriptDirectory = Path.GetDirectoryName(scriptPath);
            const string mountPath = "/inputs";
            Volume volume = new Volume(hostScriptDirectory, mountPath);

            string scriptName = Path.GetFileName(scriptPath);

            // Don't use Path.Combine(), as this will insert backslashes on windows.
            string entrypoint = $"{mountPath}/{scriptName}";

            // Set the APSIM_NO_DOCKER environment variable in the container,
            // to ensure that the container doesn't attempt to recursively use
            // docker to run the file.
            Dictionary<string, string> env = new Dictionary<string, string>()
            {
                { skipDockerVariable, "1" }
            };

            Docker docker = new Docker(outputHandler, warningHandler, errorHandler);
            await docker.PullImageAsync(apsimCompleteImageName, "latest", cancelToken);
            await docker.RunContainerAsync(apsimCompleteImageName, "Rscript", arguments.Prepend(entrypoint), volume.ToReadOnlyList(), env, mountPath, cancelToken);
        }

        /// <summary>
        /// Should docker be used to run R code?
        /// </summary>
        public static bool UseDocker()
        {
            if (Environment.GetEnvironmentVariable(skipDockerVariable) == "1")
                return false;
            return true;
        }
    }
}
