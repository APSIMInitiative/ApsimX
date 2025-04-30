using CommandLine;
using System.Diagnostics;

namespace APSIM.Workflow
{

    /// <summary>
    /// Main program class for the APSIM.Workflow application.
    /// </summary>
    public class Program
    {
        private static int exitCode = 0;
        public static List<string> apsimFilePaths = [];


    public static int Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args).WithParsed(RunOptions).WithNotParsed(HandleParseError);
        return exitCode;
    }


        /// <summary>
        /// Runs the application with the specified options.
        /// </summary>
        /// <param name="options"></param>
        private static void RunOptions(Options options)
        {
            try
            {
                if (options.SplitFiles != null)
                {
                    FileSplitter.Run(options.DirectoryPath, options.SplitFiles);
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
                    apsimFilePaths = InputUtilities.StandardiseFilePaths(Directory.GetFiles(options.DirectoryPath, "*.apsimx").ToList());
                    Console.WriteLine("Processing file: " + options.DirectoryPath);
                    bool weatherFilesCopied = PayloadUtilities.CopyWeatherFiles(options, apsimFilePaths );

                    WorkFloFileUtilities.CreateValidationWorkFloFile(options.DirectoryPath, apsimFilePaths, options.GitHubAuthorID, options.DockerImageTag);                
                    if (!File.Exists(Path.Combine(options.DirectoryPath, "workflow.yml")))
                    {
                        exitCode++;
                        throw new Exception("Error: Failed to create validation workflow file.");
                    }

                    if(options.Verbose)
                        Console.WriteLine("Validation workflow file created.");
                    if(options.Verbose)
                        Console.WriteLine("Validation workflow file created.");

                    bool zipFileCreated = PayloadUtilities.CreateZipFile(options.DirectoryPath);

                    if(options.Verbose && zipFileCreated)
                    {
                        Console.WriteLine("Zip file created.");
                    }
          
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
    /// <param name="errors">Parse errors.</param>
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
}