using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Diagnostics;
using Humanizer;
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

        Parser.Default.ParseArguments<Options>(args).WithParsed(RunOptions).WithNotParsed(HandleParseError);

        return exitCode;
    }


    /// <summary>Runs the application with the specified options.</summary>
    /// <param name="options"></param>
    private static void RunOptions(Options options)
    {
        try
        {
            if (options.SplitFiles != null)
            {
                FileSplitter.Run(options.DirectoryPath, options.SplitFiles, Path.GetDirectoryName(options.DirectoryPath) + "/", logger);
                return;
            }
            else if (options.ValidationLocations)
            {
                if (options.Verbose)
                    logger.LogInformation("Validation locations:");

                foreach (string dir in ValidationLocationUtility.GetValidationFilePaths())
                {
                    Console.WriteLine(dir);
                }
            }
            else if (options.SimulationCount)
            {
                Console.WriteLine(ValidationLocationUtility.GetSimulationCount());
            }
            if (!string.IsNullOrEmpty(options.DirectoryPath))
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                try
                {
                    // PrepareAndSubmitWorkflowJob(options);
                    string[] validationPaths = ValidationLocationUtility.GetValidationFilePaths();
                    // Create a pool name using the PR number and commit SHA.
                    if (string.IsNullOrEmpty(options.PullRequestNumber))
                        throw new ArgumentException("A pull request number argument must be provided for Azure batch pool creation to complete successfully.");
                    if (string.IsNullOrEmpty(options.CommitSHA))
                        throw new ArgumentException("A commit SHA argument must be provided for Azure batch pool creation to complete successfully.");
                    if (validationPaths.Length < 1)
                        throw new Exception("A list of validation paths must be provided to continue.");
                    if (string.IsNullOrEmpty(options.EnvString))
                        throw new ArgumentException("An environment variable must be provided to continue.");

                    string poolName = options.PullRequestNumber + options.CommitSHA;
                    string nowDateString = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
                    string jobName = $"{nowDateString}-acceptance-tests-pr-{options.PullRequestNumber}";

                    // Create the environment variable dictionary for use further on 
                    // from the environment variable string.

                    Dictionary<string, string> envDict = [];
                    foreach(string line in options.EnvString.Split(
                        [ "\r\n", "\n" ],
                        StringSplitOptions.RemoveEmptyEntries))
                    {
                        // Split only on the first '=' since values (e.g. base64 keys) may themselves contain '='.
                        string[] values = line.Split('=', 2);
                        envDict.Add(values[0], values[1]);
                    }
                    Console.WriteLine($"envString={options.EnvString}");
                    foreach (var item in envDict)
                        Console.WriteLine(item);

                    Azure.CreatePool(envDict["AZURE_PRIMARY_ACCESS_KEY"], poolName, isAutoscaling: false);
                    logger.information($"An Azure batch pool called {poolname} successfully created!");
                    Azure.CreateJobs(
                        envDict["AZURE_PRIMARY_ACCESS_KEY"],
                        validationPaths,
                        envDict,
                        jobName,
                        envDict["AZURE_KEY1"],
                        options.PullRequestNumber,
                        poolName
                    );
                    logger.information($"Azure jobs successfully submitted!");
                    // Wait for a few minutes before resizing.
                    Thread.Sleep(TimeSpan.FromMinutes(3));
                    Azure.EnablePoolAutoReszing(envDict["AZURE_PRIMARY_ACCESS_KEY"], poolName);
                    logger.information($"Azure pool successfully resized!");
                    stopwatch.Stop();
                }
                catch (Exception ex)
                {
                    logger.LogError($"Validation workflow error: {ex.Message}\n{ex.StackTrace}");
                    stopwatch.Stop();
                    logger.LogInformation($"Workflow failed after: {stopwatch.Elapsed.Humanize()}");
                    exitCode = 1;
                }

                logger.LogInformation($"Validation workflow completed successfully in {stopwatch.Elapsed.Humanize()}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error: " + ex.Message);
            exitCode = 1;
        }
    }

    private static void PrepareAndSubmitWorkflowJob(Options options)
    {
        WorkFloFileUtilities.CreateValidationWorkFloFile(options);
        if (options.Verbose)
            logger.LogInformation("Validation workflow file created.");

        bool zipFileCreated = PayloadUtilities.CreateZipFile(options.DirectoryPath, options.Verbose);
        if (options.Verbose && zipFileCreated)
            logger.LogInformation("Zip file created.");

        if (zipFileCreated & exitCode == 0)
        {
            if (options.Verbose)
                logger.LogInformation("Submitting workflow job to Azure.");

            PayloadUtilities.SubmitWorkFloJob(options.DirectoryPath).Wait();
        }
        else if (zipFileCreated & exitCode != 0)
        {
            logger.LogError("There was an issue with the validation workflow. Please check the logs for more details.");
        }
        else throw new Exception("There was an issue organising the files for submittal to Azure.\n");



    }

    /// <summary>
    /// Prints the contents of the split directory.
    /// </summary>
    /// <param name="splitDirectory">The path to the split directory.</param>
    private static void PrintSplitDirectoryContents(string splitDirectory)
    {
        logger.LogInformation(splitDirectory);
        logger.LogInformation($"Files in {splitDirectory}:");
        foreach (string file in Directory.GetFiles(splitDirectory))
        {
            logger.LogInformation("  " + Path.GetFileName(file));
        }
        logger.LogInformation("");
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
            if (error as VersionRequestedError == null && error as HelpRequestedError == null && error as MissingRequiredOptionError == null)
            {
                logger.LogError("Console error output: " + error.ToString());
                logger.LogError("Trace error output: " + error.ToString());
            }
        }

        if (!(errors.IsHelp() || errors.IsVersion() || errors.Any(e => e is MissingRequiredOptionError)))
            exitCode = 1;
    }


}
