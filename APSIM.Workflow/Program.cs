using System;
using CommandLine;
using System.Diagnostics;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Climate;
using APSIM.Shared.Utilities;

namespace APSIM.Workflow;

/// <summary>
/// Main program class for the APSIM.Workflow application.
/// </summary>
public class Program
{
    private static int exitCode = 0;
    public static string apsimFileName = string.Empty;

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
                CopyWeatherFiles(options);
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
    private static void CopyWeatherFiles(Options options)
    {
        try
        {
            string apsimxFileText = GetApsimXFileTextFromDirectory(options);
            
            if (apsimxFileText == null)
            {
                throw new Exception("Error: APSIMX file not found.");
            }

            var simulations = FileFormat.ReadFromString<Simulations>(apsimxFileText, e => throw e, false).NewModel as Simulations;
            
            if (simulations == null)
            {
                throw new Exception("Error: Failed to read simulations from APSIMX file.");
            }
            List<Weather> weatherModels = simulations.FindAllDescendants<Weather>().ToList();

            if (weatherModels.Count != 0)
            {
                if (options.Verbose)
                {
                    Console.WriteLine("Weather files found");
                }
            }
            else
            {
                Console.WriteLine("No weather files found");
                return;
            } 

            foreach (Weather weather in weatherModels)
            {
                string oldPath = weather.FileName;
                string source = PathUtilities.GetAbsolutePath(weather.FileName,"").Replace("\\", "/").Replace("APSIM.Workflow/","");
                string destination = options.DirectoryPath + Path.GetFileName(source).Replace("\\", "/");
                if (options.Verbose)
                {
                    Console.WriteLine($"Copied weather file: " + "'" + source + "'" + " to " + "'" + destination + "'");
                }
                File.Copy(source, destination, true);
                UpdateWeatherFileNamePathInApsimXFile(apsimxFileText, oldPath, destination, options);
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
    private static string GetApsimXFileTextFromDirectory(Options options)
    {
        string apsimxFileText = string.Empty;
        try
        {
            var directoryPath = Path.GetDirectoryName(options.DirectoryPath);

            if (directoryPath == null)
            {
                throw new Exception("Error: Directory path is invalid.");
            }

            string[] files = Directory.GetFiles(directoryPath, "*.apsimx", SearchOption.TopDirectoryOnly);

            if (files.Length > 1)
            {
                throw new Exception("Expected to find a single .apsimx file in the directory. More than one was found.");
            }

            apsimFileName = files[0];
            
            if (string.IsNullOrWhiteSpace(apsimFileName))
            {
                throw new Exception("Error: APSIMX file not found while searching the directory.");
            }

            apsimxFileText = File.ReadAllText(apsimFileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            exitCode = 1;
        }

        return apsimxFileText;

    }

        /// <summary>
        /// Updates the weather file name path in the APSIMX file.
        /// </summary>
        /// <param name="apsimxFileText">The APSIMX file text.</param>
        /// <param name="oldPath">The old path of the weather file.</param>
        /// <param name="newPath">The new path of the weather file.</param>
        public static void UpdateWeatherFileNamePathInApsimXFile(string apsimxFileText, string oldPath, string newPath, Options options)
        {
            string newApsimxFileText = apsimxFileText.Replace(oldPath, newPath);
            if (string.IsNullOrWhiteSpace(options.DirectoryPath))
            {
                throw new Exception("Error: Directory path is null while trying to update weather file path in APSIMX file.");
            }
            string savePath = Path.Combine(options.DirectoryPath, Path.GetFileName(apsimFileName));
            File.WriteAllText(savePath, newApsimxFileText);
            if(options.Verbose)
            {
                Console.WriteLine("Successfully updated weather file path in " + savePath);
            }
        }
}



