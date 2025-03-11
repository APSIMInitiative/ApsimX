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
            [Option('d', "directory", Required = false, HelpText = "A directory string where the directory contains an apsimx file, excel input files (optional), and a WorkFlo yml file. Must be first argument.")]
            public string DirectoryPath { get; set; } = "";

            /// <summary>
            /// Gets or sets a value indicating whether the program should print the absolute paths of valid validation directories.
            /// </summary>
            [Option('l', "locations", Required = false, HelpText = "Print the absolute paths of valid validation directories.")]
            public bool ValidationLocations { get; set; }

            [Option('p',"pullrequestid", Required = false, HelpText = "A pull request id string used when submitting performance tests.")]
            public string PullRequestID {get;set;} = "";

            [Option('g',"githubauthorid", Required = false, HelpText = "The pull requests author GitHub username")]
            public string GitHubAuthorID {get;set;} = "";
        }
    }
