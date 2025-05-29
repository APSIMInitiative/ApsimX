using APSIM.Shared.Utilities;
using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

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
            builder.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "yyyy-MM-ddTHH:mm:ssK "; // ISO 8601 format
                options.IncludeScopes = false;
            });
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
                        if (options.Verbose)
                        {
                            foreach (string dir in newSplitDirectories)
                            {
                                logger.LogInformation(dir);
                                logger.LogInformation($"Files in {dir}:");
                                foreach (string file in Directory.GetFiles(dir))
                                {
                                    logger.LogInformation("  " + Path.GetFileName(file));
                                }
                                logger.LogInformation("");
                            }
                        }
                    }
                    string splitDirectory = newSplitDirectories.FirstOrDefault();
                    if (options.Verbose)
                        logger.LogInformation("Split directory: " + splitDirectory);

                    // Check that xlsx files are present in the split directory
                    logger.LogInformation($"Before creating workflow file, xlsx files found in {splitDirectory} :{Directory.GetFiles(splitDirectory, "*.xlsx", SearchOption.AllDirectories).Length != 0}");

                    WorkFloFileUtilities.CreateValidationWorkFloFile(splitDirectory, newSplitDirectories, options.GitHubAuthorID, options.DockerImageTag);  

                    if (!File.Exists(Path.Combine(splitDirectory, "workflow.yml")))
                    {
                        exitCode++;
                        throw new Exception("Error: Failed to create validation workflow file.");
                    }

                    if(options.Verbose)
                        logger.LogInformation("Validation workflow file created.");

                    bool zipFileCreated = PayloadUtilities.CreateZipFile(splitDirectory, options.Verbose);

                    if(options.Verbose && zipFileCreated)
                        logger.LogInformation("Zip file created.");
                
                    if(weatherFilesCopied & zipFileCreated & exitCode == 0)
                    {
                        if (options.Verbose)
                            logger.LogInformation("Adding .env file to payload");
                        PayloadUtilities.CopyEnvToPayload(options.DirectoryPath, splitDirectory, options.Verbose);

                        if (options.Verbose)
                            logger.LogInformation("Submitting workflow job to Azure.");

                        PayloadUtilities.SubmitWorkFloJob(splitDirectory).Wait();
                    }
                    else if (weatherFilesCopied & zipFileCreated & exitCode != 0)
                    {
                        logger.LogError("There was an issue with the validation workflow. Please check the logs for more details.");
                    }
                    else if (!weatherFilesCopied && zipFileCreated && exitCode == 0)
                    {
                        PayloadUtilities.SubmitWorkFloJob(options.DirectoryPath).Wait();
                    }
                    else throw new Exception("There was an issue organising the files for submittal to Azure.\n");

                    if (options.Verbose)
                        logger.LogInformation("Finished with exit code " + exitCode);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Validation workflow error: {ex.Message}\n{ex.StackTrace}");
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