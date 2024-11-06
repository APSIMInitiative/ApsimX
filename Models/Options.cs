using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Models
{
    /// <summary>
    /// Command-line options for Models.exe.
    /// </summary>
    public class Options
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
        [Option("edit", HelpText = "Deprecated. Use --apply switch with config file workflow instead.")]
        public string EditFilePath { get; set; }

        /// <summary>
        /// Edit the .apsimx file(s) before running them. Path to a config file must be specified which contains lines of parameters to change, in the form 'path = value'.
        /// </summary>
        /// <remarks>
        /// This property holds the path to the config file.
        /// This is identical to --edit switch. 
        /// </remarks>
        [Option("run-use-config", HelpText = "Deprecated. Use --apply switch with config file workflow instead.")]
        public string RunUseConfig { get; set; }

        /// <summary>
        ///  Edit the .apsimx files(s) and save without running them. Path to a config file must be specified which contains lines of parameters to change, in the form 'path = value'.
        /// </summary>
        /// <remarks>
        /// This property holds the path to the config file and optionally a path to a .apsimx to save the modified .apsimx file (white-space separated).
        /// </remarks>
        [Option("edit-use-config", HelpText = "Deprecated. Use --apply switch with config file workflow instead.")]
        public string EditUseConfig { get; set; }

        /// <summary>
        /// List simulation names without running them.
        /// </summary>
        [Option("list-simulations", HelpText = "List simulation names without running them.")]
        public bool ListSimulationNames { get; set; }

        /// <summary>
        /// List enabled simulation names without running them.
        /// </summary>
        [Option('e', "list-enabled-simulations", HelpText = "List enabled simulation names without running them.")]
        public bool ListEnabledSimulationNames { get; set; }

        /// <summary>
        /// List all files that are referenced by an .apsimx file(s) with absolute paths.
        /// </summary>
        [Option("list-referenced-filenames", HelpText = "List all files that are referenced by an .apsimx file(s) as an absolute path.")]
        public bool ListReferencedFileNames { get; set; }

        /// <summary>
        /// List all files that are referenced by an .apsimx file(s) as they are. 
        /// </summary>
        [Option("list-referenced-filenames-unmodified", HelpText = "List all files that are referenced by an .apsimx file(s) as is.")]
        public bool ListReferencedFileNamesUnmodified { get; set; }

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
        /// Uses a config file to apply instructions. Can be used to create new simulations and modify existing ones.
        /// </summary>
        /// <remarks>
        /// Intended to provide a overall approach to simulation handling.
        /// </remarks>
        [Option("apply", HelpText = "Uses a config file to apply instructions. Can be used to create new simulations and modify existing ones.")]
        public string Apply { get; set; }

        /// <summary>
        /// Allows a group of simulations to be selectively run. Requires a playlist node to be present in the APSIM file.
        /// </summary>
        [Option('p', "playlist", HelpText = "Allows a group of simulations to be selectively run. Requires a playlist node to be present in the APSIM file.")]
        public string Playlist { get; set; }

        /// <summary>
        /// Sets the verbosity level of all summary files.
        /// </summary>
        [Option('l', "log", HelpText = "Sets the verbosity level of all summary nodes in file(s).")]
        public string Log { get; set; }

        /// <summary>
        /// Sets Simulations to use in memory database rather than database files.
        /// </summary>
        [Option("in-memory-db", HelpText = "Sets datastore to use memory instead of database." )]
        public bool InMemoryDB {get; set;}

        /// <summary>
        /// Allows the use of a batch file which specifies a series of changes to make to an apsimx file. Used in conjunction with --apply switch.
        /// </summary>
        [Option('b', "batch", HelpText="Allows the use of a batch file which specifies a series of changes to make to an apsimx file. To be used with the --apply switch.")]
        public string Batch{ get; set; }

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
                                         new Options()
                                         {
                                             Files = new[] { "file.apsimx", "file2.apsimx" }
                                         });
                yield return new Example("Run all files under a directory, recursively",
                                         new Options()
                                         {
                                             Files = new[] { "dir/*.apsimx" },
                                             Recursive = true,
                                         });
                yield return new Example("Edit a file before running it",
                                         new Options()
                                         {
                                             Files = new[] { "/path/to/file.apsimx" },
                                             EditFilePath = "/path/to/config/file.txt"
                                         });
                yield return new Example("Reconfigure a file with a config file",
                                         new Options()
                                         {
                                             Files = new[] { "/path/to/file.apsimx" },
                                             Apply = "/path/to/config/file.txt"
                                         });
            }
        }
    }
}