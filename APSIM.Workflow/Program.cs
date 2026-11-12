using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Diagnostics;
using Humanizer;
using APSIM.Workflow.Verbs;
using System.Threading;
using System.Globalization;

namespace APSIM.Workflow;

/// <summary>
/// Main program class for the APSIM.Workflow application.
/// </summary>
public class Program
{
    /// <summary>Exit code for the application.</summary>
    private static int exitCode = 0;

    /// <summary>List of APSIM file paths to be processed.</summary>
    public static List<string> apsimFilePaths = new();

    private static ILogger<Program> logger;

    /// <summary> Main entry point for APSIM.WorkFlow</summary>
    /// <param name="args">command line arguments</param>
    public static int Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options =>
            {
                options.FormatterName = MinimalConsoleFormatter.FormatterName;
            });
            builder.AddConsoleFormatter<MinimalConsoleFormatter, ConsoleFormatterOptions>();
        });
        logger = loggerFactory.CreateLogger<Program>();

        Parser.Default.ParseArguments<LocationsOptions, SimCountOptions, SplitOptions, AzureJobsOptions, JobCompleteOptions>(args)
            .WithParsed<LocationsOptions>(RunLocations)
            .WithParsed<SimCountOptions>(RunSimCount)
            .WithParsed<SplitOptions>(RunSplit)
            .WithParsed<AzureJobsOptions>(RunAzureJobs)
            .WithParsed<JobCompleteOptions>(RunJobComplete)
            .WithNotParsed(HandleParseError);

        return exitCode;
    }

    /// <summary>Notifies the A.V.A server that a job has completed.</summary>
    /// <param name="options">The job complete options.</param>
    private static void RunJobComplete(JobCompleteOptions options)
    {
        try
        {
            string payload = AvaUtilities.BuildPayload(
                options.PullRequestNumber,
                options.FullCommitHash,
                options.JobPath,
                options.JobTime,
                AvaUtilities.ReadLogLines(options.LogFile));
            using var client = new System.Net.Http.HttpClient();
            AvaUtilities.PostAsync(client, payload, AvaUtilities.JobCompleteRoute).GetAwaiter().GetResult();
            logger.LogInformation("A.V.A server notified that the job is complete.");
        }
        catch (Exception ex)
        {
            logger.LogError("Error: " + ex.Message);
            exitCode = 1;
        }
    }


    /// <summary>Prints the absolute paths of the validation files.</summary>
    /// <param name="options">The locations options.</param>
    private static void RunLocations(LocationsOptions options)
    {
        try
        {
            if (options.Verbose)
                logger.LogInformation("Validation locations:");

            foreach (string dir in RemoveExcludedPaths(ValidationLocationUtility.GetValidationFilePaths()))
                Console.WriteLine(dir);
        }
        catch (Exception ex)
        {
            logger.LogError("Error: " + ex.Message);
            exitCode = 1;
        }
    }

    /// <summary>Prints the number of simulations/validation locations available.</summary>
    /// <param name="options">The simulation count options.</param>
    private static void RunSimCount(SimCountOptions options)
    {
        try
        {
            Console.WriteLine(ValidationLocationUtility.GetSimulationCount());
        }
        catch (Exception ex)
        {
            logger.LogError("Error: " + ex.Message);
            exitCode = 1;
        }
    }

    /// <summary>Splits an APSIM file. Currently disabled.</summary>
    /// <param name="options">The split options.</param>
    private static void RunSplit(SplitOptions options)
    {
        // FileSplitter.Run(options.DirectoryPath, options.SplitFile, Path.GetDirectoryName(options.DirectoryPath) + "/", logger);
    }

    /// <summary>Creates the Azure pool and submits the validation jobs.</summary>
    /// <param name="options">The Azure jobs options.</param>
    private static void RunAzureJobs(AzureJobsOptions options)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        try
        {
            string[] validationPaths = RemoveExcludedPaths(ValidationLocationUtility.GetValidationFilePaths());
            if (validationPaths.Length < 1)
                throw new Exception("A list of validation paths must be provided to continue.");

            // Create a pool name using the PR number and commit SHA.
            string shortCommitSha = options.CommitSHA.Length > 6 ? options.CommitSHA[..6] : options.CommitSHA;
            string poolName = $"{options.PullRequestNumber}-{shortCommitSha}";
            string nowDateString = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
            string jobName = $"{nowDateString}-acceptance-tests-pr-{options.PullRequestNumber}";

            // Create the environment variable dictionary for use further on
            // from the environment variable string.
            Dictionary<string, string> envDict = [];
            foreach (string line in options.EnvString.Split(
                [ "\r\n", "\n" ],
                StringSplitOptions.RemoveEmptyEntries))
            {
                // Split only on the first '=' since values (e.g. base64 keys) may themselves contain '='.
                string[] values = line.Split('=', 2);
                envDict.Add(values[0], values[1]);
            }

            // Add PO Stats specific environment variables
            envDict.Add("AUTHOR", options.GitHubAuthorID);
            envDict.Add("COMMIT_SHA", shortCommitSha);
            envDict.Add("FULL_COMMIT_HASH", options.FullCommitHash);

            Azure.CreatePool(envDict["AZURE_PRIMARY_ACCESS_KEY"], poolName, isAutoscaling: false);
            logger.LogInformation($"An Azure batch pool called {poolName} successfully created!");
            Azure.CreateJobs(
                envDict["AZURE_PRIMARY_ACCESS_KEY"],
                validationPaths,
                envDict,
                jobName,
                envDict["AZURE_KEY1"],
                options.PullRequestNumber,
                poolName
            );
            logger.LogInformation($"Azure jobs successfully submitted!");
            // Wait for a few minutes before resizing.
            Thread.Sleep(TimeSpan.FromMinutes(8));
            Azure.EnablePoolAutoReszing(envDict["AZURE_PRIMARY_ACCESS_KEY"], poolName);
            logger.LogInformation($"Azure pool successfully resized!");
            stopwatch.Stop();
            logger.LogInformation($"Validation workflow completed successfully in {stopwatch.Elapsed.Humanize()}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Validation workflow error: {ex.Message}\n{ex.StackTrace}");
            stopwatch.Stop();
            logger.LogInformation($"Workflow failed after: {stopwatch.Elapsed.Humanize()}");
            exitCode = 1;
        }
    }


    /// <summary>
    /// Handles parser errors to ensure that a non-zero exit code
    /// is returned when parse errors are encountered.
    /// </summary>
    /// <param name="errors">Parse errors</param>
    private static void HandleParseError(IEnumerable<Error> errors)
    {
        foreach (var error in errors)
        {
            if (error is not VersionRequestedError && error is not HelpRequestedError && error is not HelpVerbRequestedError)
            {
                logger.LogError("Console error output: " + error.ToString());
                logger.LogError("Trace error output: " + error.ToString());
            }
        }

        // A missing required option must fail the job rather than silently succeed.
        if (!(errors.IsHelp() || errors.IsVersion()))
            exitCode = 1;
    }

    /// <summary>
    /// Removes paths from the validation paths array if they contain any of the excluded simulation file paths.
    /// </summary>
    /// <param name="paths">The collection of validation paths.</param>
    /// <returns>A filtered array of validation paths without excluded simulations.</returns>
    public static string[] RemoveExcludedPaths(IEnumerable<string> paths)
    {
        return paths.Where(path => !ContainsExcludedPath(path)).ToArray();
    }

    /// <summary>
    /// Removes paths from the validation paths list if they contain any of the excluded simulation file paths.
    /// </summary>
    /// <param name="paths">The list of validation paths.</param>
    public static void RemoveExcludedPaths(List<string> paths)
    {
        paths.RemoveAll(ContainsExcludedPath);
    }

    /// <summary>
    /// Determines whether a path contains any of the excluded simulation file paths.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns>True if the path contains an excluded simulation path; otherwise, false.</returns>
    public static bool ContainsExcludedPath(string path)
    {
        string normalizedPath = path.NormalizePath().TrimStart('/');
        return PayloadUtilities.EXCLUDED_SIMS_FILEPATHS.Any(excluded =>
            normalizedPath.Contains(excluded.NormalizePath().TrimStart('/'), StringComparison.OrdinalIgnoreCase));
    }

}
