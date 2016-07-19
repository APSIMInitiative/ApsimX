namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Gets a run job for running one or more simulations.
    /// </summary>
    public class Runner
    {
        /// <summary>Runs the specified simulations.</summary>
        /// <param name="model">Simulations to run.</param>
        /// <param name="simulations">Simulations model.</param>
        /// <param name="runTests">Run the test nodes?</param>
        /// <returns>A runnable job or null if nothing to run.</returns>
        public static JobManager.IRunnable ForSimulations(Simulations simulations, Model model, bool runTests)
        {
            return new RunOrganiser(simulations, model, runTests);
        }

        /// <summary>Run simulations in files specified by a file specification.</summary>
        /// <param name="fileSpec">The file specification</param>
        /// <param name="recurse">Recurse throug sub directories?</param>
        /// <param name="runTests">Run the test nodes?</param>
        /// <returns>The file of jobs that were run.</returns>
        public static JobManager.IRunnable ForFolder(string fileSpec, bool recurse, bool runTests)
        {
            return new RunDirectoryOfApsimFiles(fileSpec, recurse, runTests);
        }

        /// <summary>Run simulations in files specified by a file specification.</summary>
        /// <param name="fileName">The file specification</param>
        /// <param name="runTests">Run the test nodes?</param>
        /// <returns>The file of jobs that were run.</returns>
        public static JobManager.IRunnable ForFile(string fileName, bool runTests)
        {
            if (!File.Exists(fileName))
                throw new Exception("Cannot find file: " + fileName);

            Simulations simulations =Simulations.Read(fileName);
            return ForSimulations(simulations, simulations, runTests);
        }


        /// <summary>Are some jobs still running?</summary>
        /// <param name="jobs">The jobs to check.</param>
        /// <returns>True if all completed.</returns>
        private static bool AreSomeJobsRunning(List<JobManager.IRunnable> jobs)
        {
            foreach (JobManager.IRunnable job in jobs)
                if (!job.IsCompleted)
                    return true;
            return false;
        }



        /// <summary>Organises the running of a collection of jobs.</summary>
        private class RunOrganiser : JobManager.IRunnable
        {
            private Simulations simulations;
            private Model model;
            private bool runTests;

            /// <summary>Constructor</summary>
            /// <param name="model">The model to run.</param>
            /// <param name="simulations">simulations object.</param>
            /// <param name="runTests">Run the test nodes?</param>
            public RunOrganiser(Simulations simulations, Model model, bool runTests)
            {
                this.simulations = simulations;
                this.model = model;
                this.runTests = runTests;
            }

            /// <summary>Error message goes here. Set by JobMangager.</summary>
            public string ErrorMessage { get; set; }

            /// <summary>Is completed flag. Set by JobMangager.</summary>
            public bool IsCompleted { get; set; }

            /// <summary>Signal that this job isn't time consuming</summary>
            public bool IsComputationallyTimeConsuming { get { return false; } }

            /// <summary>Number of sims running.</summary>
            public int numRunning { get; private set; }

            /// <summary>Run the jobs.</summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            public void Run(object sender, System.ComponentModel.DoWorkEventArgs e)
            {
                JobManager jobManager = e.Argument as JobManager;

                List<JobManager.IRunnable> jobs = new List<JobManager.IRunnable>();

                DataStore store = Apsim.Child(simulations, typeof(DataStore)) as DataStore;

                Simulation[] simulationsToRun;
                if (model is Simulations)
                {
                    // As we are going to run all simulations, we can delete all tables in the DataStore. This
                    // will clean up order of columns in the tables and removed unused ones.
                    store.DeleteAllTables();
                    simulationsToRun = Simulations.FindAllSimulationsToRun(simulations);
                }
                else
                {
                    store.RemoveUnwantedSimulations(simulations);

                    if (model is Simulation)
                    {
                        if (model.Parent == null)
                        {
                            // model is already a cloned simulation, probably from user running a single 
                            // simulation from an experiment.
                            simulationsToRun = new Simulation[1] { model as Simulation };
                        }
                        else
                        {
                            simulationsToRun = new Simulation[1] { Apsim.Clone(model as Simulation) as Simulation };
                            Simulations.CallOnLoaded(simulationsToRun[0]);
                        }
                    }
                    else
                        simulationsToRun = Simulations.FindAllSimulationsToRun(model);
                }

                store.Disconnect();

                simulations.MakeSubstitutions(simulationsToRun);

                foreach (Simulation simulation in simulationsToRun)
                {
                    jobs.Add(simulation);
                    jobManager.AddJob(simulation);
                }

                // Wait until all our jobs are all finished.
                while (AreSomeJobsRunning(jobs))
                    Thread.Sleep(200);

                // Collect all error messages.
                foreach (Simulation job in simulationsToRun)
                {
                    if (job.ErrorMessage != null)
                        ErrorMessage += job.ErrorMessage + Environment.NewLine;
                }

                // <summary>Call the all completed event in all models.</summary>
                object[] args = new object[] { this, new EventArgs() };
                foreach (Model childModel in Apsim.ChildrenRecursively(simulations))
                {
                    try
                    {
                        Apsim.CallEventHandler(childModel, "AllCompleted", args);
                    }
                    catch (Exception err)
                    {
                        ErrorMessage += "Error in file: " + simulations.FileName + Environment.NewLine;
                        ErrorMessage += err.ToString() + Environment.NewLine + Environment.NewLine;
                    }
                }

                // Optionally run the test nodes.
                if (runTests)
                {
                    foreach (Tests tests in Apsim.ChildrenRecursively(simulations, typeof(Tests)))
                    {
                        try
                        {
                            tests.Test();
                        }
                        catch (Exception err)
                        {
                            ErrorMessage += "Error in file: " + simulations.FileName + Environment.NewLine;
                            ErrorMessage += err.ToString() + Environment.NewLine + Environment.NewLine;
                        }
                    }
                }

                if (ErrorMessage != null)
                    throw new Exception(ErrorMessage);
            }
        }

        /// <summary>
        /// This runnable class finds .apsimx files on the 'fileSpec' passed into
        /// the constructor. If 'recurse' is true then it will also recursively
        /// look for files in sub directories.
        /// </summary>
        private class RunDirectoryOfApsimFiles : JobManager.IRunnable
        {
            /// <summary>Gets or sets the filespec that we will look for.</summary>
            private string fileSpec;

            /// <summary>Run the test nodes?</summary>
            private bool runTests;

            /// <summary>Gets or sets a value indicating whether we search recursively for files matching </summary>
            private bool recurse;

            /// <summary>Gets a value indicating whether this job is completed. Set by JobManager.</summary>
            public bool IsCompleted { get; set; }

            /// <summary>Gets the error message. Can be null if no error. Set by JobManager.</summary>
            public string ErrorMessage { get; set; }

            /// <summary>Gets a value indicating whether this instance is computationally time consuming.</summary>
            public bool IsComputationallyTimeConsuming { get { return false; } }

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

            /// <summary>Run this job.</summary>
            /// <param name="sender">A system telling us to go </param>
            /// <param name="e">Arguments to same </param>
            public void Run(object sender, System.ComponentModel.DoWorkEventArgs e)
            {
                // Extract the path from the filespec. If non specified then assume
                // current working directory.
                string path = Path.GetDirectoryName(fileSpec);
                if (path == null | path == "")
                {
                    path = Directory.GetCurrentDirectory();
                }

                List<string> files = Directory.GetFiles(
                    path,
                    Path.GetFileName(fileSpec),
                    recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();

                // See above. FIXME!
                files.RemoveAll(s => s.Contains("UnitTests"));
                files.RemoveAll(s => s.Contains("UserInterface"));
                files.RemoveAll(s => s.Contains("ApsimNG"));

                // Get a reference to the JobManager so that we can add jobs to it.
                JobManager jobManager = e.Argument as JobManager;

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
                    jobs.Add(job);
                    jobManager.AddJob(job);
                }

                // Wait until all our jobs are all finished.
                while (AreSomeJobsRunning(jobs))
                    Thread.Sleep(200);

                // Collect all error messages.
                foreach (JobManager.IRunnable job in jobs)
                    if (job.ErrorMessage != null)
                        ErrorMessage += job.ErrorMessage + Environment.NewLine;

                if (ErrorMessage != null)
                    throw new Exception(ErrorMessage);
            }
        }




        /// <summary>
        /// This runnable class runs an external process.
        /// </summary>
        private class RunExternal : JobManager.IRunnable
        {
            /// <summary>Gets a value indicating whether this instance is computationally time consuming.</summary>
            public bool IsComputationallyTimeConsuming { get { return true; } }

            /// <summary>Gets or sets the error message. Set by the JobManager.</summary>
            public string ErrorMessage { get; set; }

            /// <summary>Gets or sets a value indicating whether this job is completed. Set by the JobManager.</summary>
            public bool IsCompleted { get; set; }

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
            /// <param name="sender">The sender.</param>
            /// <param name="e">The event data.</param>
            public void Run(object sender, System.ComponentModel.DoWorkEventArgs e)
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
                    ErrorMessage = "Error in file: " + arguments + Environment.NewLine;
                    ErrorMessage += stdout;
                    throw new Exception(ErrorMessage);
                }
            }
        }
    }
}
