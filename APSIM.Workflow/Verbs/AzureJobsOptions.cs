using CommandLine;

namespace APSIM.Workflow.Verbs;

/// <summary>Command line options for the azure-jobs verb.</summary>
[Verb("azure-jobs", HelpText = "Create an Azure batch pool and submit the validation jobs.")]
public class AzureJobsOptions
{
    /// <summary>Gets or sets the pull request number.</summary>
    [Option('n', "pr-number", Required = true, HelpText = "The pull request number for the workflow.")]
    public string PullRequestNumber { get; set; } = "";

    /// <summary>Gets or sets the commit SHA used to name the Azure pool.</summary>
    [Option('c', "commit-sha", Required = true, HelpText = "The commit SHA to use for the workflow.")]
    public string CommitSHA { get; set; } = "";

    /// <summary>Gets or sets an env file represented as a string.</summary>
    [Option("env-string", Required = true, HelpText = "An env file represented as a string.")]
    public string EnvString { get; set; } = "";

    /// <summary>Gets or sets the full commit hash.</summary>
    [Option("full-commit-hash", Required = false, HelpText = "The full commit hash. Used for PO Stats system.")]
    public string FullCommitHash { get; set; } = "";

    /// <summary>Gets or sets the pull request author's GitHub username.</summary>
    [Option('g', "githubauthorid", Required = false, HelpText = "The pull requests author GitHub username")]
    public string GitHubAuthorID { get; set; } = "";
}
