    using System;
    using CommandLine;

    namespace APSIM.Workflow
    {
        /// <summary>
        /// Specifies the command line options for the APSIM.Workflow application.
        /// </summary>
        public class Options
        {
            /// <summary>
            /// Gets or sets a value indicating whether verbose output is enabled.
            /// </summary>
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            /// <summary>
            /// Gets or sets the input file to be processed.
            /// </summary>
            [Value(0, HelpText = "A directory string where the directory contains an apsimx file, excel input files (optional), and a WorkFlo yml file. Must be first argument.", MetaName = "directory string", Required = true)]
            public required string DirectoryPath { get; set; }
        }
    }
