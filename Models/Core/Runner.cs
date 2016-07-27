namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using Factorial;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
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

        /// <summary>Make model substitutions if necessary.</summary>
        /// <param name="simulations">The simulations to make substitutions in.</param>
        /// <param name="parentSimulations">Parent simulations object</param>
        public static void MakeSubstitutions(Simulations parentSimulations, List<Simulation> simulations)
        {
            IModel replacements = Apsim.Child(parentSimulations, "Replacements");
            if (replacements != null)
            {
                foreach (IModel replacement in replacements.Children)
                {
                    foreach (Simulation simulation in simulations)
                    {
                        foreach (IModel match in Apsim.FindAll(simulation, replacement.GetType()))
                        {
                            if (match.Name.Equals(replacement.Name, StringComparison.InvariantCultureIgnoreCase))
                            {
                                // Do replacement.
                                IModel newModel = Apsim.Clone(replacement);
                                int index = match.Parent.Children.IndexOf(match as Model);
                                match.Parent.Children.Insert(index, newModel as Model);
                                newModel.Parent = match.Parent;
                                match.Parent.Children.Remove(match as Model);
                                CallOnLoaded(newModel);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Call Loaded event in specified model and all children</summary>
        /// <param name="model">The model.</param>
        private static void CallOnLoaded(IModel model)
        {
            // Call OnLoaded in all models.
            Apsim.CallEventHandler(model, "Loaded", null);
            foreach (Model child in Apsim.ChildrenRecursively(model))
                Apsim.CallEventHandler(child, "Loaded", null);
        }

        /// <summary>Are some jobs still running?</summary>
        /// <param name="jobs">The jobs to check.</param>
        /// <param name="jobManager">The job manager</param>
        /// <returns>True if all completed.</returns>
        private static bool AreSomeJobsRunning(List<JobManager.IRunnable> jobs, JobManager jobManager)
        {
            foreach (JobManager.IRunnable job in jobs)
            {
                if (!jobManager.IsJobCompleted(job))
                    return true;
            }
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

            /// <summary>Number of sims running.</summary>
            public int numRunning { get; private set; }

            /// <summary>Run the organiser. Will throw on error.</summary>
            /// <param name="jobManager">The job manager</param>
            /// <param name="workerThread">The thread this job is running on</param>
            public void Run(JobManager jobManager, BackgroundWorker workerThread)
            {
                List<JobManager.IRunnable> jobs = new List<JobManager.IRunnable>();

                Simulation[] simulationsToRun = FindAllSimulationsToRun(this.model);

                // IF we are going to run all simulations, we can delete all tables in the DataStore. This
                // will clean up order of columns in the tables and removed unused ones.
                // Otherwise just remove the unwanted simulations from the DataStore.
                DataStore store = Apsim.Child(simulations, typeof(DataStore)) as DataStore;
                if (model is Simulations)
                    store.DeleteAllTables();
                else
                    store.RemoveUnwantedSimulations(simulations);
                store.Disconnect();

                foreach (Simulation simulation in simulationsToRun)
                {
                    jobs.Add(simulation);
                    jobManager.AddJob(simulation);
                }

                // Wait until all our jobs are all finished.
                while (AreSomeJobsRunning(jobs, jobManager))
                    Thread.Sleep(200);

                // Collect all error messages.
                string ErrorMessage = null;
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


            /// <summary>Find all simulations under the specified parent model.</summary>
            /// <param name="model">The parent.</param>
            /// <returns></returns>
            private Simulation[] FindAllSimulationsToRun(Model model)
            {
                List<Simulation> simulations = new List<Simulation>();

                if (model is Experiment)
                    simulations.AddRange((model as Experiment).Create());
                else if (model is Simulation)
                {
                    Simulation clonedSim;
                    if (model.Parent == null)
                    {
                        // model is already a cloned simulation, probably from user running a single 
                        // simulation from an experiment.
                        clonedSim = model as Simulation;
                    }
                    else
                        clonedSim = Apsim.Clone(model) as Simulation;

                    MakeSubstitutions(this.simulations, new List<Simulation> { clonedSim });

                    CallOnLoaded(clonedSim);
                    simulations.Add(clonedSim);
                }
                else
                {
                    // Look for simulations.
                    foreach (Model child in Apsim.ChildrenRecursively(model))
                    {
                        if (child is Experiment)
                            simulations.AddRange((child as Experiment).Create());
                        else if (child is Simulation && !(child.Parent is Experiment))
                            simulations.AddRange(FindAllSimulationsToRun(child));
                    }
                }

                // Make sure each simulation has it's filename set correctly.
                foreach (Simulation simulation in simulations)
                {
                    if (simulation.FileName == null)
                        simulation.FileName = RootSimulations(model).FileName;
                }

                return simulations.ToArray();
            }


            /// <summary>Roots the simulations.</summary>
            /// <param name="model">The model.</param>
            /// <returns></returns>
            private static Simulations RootSimulations(Model model)
            {
                Model m = model;
                while (m != null && m.Parent != null && !(m is Simulations))
                    m = m.Parent as Model;

                return m as Simulations;
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

            /// <summary>Run the external process. Will throw on error.</summary>
            /// <param name="jobManager">The job manager</param>
            /// <param name="workerThread">The thread this job is running on</param>
            public void Run(JobManager jobManager, BackgroundWorker workerThread)
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
                while (AreSomeJobsRunning(jobs, jobManager))
                    Thread.Sleep(200);

                // Collect all error messages.
                foreach (JobManager.IRunnable runnableJob in jobs)
                {
                    List<Exception> errors = jobManager.Errors(runnableJob);
                    if (errors.Count > 0)
                        ErrorMessage += errors[0].ToString() + Environment.NewLine;
                }

                if (ErrorMessage != null)
                    throw new Exception(ErrorMessage);
            }
        }




        /// <summary>
        /// This runnable class runs an external process.
        /// </summary>
        class RunExternal : JobManager.IRunnable, JobManager.IComputationalyTimeConsuming
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

            /// <summary>Run the external process. Will throw on error.</summary>
            /// <param name="jobManager">The job manager</param>
            /// <param name="workerThread">The thread this job is running on</param>
            public void Run(JobManager jobManager, BackgroundWorker workerThread)
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
}
