using CommandLine;

namespace APSIM.Workflow.Verbs;

/// <summary>Command line options for the azure-start-pool verb.</summary>
[Verb("azure-start-pool", HelpText = "Start a fixed size Azure pool so it is ready for jobs")]
public class AzureStartPoolOptions
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
}