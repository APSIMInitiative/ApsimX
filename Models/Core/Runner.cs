namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using Factorial;
    using Runners;
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
    }
}
