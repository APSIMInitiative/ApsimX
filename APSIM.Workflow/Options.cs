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

            /// <summary> Github author ID for the pull request. </summary>
            [Option('g',"githubauthorid", Required = false, HelpText = "The pull requests author GitHub username")]
            public string GitHubAuthorID {get;set;} = "";

            /// <summary> Docker image tag for a pull request used to validate data.</summary>
            [Option('t', "tag", Required = false, HelpText = "The docker image tag to use.")]
            public string DockerImageTag { get; set; } = "latest";

            /// <summary>File to split</summary>
            [Option('s', "splitfiles", Required = false, HelpText = "Apsimx file to split.")]
            public string SplitFiles { get; set; }
        }
    }
