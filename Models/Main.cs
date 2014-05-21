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
                RunDirectoryOfApsimFiles runApsim = new RunDirectoryOfApsimFiles(fileName, commandLineSwitch);

                Stopwatch timer = new Stopwatch();
                timer.Start();

                int numSimulations = 0;
                if (commandLineSwitch == "/SingleThreaded")
                    numSimulations = RunSingleThreaded(fileName);
                else
                {
                    Utility.JobManager jobManager = new Utility.JobManager();
                    jobManager.OnComplete += OnError;
                    jobManager.AddJob(runApsim);
                    jobManager.Start(waitUntilFinished: true);
                    if (jobManager.SomeHadErrors) return 1;

                    // Write out the number of simulations run to the console.
                    numSimulations = jobManager.NumberOfJobs - 1;
                }
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
        /// Run all simulations in the specified 'fileName' single threaded i.e.
        /// don't use the JobManager. Useful for profilling.
        /// </summary>
        private static int RunSingleThreaded(string fileName)
        {
            Simulations simulations = Simulations.Read(fileName);
            // Don't use JobManager - just run the simulations.
            Simulation[] simulationsToRun = Simulations.FindAllSimulationsToRun(simulations);
            foreach (Simulation simulation in simulationsToRun)
                simulation.Run(null, null);
            return simulationsToRun.Length;
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
            public RunDirectoryOfApsimFiles(string fileSpec, string commandLineSwitch)
            {
                FileSpec = fileSpec;
                Recurse = commandLineSwitch == "/Recurse";
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