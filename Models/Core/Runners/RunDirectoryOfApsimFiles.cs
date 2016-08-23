namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// This runnable class finds .apsimx files on the 'fileSpec' passed into
    /// the constructor. If 'recurse' is true then it will also recursively
    /// look for files in sub directories.
    /// </summary>
    class RunDirectoryOfApsimFiles : JobManager.IRunnable
    {
        /// <summary>Gets or sets the filespec that we will look for.</summary>
        private string fileSpec;

        /// <summary>Run the test nodes?</summary>
        private bool runTests;

        /// <summary>Gets or sets a value indicating whether we search recursively for files matching </summary>
        private bool recurse;

        /// <summary>Constructor</summary>
        /// <param name="fileSpec">The filespec to search for simulations.</param>
        /// <param name="recurse">True if need to recurse through folder structure.</param>
        /// <param name="runTests">Run the test nodes?</param>
        public RunDirectoryOfApsimFiles(string fileSpec, bool recurse, bool runTests)
        {
            this.fileSpec = fileSpec;
            this.recurse = recurse;
            this.runTests = runTests;
        }

        /// <summary>Called to start the job.</summary>
        /// <param name="jobManager">The job manager running this job.</param>
        /// <param name="workerThread">The thread this job is running on.</param>
        public void Run(JobManager jobManager, BackgroundWorker workerThread)
        {
            // Extract the path from the filespec. If non specified then assume
            // current working directory.
            string path = Path.GetDirectoryName(fileSpec);
            if (path == null | path == "")
                path = Directory.GetCurrentDirectory();

            List<string> files = Directory.GetFiles(
                path,
                Path.GetFileName(fileSpec),
                recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();

            // See above. FIXME!
            files.RemoveAll(s => s.Contains("UnitTests"));
            files.RemoveAll(s => s.Contains("UserInterface"));
            files.RemoveAll(s => s.Contains("ApsimNG"));

            // For each .apsimx file - read it in and create a job for each simulation it contains.
            string workingDirectory = Directory.GetCurrentDirectory();
            string binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string apsimExe = Path.Combine(binDirectory, "Models.exe");
            foreach (string apsimxFileName in files)
            {
                string arguments = StringUtilities.DQuote(apsimxFileName);
                if (runTests)
                    arguments += " /RunTests";
                jobManager.AddChildJob(this, new RunExternal(apsimExe, arguments, workingDirectory));
            }
        }
    }
}
