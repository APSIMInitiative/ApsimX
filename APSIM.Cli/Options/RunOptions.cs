using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace APSIM.Cli.Options
{
    /// <summary>
    /// Command-line options for Models.exe.
    /// </summary>
    [Verb("run", HelpText = "Run one or more files")]
    public class RunOptions
    {
        /// <summary>Files to be run.</summary>
        [Value(0, HelpText = ".apsimx file(s) to be run.", MetaName = "ApsimXFileSpec", Required = true)]
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
        [Option("run-from-list", HelpText = "Provide a file containing the list of APSIM files to run.")]
        public bool RunFromList { get; set; }

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
        /// Edit the .apsimx file(s) before running them. Path to a config file must be specified which contains lines of parameters to change, in the form 'path = value'.
        /// </summary>
        /// <remarks>
        /// This property holds the path to the config file.
        /// </remarks>
        [Option("edit", HelpText = "Edit the .apsimx file(s) before running them. Path to a config file must be specified which contains lines of parameters to change, in the form 'path = value'.")]
        public string EditFilePath { get; set; }

        /// <summary>
        /// List simulation names without running them.
        /// </summary>
        [Option("list-simulations", HelpText = "List simulation names without running them.")]
        public bool ListSimulationNames { get; set; }

        /// <summary>
        /// List all files that are referenced by an .apsimx file(s)
        /// </summary>
        [Option("list-referenced-filenames", HelpText = "List all files that are referenced by an .apsimx file(s).")]
        public bool ListReferencedFileNames { get; set; }

        /// <summary>
        /// Run all simulations sequentially on a single thread.
        /// </summary>
        /// <remarks>
        /// SetName specified to make it incompatible with multi-process switch.
        /// </remarks>
        [Option("single-threaded", HelpText = "Run all simulations sequentially on a single thread.", SetName = "singlethreaded")]
        public bool SingleThreaded { get; set; }

        /// <summary>
        /// Maximum number of threads/processes to spawn for running simulations.
        /// </summary>
        [Option("cpu-count", Default = -1, HelpText = "Maximum number of threads/processes to spawn for running simulations.")]
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
                return Models.Core.Run.Runner.RunTypeEnum.MultiThreaded;
            }
        }

        /// <summary>
        /// Concrete examples shown in help text.
        /// </summary>
        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Normal usage",
                                         new RunOptions()
                                         {
                                             Files = new[] { "file.apsimx", "file2.apsimx" }
                                         });
                yield return new Example("Run all files under a directory, recursively",
                                         new RunOptions()
                                         {
                                             Files = new[] { "dir/*.apsimx"},
                                             Recursive = true,
                                         });
                yield return new Example("Edit a file before running it",
                                         new RunOptions()
                                         {
                                             Files = new[] { "/path/to/file.apsimx" },
                                             EditFilePath = "/path/to/config/file.txt"
                                         });
            }
        }
    }
}