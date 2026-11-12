namespace APSIM.Workflow;

/// <summary>
/// Settings shared by the payload, input and WorkFlo file utilities.
/// This is not parsed from the command line; each verb has its own options class.
/// </summary>
public class Options
{
    /// <summary>Gets or sets a value indicating whether verbose output is enabled.</summary>
    public bool Verbose { get; set; }

    /// <summary>Gets or sets the directory where a WorkFlo payload is located.</summary>
    public string DirectoryPath { get; set; } = "";

    /// <summary>Gets or sets the GitHub username of the pull request author.</summary>
    public string GitHubAuthorID { get; set; } = "";

    /// <summary>Gets or sets the docker image tag for a pull request used to validate data.</summary>
    public string DockerImageTag { get; set; } = "latest";

    /// <summary>Gets or sets the commit SHA to use for the workflow.</summary>
    public string CommitSHA { get; set; } = "";

    /// <summary>Gets or sets the pull request number for the workflow.</summary>
    public string PullRequestNumber { get; set; } = "";

    /// <summary>Gets or sets the Azure pool to use for the workflow.</summary>
    public string AzurePool { get; set; } = "workflo-pool";
}
