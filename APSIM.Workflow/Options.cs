using CommandLine;

namespace APSIM.Workflow;

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
    [Option('d', "payload-directory", Required = false, HelpText = "Directory path where a WorkFlo payload directory is located. Will typically contain a workflow.yml and .env file.")]
    public string DirectoryPath { get; set; } = "";

    /// <summary>
    /// Gets or sets a value indicating whether the program should print the absolute paths of valid validation directories.
    /// </summary>
    [Option('l', "locations", Required = false, HelpText = "Print the absolute paths of valid validation directories.")]
    public bool ValidationLocations { get; set; }

    /// <summary> Github author ID for the pull request. </summary>
    [Option('g', "githubauthorid", Required = false, HelpText = "The pull requests author GitHub username")]
    public string GitHubAuthorID { get; set; } = "";

    /// <summary> Docker image tag for a pull request used to validate data.</summary>
    [Option('t', "tag", Required = false, HelpText = "The docker image tag to use.")]
    public string DockerImageTag { get; set; } = "latest";

    /// <summary>File to split</summary>
    [Option('s', "splitfiles", Required = false, HelpText = "Apsimx file to split.")]
    public string SplitFiles { get; set; }

    /// <summary>
    /// Gets or sets the path to the APSIMX file in a docker container.
    /// </summary>
    [Option('p', "validation-path", Required = false, HelpText = "The path to a directory containing APSIMX files in the docker container.")]
    public string ValidationPath { get; set; } = "";

    /// <summary>
    /// Gets or sets the commit SHA to use for the workflow.
    /// This is typically the SHA of the commit that triggered the workflow run.
    /// </summary>
    [Option('c', "commit-sha", Required = false, HelpText = "The commit SHA to use for the workflow.")]
    public string CommitSHA { get; set; } = "";

    /// <summary>
    /// Gets or sets the pull request number for the workflow.
    /// This is typically the number of the pull request that triggered the workflow run.
    /// </summary>
    [Option('n', "pr-number", Required = false, HelpText = "The pull request number for the workflow.")]
    public string PullRequestNumber { get; set; } = "";

    /// <summary> Gets the number of simulations/validation locations available. </summary>
    [Option("sim-count", Required = false, HelpText = "The number of simulations/validation locations available.")]
    public string SimulationCount { get; set; } = "";

}
    
