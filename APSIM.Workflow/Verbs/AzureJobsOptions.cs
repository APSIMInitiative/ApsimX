using CommandLine;

namespace APSIM.Workflow.Verbs;

/// <summary>Command line options for the azure-jobs verb.</summary>
[Verb("azure-jobs", HelpText = "Create an Azure batch pool and submit the validation jobs.")]
public class AzureJobsOptions
{
    /// <summary>Gets or sets the pull request number.</summary>
    [Option('n', "pr-number", Required = true, HelpText = "The pull request number for the workflow.")]
    public string PullRequestNumber { get; set; } = "";

    /// <summary>Gets or sets the full commit hash.</summary>
    [Option("commit-hash", Required = true, HelpText = "The full commit hash.")]
    public string CommitHash { get; set; } = "";

    /// <summary>Gets or sets an env file represented as a string.</summary>
    [Option("env-string", Required = true, HelpText = "An env file represented as a string.")]
    public string EnvString { get; set; } = "";

    /// <summary>Gets or sets the pull request author's GitHub username.</summary>
    [Option('g', "githubauthorid", Required = false, HelpText = "The pull requests author GitHub username")]
    public string GitHubAuthorID { get; set; } = "";

    /// <summary>Gets or sets a value indicating whether verbose output is enabled.</summary>
    [Option("pre-start-pool", Required = false, HelpText = "Add to start just the pool")]
    public bool PreStartPool { get; set; }
}
