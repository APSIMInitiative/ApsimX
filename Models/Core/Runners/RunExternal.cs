namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// This runnable class runs an external process.
    /// </summary>
    class RunExternal : IRunnable, IComputationalyTimeConsuming
    {
        /// <summary>Gets or sets the executable file.</summary>
        private string executable;

        /// <summary>Gets or sets the working directory.</summary>
        private string workingDirectory;

        /// <summary>The arguments.</summary>
        private string arguments;

        /// <summary>Initializes a new instance of the <see cref="RunExternal"/> class.</summary>
        /// <param name="executable">Name of the executable file.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="workingDirectory">The working directory.</param>
        public RunExternal(string executable, string arguments, string workingDirectory)
        {
            this.executable = executable;
            this.workingDirectory = workingDirectory;
            this.arguments = arguments;
        }

        /// <summary>Called to start the job.</summary>
        /// <param name="cancelToken">The thread this job is running on.</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            // Start the external process to run APSIM and wait for it to finish.
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = executable;
            if (!File.Exists(p.StartInfo.FileName))
                throw new Exception("Cannot find executable: " + p.StartInfo.FileName);
            p.StartInfo.Arguments = arguments;
            p.StartInfo.WorkingDirectory = workingDirectory;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            string stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode > 0)
            {
                string errorMessage = "Error in file: " + arguments + Environment.NewLine;
                errorMessage += stdout;
                throw new Exception(errorMessage);
            }
        }
    }
}
