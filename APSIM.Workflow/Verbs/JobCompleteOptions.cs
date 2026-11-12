using CommandLine;

namespace APSIM.Workflow.Verbs;

/// <summary>
/// Command line options for the job-complete verb, which notifies the A.V.A server
/// that a validation job (a single APSIM file) has completed.
/// </summary>
[Verb("job-complete", HelpText = "Notify the A.V.A server that a job has completed.")]
public class JobCompleteOptions
{
    /// <summary>The pull request number.</summary>
    [Option("pr-number", Required = true, HelpText = "The pull request number.")]
    public string PullRequestNumber { get; set; } = "";

    /// <summary>The full commit hash.</summary>
    [Option("full-commit-hash", Required = true, HelpText = "The full commit hash.")]
    public string FullCommitHash { get; set; } = "";

    /// <summary>Path of the APSIM file that the completed job ran.</summary>
    [Option("job-path", Required = true, HelpText = "Path of the APSIM file the completed job ran.")]
    public string JobPath { get; set; } = "";

    /// <summary>Elapsed time of the job in seconds.</summary>
    [Option("job-time", Required = true, HelpText = "Elapsed time of the job in seconds.")]
    public string JobTime { get; set; } = "";

    /// <summary>Log file whose lines are sent as the job data.</summary>
    [Option("log-file", Required = true, HelpText = "Log file whose lines are sent to the A.V.A server.")]
    public string LogFile { get; set; } = "";
}
