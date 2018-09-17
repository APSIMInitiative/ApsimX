// ----------------------------------------------------------------------
// <copyright file="Main.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Runners;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>Class to hold a static main entry point.</summary>
    public class Program
    {
        private static string errors = string.Empty;

        /// <summary>
        /// List of files to be run.
        /// </summary>
        private static List<string> files = new List<string>();

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

                string usageMessage = "Usage: Models ApsimXFileSpec [/Recurse] [/SingleThreaded] [/RunTests] [/Csv] [/Version] [/?]";
                if (args.Contains("/?"))
                {
                    string detailedHelpInfo = usageMessage;
                    detailedHelpInfo += Environment.NewLine + Environment.NewLine;
                    detailedHelpInfo += "ApsimXFileSpec:          The path to an .apsimx file. May include wildcard.";
                    detailedHelpInfo += Environment.NewLine + Environment.NewLine + "Options:" + Environment.NewLine;
                    detailedHelpInfo += "    /Recurse             Recursively search subdirectories for files matching ApsimXFileSpec" + Environment.NewLine;
                    detailedHelpInfo += "    /SingleThreaded      Run all simulations in a single thread." + Environment.NewLine;
                    detailedHelpInfo += "    /RunTests            Run all tests." + Environment.NewLine;
                    detailedHelpInfo += "    /Csv                 Export all reports to .csv files." + Environment.NewLine;
                    detailedHelpInfo += "    /Version             Display the version number." + Environment.NewLine;
                    detailedHelpInfo += "    /?                   Show detailed help information.";
                    Console.WriteLine(detailedHelpInfo);
                    return 1;
                }

                if (args.Length < 1 || args.Length > 7)
                {
                    Console.WriteLine(usageMessage);
                    return 1;
                }

                if (args.Contains("/Version"))
                {
                    Model m = new Model();
                    Console.WriteLine(m.ApsimVersion);
                    return 0;
                }

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
                if (args.Select(arg => arg.ToLower()).Contains("/csv"))
                {
                    string dir = Path.GetDirectoryName(fileName);
                    if (dir == "")
                        dir = Directory.GetCurrentDirectory();
                    files = Directory.GetFiles(
                        dir,
                        Path.GetFileName(fileName),
                        args.Contains("/Recurse") ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
                    jobRunner.AllJobsCompleted += GenerateCsvFiles;
                }
                jobRunner.JobCompleted += OnJobCompleted;
                jobRunner.AllJobsCompleted += OnAllJobsCompleted;
                jobRunner.Run(job, wait: true);

                // If errors occurred, write them to the console.
                if (errors != string.Empty)
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

        /// <summary>
        /// Generates a .csv file for each .apsimx file that has been run.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void GenerateCsvFiles(object sender, AllCompletedArgs e)
        {
            foreach (string file in files)
            {
                string fileName = Path.ChangeExtension(file, ".db");
                Storage.DataStore storage = new Storage.DataStore(fileName);
                storage.Open(true);
                Report.Report.WriteAllTables(storage, fileName);
                Console.WriteLine("Successfully created csv file " + Path.ChangeExtension(fileName, ".csv"));
            }
        }

        /// <summary>Job has completed</summary>
        private static void OnJobCompleted(object sender, JobCompleteArgs e)
        {
            lock (errors)
            {
                if (e.exceptionThrowByJob != null)
                {
                    if (e.exceptionThrowByJob is RunExternalException)
                        errors += e.exceptionThrowByJob.Message + Environment.NewLine + "----------------------------------------------" + Environment.NewLine;
                    else
                        errors += e.exceptionThrowByJob.ToString() + Environment.NewLine + "----------------------------------------------" + Environment.NewLine;
                }
            }
        }

        /// <summary>All jobs have completed</summary>
        private static void OnAllJobsCompleted(object sender, AllCompletedArgs e)
        {
            lock (errors)
            {
                if (e.exceptionThrown != null)
                    errors += e.exceptionThrown.ToString() + Environment.NewLine + "----------------------------------------------" + Environment.NewLine;
            }
        }

    }
}