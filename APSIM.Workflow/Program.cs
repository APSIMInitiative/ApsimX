using APSIM.Shared.Utilities;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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


    /// <summary> Main entry point for APSIM.WorkFlow</summary>
    /// <param name="args">command line arguments</param>
    public static int Main(string[] args)
    {
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
                FileSplitter.Run(options.DirectoryPath, options.SplitFiles, true);
                return;
            }
            else if (options.ValidationLocations)
            {
                if (options.Verbose)
                    Console.WriteLine("Validation locations:");

                foreach(string dir in ValidationLocationUtility.GetDirectoryPaths())
                {
                    Console.WriteLine(dir);
                }
            }
            if (!string.IsNullOrWhiteSpace(options.DirectoryPath))
            {
                try
                {
                    bool weatherFilesCopied = false;
                    Console.WriteLine("Processing directory: " + options.DirectoryPath);
                    apsimFilePaths = InputUtilities.StandardiseFilePaths(PathUtilities.GetAllApsimXFilePaths(options.DirectoryPath));
                    List<string> newSplitDirectories = new();
                    foreach (string apsimxFilePath in apsimFilePaths)
                    {
                        newSplitDirectories = FileSplitter.Run(apsimxFilePath, null, true);
                        weatherFilesCopied = true;
                        if (options.Verbose)
                            Console.WriteLine($"Number of Split directories for {apsimxFilePath}: {newSplitDirectories.Count}");
                        if (options.Verbose)
                        {
                            foreach (string splitDirectory in newSplitDirectories)
                            {
                                Console.WriteLine(splitDirectory);
                            }
                        }
                    }

                    foreach (string splitDirectory in newSplitDirectories)
                    {
                        if (options.Verbose)
                            Console.WriteLine("Split directory: " + splitDirectory);

                        WorkFloFileUtilities.CreateValidationWorkFloFile(splitDirectory, newSplitDirectories, options.GitHubAuthorID, options.DockerImageTag);  

                        if (!File.Exists(Path.Combine(splitDirectory, "workflow.yml")))
                        {
                            exitCode++;
                            throw new Exception("Error: Failed to create validation workflow file.");
                        }

                        if(options.Verbose)
                            Console.WriteLine("Validation workflow file created.");

                        bool zipFileCreated = PayloadUtilities.CreateZipFile(splitDirectory);

                        if(options.Verbose && zipFileCreated)
                            Console.WriteLine("Zip file created.");
                
                        if(weatherFilesCopied & zipFileCreated & exitCode == 0)
                        {
                            if (options.Verbose)
                                Console.WriteLine("Submitting workflow job to Azure.");

                            PayloadUtilities.SubmitWorkFloJob(options.DirectoryPath).Wait();
                        }
                        else if (weatherFilesCopied & zipFileCreated & exitCode != 0)
                        {
                            Console.WriteLine("There was an issue with the validation workflow. Please check the logs for more details.");
                        }
                        else if (!weatherFilesCopied && zipFileCreated && exitCode == 0)
                        {
                            PayloadUtilities.SubmitWorkFloJob(options.DirectoryPath).Wait();
                        }
                        else throw new Exception("There was an issue organising the files for submittal to Azure.\n");
                    }

                    if (options.Verbose)
                        Console.WriteLine("Finished with exit code " + exitCode);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Validation workflow error: " + ex.Message);
                    exitCode = 1;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
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
        // To help with Jenkins only errors.
        foreach (var error in errors)
        {
            //We need to exclude these as the nuget package has a bug that causes them to appear even if there is no error.
            if (error as VersionRequestedError == null && error as HelpRequestedError == null && error as MissingRequiredOptionError == null)
            {
                Console.WriteLine("Console error output: " + error.ToString());
                Trace.WriteLine("Trace error output: " + error.ToString());
            }
        }

        if (!(errors.IsHelp() || errors.IsVersion() || errors.Any(e => e is MissingRequiredOptionError)))
            exitCode = 1;
    }

    
}