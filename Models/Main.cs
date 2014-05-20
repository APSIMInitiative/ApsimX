using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Models.Core;

namespace Models
{
    public class Program
    {
        /// <summary>
        /// Main program entry point.
        /// </summary>
        static int Main(string[] args)
        {
            try
            {
                string fileName = null;
                string commandLineSwitch = null;

                // Check the command line arguments.
                if (args.Length >= 1)
                    fileName = args[0];
                if (args.Length == 2)
                    commandLineSwitch = args[1];
                if (args.Length < 1 || args.Length > 2)
                    throw new Exception("Usage: ApsimX ApsimXFileSpec [/Recurse]");

                // Create a instance of a job that will go find .apsimx files. Then
                // pass the job to a job runner.
                RunDirectoryOfApsimFiles runApsim = new RunDirectoryOfApsimFiles(fileName, commandLineSwitch == "/Recurse");

                Stopwatch timer = new Stopwatch();
                timer.Start();
                Utility.JobManager jobManager = new Utility.JobManager();
                jobManager.OnComplete += OnError;
                jobManager.AddJob(runApsim);
                jobManager.Start(waitUntilFinished:true);
                if (jobManager.SomeHadErrors) return 1;

                // Write out the number of simulations run to the console.
                int numSimulations = jobManager.NumberOfJobs - 1;
                timer.Stop();
                Console.WriteLine("Finished running " + numSimulations.ToString() + " simulations. Duration " + timer.Elapsed.TotalMinutes.ToString("#.00") + " minutes.");
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// When an error is encountered, this handler will be called.
        /// </summary>
        static void OnError(object sender, Utility.JobManager.JobCompleteArgs e)
        {
            if (e.ErrorMessage != null)
                Console.WriteLine(e.ErrorMessage);
        }






        /// <summary>
        /// This runnable class finds .apsimx files on the 'fileSpec' passed into
        /// the constructor. If 'recurse' is true then it will also recursively
        /// look for files in sub directories.
        /// </summary>
        class RunDirectoryOfApsimFiles : Utility.JobManager.IRunnable
        {
            private string FileSpec;
            private bool Recurse;

            /// <summary>
            /// Constructor
            /// </summary>
            public RunDirectoryOfApsimFiles(string fileSpec, bool recurse)
            {
                FileSpec = fileSpec;
                Recurse = recurse;
            }

            /// <summary>
            /// Run this job.
            /// </summary>
            public void Run(object sender, System.ComponentModel.DoWorkEventArgs e)
            {
                // Extract the path from the filespec. If non specified then assume
                // current working directory.
                string path = Path.GetDirectoryName(FileSpec);
                if (path == null)
                    path = Directory.GetCurrentDirectory();

                string fileSpecNoPath = Path.GetFileName(FileSpec);

                List<string> Files;
                if (Recurse)
                    Files = Directory.GetFiles(path, fileSpecNoPath, SearchOption.AllDirectories).ToList();
                else
                    Files = Directory.GetFiles(path, fileSpecNoPath, SearchOption.TopDirectoryOnly).ToList();

                Files.RemoveAll(s => s.Contains("UnitTests"));

                // Get a reference to the JobManager so that we can add jobs to it.
                Utility.JobManager jobManager = e.Argument as Utility.JobManager;

                // For each .apsimx file - read it in and create a job for each simulation it contains.
                foreach (string apsimxFileName in Files)
                {
                    Simulations simulations = Simulations.Read(apsimxFileName);
                    jobManager.AddJob(simulations);
                }
            }
        }


    }
}