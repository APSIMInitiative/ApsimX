using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Diagnostics;
using System.Threading.Tasks;
using Humanizer;
using System.Threading;

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
                FileSplitter.Run(options.DirectoryPath, options.SplitFiles, true, logger);
                return;
            }
            else if (options.ValidationLocations)
            {
                if (options.Verbose)
                    logger.LogInformation("Validation locations:");

                foreach (string dir in ValidationLocationUtility.GetDirectoryPaths())
                {
                    Console.WriteLine(dir + "/");
                }
            }
            if (!string.IsNullOrEmpty(options.ValidationPath) && !string.IsNullOrEmpty(options.DirectoryPath))
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                try
                {
                    if (options.Verbose)
                        logger.LogInformation($"Validation path: {options.ValidationPath}");
                    PrepareAndSubmitWorkflowJob(options);
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
        // WorkFloFileUtilities.CreateValidationWorkFloFile(options.DirectoryPath, options.ValidationPath, options.GitHubAuthorID, options.DockerImageTag);
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