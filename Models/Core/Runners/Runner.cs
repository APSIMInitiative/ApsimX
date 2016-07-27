namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using Factorial;
    using Runners;
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
        public static void CallOnLoaded(IModel model)
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
        public static bool AreSomeJobsRunning(List<JobManager.IRunnable> jobs, JobManager jobManager)
        {
            foreach (JobManager.IRunnable job in jobs)
            {
                if (!jobManager.IsJobCompleted(job))
                    return true;
            }
            return false;
        }






    }
}
