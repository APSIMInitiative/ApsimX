namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System;

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

            // Sort the files from longest running at index 0 and shortest running at bottom of list.
            files.Sort(new FileRunTimeComparer());

            // For each .apsimx file - read it in and create a job for each simulation it contains.
            string workingDirectory = Directory.GetCurrentDirectory();
            string binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string apsimExe = Path.Combine(binDirectory, "Models.exe");
            List<JobManager.IRunnable> jobs = new List<JobManager.IRunnable>();
            foreach (string apsimxFileName in files)
            {
                string arguments = StringUtilities.DQuote(apsimxFileName);
                if (runTests)
                    arguments += " /RunTests";
                JobManager.IRunnable job = new RunExternal(apsimExe, arguments, workingDirectory);
                jobManager.AddChildJob(this, job);
                jobs.Add(job);
            }

            // Wait for all jobs to complete.
            do
            {
                Thread.Sleep(500);
            }
            while (!jobManager.AreChildJobsComplete(this));

            // Write all job elapsed times so that next time this gets called we can do the long running
            // jobs first.
            WriteElapsedTimes(files, jobManager, jobs);
        }

        
        /// <summary>
        /// Write all job elapsed times so that next time this gets called we can do the long running
        /// jobs first.
        /// </summary>
        /// <param name="files">The file names of the .apsim files.</param>
        /// <param name="jobManager">Job manager.</param>
        /// <param name="jobs">The jobs for each file name that have been run.</param>
        private void WriteElapsedTimes(List<string> files, JobManager jobManager, List<JobManager.IRunnable> jobs)
        {
            if (files.Count != jobs.Count)
                throw new Exception("The number of files doesn't match the number of jobs.");

            List<Configuration.FileRunTime> runtimes = new List<Core.Configuration.FileRunTime>();
            for (int i = 0; i < files.Count; i++)
            {
                runtimes.Add(new Configuration.FileRunTime()
                {
                    fileName = files[i],
                    elapsedTime = jobManager.ElapsedTime(jobs[i])
                });
            }
            Configuration.Settings.RunTimes = runtimes;
        }


        /// <summary>
        /// 
        /// </summary>
        private class FileRunTimeComparer : IComparer<string>
        {
            /// <summary>
            /// Compares two file names and returns a value indicating whether one is less than, equal to, or greater than the other
            /// in terms of how long they take to run.
            /// </summary>
            /// <param name="fileName1">The first filename</param>
            /// <param name="fileName2">The second filename.</param>
            public int Compare(string fileName1, string fileName2)
            {
                Configuration.FileRunTime runtime1 = Configuration.Settings.RunTimes.Find(r => r.fileName == fileName1);
                Configuration.FileRunTime runtime2 = Configuration.Settings.RunTimes.Find(r => r.fileName == fileName2);
                if (runtime1 == null && runtime2 == null)
                    return 0;
                else if (runtime1 != null && runtime2 != null)
                    return runtime2.elapsedTime.CompareTo(runtime1.elapsedTime);
                else if (runtime1 != null && runtime2 == null)
                    return 1;
                else
                    return -1;
            }

        }
    }
}
