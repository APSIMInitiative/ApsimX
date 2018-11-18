namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// This runnable class finds .apsimx files on the 'fileSpec' passed into
    /// the constructor. If 'recurse' is true then it will also recursively
    /// look for files in sub directories.
    /// </summary>
    class RunDirectoryOfApsimFiles : IJobManager
    {
        /// <summary>Gets or sets the filespec that we will look for.</summary>
        private string fileSpec;

        /// <summary>Run the test nodes?</summary>
        private bool runTests;

        /// <summary>Search recursively for files?</summary>
        private bool recurse;

        /// <summary>Should the child process' output be redirected?</summary>
        private bool verbose;

        /// <summary>Should the child processes be run in multi-process mode?</summary>
        private bool multiProcess;

        /// <summary>List of files found that need running</summary>
        private List<string> files;

        /// <summary>Constructor</summary>
        /// <param name="fileSpec">The filespec to search for simulations.</param>
        /// <param name="recurse">True if need to recurse through folder structure.</param>
        /// <param name="runTests">Run the test nodes?</param>
        /// <param name="verbose">Should the child process' output be redirected?</param>
        /// <param name="multiProcess">Should the child processes be run in multi-process mode?</param>
        public RunDirectoryOfApsimFiles(string fileSpec, bool recurse, bool runTests, bool verbose, bool multiProcess)
        {
            this.fileSpec = fileSpec;
            this.recurse = recurse;
            this.runTests = runTests;
            this.verbose = verbose;
            this.multiProcess = multiProcess;
        }

        /// <summary>Return the index of next job to run or -1 if nothing to run.</summary>
        /// <returns>Job to run or null if no more</returns>
        public IRunnable GetNextJobToRun()
        {
            if (files == null)
            {
                // Extract the path from the filespec. If non specified then assume
                // current working directory.
                string path = Path.GetDirectoryName(fileSpec);
                if (path == null | path == "")
                    path = Directory.GetCurrentDirectory();

                files = Directory.GetFiles(
                    path,
                    Path.GetFileName(fileSpec),
                    recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();

                // See above. FIXME!
                files.RemoveAll(s => s.Contains("UnitTests"));
                files.RemoveAll(s => s.Contains("UserInterface"));
                files.RemoveAll(s => s.Contains("ApsimNG"));
            }

            if (files.Count == 0)
                return null;

            // For each .apsimx file - read it in and create a job for each simulation it contains.
            string workingDirectory = Directory.GetCurrentDirectory();
            string binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string apsimExe = Path.Combine(binDirectory, "Models.exe");

            string arguments = StringUtilities.DQuote(files[0]);
            files.RemoveAt(0);
            if (multiProcess)
                arguments += " /m";
            if (runTests)
                arguments += " /RunTests";
            if (verbose)
                arguments += " /Verbose";
            return new RunExternal(apsimExe, arguments, workingDirectory, verbose);
        }

        /// <summary>Called by the job runner when all jobs completed</summary>
        public void Completed()
        {
        }
    }
}
