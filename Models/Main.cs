// ----------------------------------------------------------------------
// <copyright file="Main.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    /// <summary>Class to hold a static main entry point.</summary>
    public class Program
    {
        private static string errors;

        /// <summary>
        /// Main program entry point.
        /// </summary>
        /// <param name="args"> Command line arguments</param>
        /// <returns> Program exit code (0 for success)</returns>
        public static int Main(string[] args)
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "ApsimX");
            Directory.CreateDirectory(tempFolder);
            Environment.SetEnvironmentVariable("TMP", tempFolder, EnvironmentVariableTarget.Process);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Manager.ResolveManagerAssembliesEventHandler);

            int exitCode = 0;
            try
            {
                string fileName = null;

                // Extract file name from command line arguments.
                if (args.Length >= 1)
                    fileName = args[0];

                if (args.Length < 1 || args.Length > 4)
                    throw new Exception("Usage: ApsimX ApsimXFileSpec [/Recurse] [/SingleThreaded] [/RunTests]");

                Stopwatch timer = new Stopwatch();
                timer.Start();

                // If the filename argument has a wildcard then create a IJobManager to go look for matching files to run.
                // Otherwise, create a JobManager to open the filename and run it in a separate, external process
                IJobManager job;
                if (fileName.Contains('*') || fileName.Contains('?'))
                    job = Runner.ForFolder(fileName, args.Contains("/Recurse"), args.Contains("/RunTests"));
                else
                    job = Runner.ForFile(fileName, args.Contains("/RunTests"));

                // Run the job created above using either a single thread or multi threaded (default)
                IJobRunner jobRunner;
                if (args.Contains("/SingleThreaded"))
                    jobRunner = new JobRunnerSync();
                else
                    jobRunner = new JobRunnerAsync();
                jobRunner.JobCompleted += OnJobCompleted;
                jobRunner.Run(job, wait: true);

                // If errors occurred, write them to the console.
                if (errors != null)
                {
                    Console.WriteLine("ERRORS FOUND!!");
                    Console.WriteLine(errors);
                    exitCode = 1;
                }
                else
                    exitCode = 0;

                timer.Stop();
                Console.WriteLine("Finished running simulations. Duration " + timer.Elapsed.TotalSeconds.ToString("#.00") + " sec.");
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                exitCode = 1;
            }

            return exitCode;
        }

        /// <summary>Job has completed</summary>
        private static void OnJobCompleted(object sender, JobCompleteArgs e)
        {
            if (e.exceptionThrowByJob != null)
                errors += e.exceptionThrowByJob.ToString() + Environment.NewLine;
        }
                
    }
}