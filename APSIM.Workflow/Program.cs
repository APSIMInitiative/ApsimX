using System;
using CommandLine;
using System.Diagnostics;
using Models.Core;

namespace APSIM.Workflow;

/// <summary>
/// Main program class for the APSIM.Workflow application.
/// </summary>
class Program
{
    private static int exitCode = 0;

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
            if (options.DirectoryPath != null)
            {
                Console.WriteLine("Processing file: " + options.DirectoryPath);
                CopyWeatherFiles(options.DirectoryPath);
            }
            if (options.Verbose)
            {
                Console.WriteLine("Verbose output enabled.");
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

    /// <summary>
    /// Copies the weather files from the specified directories in the apsimx file to the directory.
    /// </summary>
    /// <param name="zipFile"></param>
    private static void CopyWeatherFiles(string directoryPath)
    {
        try
        {
            string apsimxFilePath = GetApsimXFilePathFromDirectory(directoryPath);
            Simulations simulations = FileFormat.ReadFromFile<Simulations>(directoryPath);
            foreach (Weather weather in simulations.FindAllDescendants<Weather>())
            {
                string source = Path.Combine(Path.GetDirectoryName(directoryPath), weather.FileName);
                string destination = Path.Combine(Path.GetDirectoryName(directoryPath), weather.FileName);
                File.Copy(source, destination, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            exitCode = 1;
        }

    }

    /// <summary>
    /// Gets the path of the apsimx file from the directory.
    /// </summary>
    /// <param name="zipFile"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private string GetApsimXFilePathFromDirectory(string directoryPath)
    {
        string apsimxFile = null;
        try
        {
            string[] files = Directory.GetFiles(Path.GetDirectoryName(directoryPath), "*.apsimx", SearchOption.TopDirectoryOnly);
            if (files.Length == 1)
            {
                apsimxFile = files[0];
            }
            else
            {
                throw new Exception("Expected to find a single .apsimx file in the directory.");
            }
        }
        return apsimxFile;
    }
}



