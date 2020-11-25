using System;
using System.Collections.Generic;
using CommandLine;

namespace Models
{
    /// <summary>
    /// Command-line options for Models.exe.
    /// </summary>
    public class Options
    {
        /// <summary>Files to be run.</summary>
        [Value(0, HelpText = ".apsimx file(s) to be run.", MetaName = "ApsimXFileSpec")]
        public IEnumerable<string> Files { get; set; }

        /// <summary>
        /// Recursively search through subdirectories for files matching the file specification.
        /// </summary>
        [Option("recursive", HelpText = "Recursively search through subdirectories for files matching the file specification.")]
        public bool Recursive { get; set; }

        /// <summary>
        /// Upgrade a file to the latest version of the .apsimx file format without running the file.
        /// </summary>
        [Option("upgrade", HelpText = "Upgrade a file to the latest version of the .apsimx file format without running the file.")]
        public bool Upgrade { get; set; }

        /// <summary>
        /// After running the file, run all tests inside the file.
        /// </summary>
        [Option("run-tests", HelpText = "After running the file, run all tests inside the file.")]
        public bool RunTests { get; set; }

        /// <summary>
        /// Write detailed messages to stdout when a simulation starts/finishes.
        /// </summary>
        [Option("verbose", HelpText = "Write detailed messages to stdout when a simulation starts/finishes.")]
        public bool Verbose { get; set; }

        /// <summary>
        /// Export all reports to .csv files.
        /// </summary>
        [Option("csv", HelpText = "Export all reports to .csv files.")]
        public bool ExportToCsv { get; set; }

        /// <summary>
        /// Merge multiple .db files into a single .db file.
        /// </summary>
        [Option("merge-db-files", HelpText = "Merge multiple .db files into a single .db file.")]
        public bool MergeDBFiles { get; set; }

        /// <summary>
        /// Edit the .apsimx file(s) wihtout running them. Path to a config file must be specified which contains lines of parameters to change, in the form 'path = value'.
        /// </summary>
        /// <remarks>
        /// This property holds the path to the config file.
        /// </remarks>
        [Option("edit", HelpText = "Edit the .apsimx file(s) wihtout running them. Path to a config file must be specified which contains lines of parameters to change, in the form 'path = value'.")]
        public string EditFilePath { get; set; }

        /// <summary>
        /// List simulation names without running them.
        /// </summary>
        [Option("list-simulations", HelpText = "List simulation names without running them.")]
        public bool ListSimulationNames { get; set; }

        /// <summary>
        /// Run all simulations sequentially on a single thread.
        /// </summary>
        [Option("single-threaded", HelpText = "Run all simulations sequentially on a single thread.")]
        public bool SingleThreaded { get; set; }

        /// <summary>
        /// Use the multi-process job runner.
        /// </summary>
        [Option("multi-process", HelpText = "Use the multi-process job runner.")]
        public bool MultiProcess { get; set; }

        /// <summary>
        /// Maximum number of threads/processes to spawn for running simulations.
        /// </summary>
        [Option("cpu-count", HelpText = "Maximum number of threads/processes to spawn for running simulations.")]
        public int NumProcessors { get; set; }

        /// <summary>
        /// Only run simulations if their names match this regular expression.
        /// </summary>
        /// <value></value>
        [Option("simulation-names", HelpText = "Only run simulations if their names match this regular expression.")]
        public string SimulationNameRegex { get; set; }

        /// <summary>
        /// Type of runner used to run the simulations.
        /// </summary>
        public Models.Core.Run.Runner.RunTypeEnum RunType
        {
            get
            {
                if (SingleThreaded)
                    return Models.Core.Run.Runner.RunTypeEnum.SingleThreaded;
                if (MultiProcess)
                    return Models.Core.Run.Runner.RunTypeEnum.MultiProcess;
                return Models.Core.Run.Runner.RunTypeEnum.MultiThreaded;
            }
        }
    }
}