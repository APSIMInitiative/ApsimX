using APSIM.Shared.Utilities;
using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Diagnostics;
using System.Threading.Tasks;

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
            if (options.SplitFiles != null )
            {
                FileSplitter.Run(options.DirectoryPath, options.SplitFiles, true, logger);
                return;
            }
            else if (options.ValidationLocations)
            {
                if (options.Verbose)
                    logger.LogInformation("Validation locations:");

                foreach(string dir in ValidationLocationUtility.GetDirectoryPaths())
                {
                    logger.LogInformation(dir);
                }
            }
            if (!string.IsNullOrWhiteSpace(options.DirectoryPath))
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                try
                {
                    bool weatherFilesCopied = false;
                    logger.LogInformation($"Processing directory: {options.DirectoryPath}");
                    apsimFilePaths = InputUtilities.StandardiseFilePaths(PathUtilities.GetAllApsimXFilePaths(options.DirectoryPath));
                    List<string> newSplitDirectories = new();
                    foreach (string apsimxFilePath in apsimFilePaths)
                    {
                        newSplitDirectories = FileSplitter.Run(apsimxFilePath, null, true, logger);
                        weatherFilesCopied = true;
                        if (options.Verbose)
                            logger.LogInformation($"Number of Split directories for {apsimxFilePath}: {newSplitDirectories.Count}");

                        var tasks = new List<Task>();
                        foreach (string splitDirectory in newSplitDirectories)
                        {
                            // Does this asynchronously so that it can handle multiple directories
                            tasks.Add(PrepareAndSubmitWorkflowJob(options, weatherFilesCopied, newSplitDirectories, splitDirectory));
                        }
                        Task.WhenAll(tasks).Wait();

                        if (options.Verbose)
                            logger.LogInformation("Finished with exit code " + exitCode);

                        stopwatch.Stop();
                        logger.LogInformation($"Total time taken: {stopwatch.Elapsed.TotalMinutes} minutes and {stopwatch.Elapsed.TotalSeconds} seconds");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Validation workflow error: {ex.Message}\n{ex.StackTrace}");
                    stopwatch.Stop();
                    logger.LogInformation($"Workflow failed after: {stopwatch.Elapsed.TotalMinutes} minutes and {stopwatch.Elapsed.TotalSeconds} seconds");
                    exitCode = 1;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error: " + ex.Message);
            exitCode = 1;
        }
    }

    private static async Task PrepareAndSubmitWorkflowJob(Options options, bool weatherFilesCopied, List<string> newSplitDirectories, string splitDirectory)
    {
        if (options.Verbose)
            PrintSplitDirectoryContents(splitDirectory);

        // string splitDirectory = newSplitDirectories.FirstOrDefault();
        if (options.Verbose)
            logger.LogInformation("Split directory: " + splitDirectory);

        // Check that xlsx files are present in the split directory
        if (options.Verbose)
            logger.LogInformation($"Before creating workflow file, xlsx files found in {splitDirectory} :{Directory.GetFiles(splitDirectory, "*.xlsx", SearchOption.AllDirectories).Length != 0}");

        WorkFloFileUtilities.CreateValidationWorkFloFile(splitDirectory, newSplitDirectories, options.GitHubAuthorID, options.DockerImageTag);

        if (!File.Exists(Path.Combine(splitDirectory, "workflow.yml")))
        {
            exitCode++;
            throw new Exception("Error: Failed to create validation workflow file.");
        }

        if (options.Verbose)
            logger.LogInformation("Validation workflow file created.");

        bool zipFileCreated = PayloadUtilities.CreateZipFile(splitDirectory, options.Verbose);

        if (options.Verbose && zipFileCreated)
            logger.LogInformation("Zip file created.");

        if (weatherFilesCopied & zipFileCreated & exitCode == 0)
        {
            if (options.Verbose)
                logger.LogInformation("Adding .env file to payload");
            PayloadUtilities.CopyEnvToPayload(options.DirectoryPath, splitDirectory, options.Verbose);

            if (options.Verbose)
                logger.LogInformation("Submitting workflow job to Azure.");

            await PayloadUtilities.SubmitWorkFloJob(splitDirectory);
        }
        else if (weatherFilesCopied & zipFileCreated & exitCode != 0)
        {
            logger.LogError("There was an issue with the validation workflow. Please check the logs for more details.");
        }
        else if (!weatherFilesCopied && zipFileCreated && exitCode == 0)
        {
            await PayloadUtilities.SubmitWorkFloJob(options.DirectoryPath);
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