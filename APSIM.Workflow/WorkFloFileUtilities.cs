
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace APSIM.Workflow;

/// <summary>
/// Utility class for creating and managing WorkFlo files.
/// A workflow file is a YAML file that defines a series of tasks to be executed in a specific order.
/// </summary>
public static class WorkFloFileUtilities
{
    /// <summary>
    /// Creates a validation workflow file in the specified directory.
    /// </summary>
    /// <param name="directoryPathString"></param>
    /// <param name="apsimFilePaths"></param>
    /// <param name="githubAuthorID"></param>
    /// <param name="dockerImageTag"></param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static void CreateValidationWorkFloFile(string directoryPathString, List<string> apsimFilePaths, string githubAuthorID, string dockerImageTag = "latest")
    {
        try
        {
            if (!Directory.Exists(directoryPathString))
            {
                throw new DirectoryNotFoundException("Directory not found: " + directoryPathString);
            }

            string indent = "  ";
            string apsimFileName = Path.GetFileName(Directory.GetFiles(directoryPathString).FirstOrDefault(file => file.EndsWith(".apsimx")));
            string workFloFileName = "workflow.yml";
            string workFloName = GetDirectoryName(directoryPathString);
            string[] inputFiles = GetInputFileNames(directoryPathString);
            string workFloFileContents = InitializeWorkFloFile(workFloName);
            workFloFileContents = AddInputFilesToWorkFloFile(workFloFileContents, inputFiles);
            workFloFileContents = AddTaskToWorkFloFile(workFloFileContents, inputFiles);
            workFloFileContents = AddInputFilesToWorkFloFile(workFloFileContents, inputFiles, indent);
            workFloFileContents = AddStepsToWorkFloFile(workFloFileContents, indent, [apsimFileName], dockerImageTag);
            workFloFileContents = AddPOStatsStepToWorkFloFile(workFloFileContents, indent, githubAuthorID);
            File.WriteAllText(Path.Combine(directoryPathString, workFloFileName), workFloFileContents);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating validation workflow file: {ex.Message}\n{ex.StackTrace}");
        }

    }

    /// <summary>
    /// Gets the input file names from the specified directory path.
    /// </summary>
    /// <param name="directoryPathString"></param>
    /// <returns></returns>
    private static string[] GetInputFileNames(string directoryPathString)
    {
        Console.WriteLine($"All files in directory {directoryPathString}:");
        foreach (string file in Directory.GetFiles(directoryPathString))
        {
            Console.WriteLine(file);
        }
        return Directory.GetFiles(directoryPathString).Where(
            filename => !filename.EndsWith(".yml") &&
                        !filename.EndsWith("payload.zip") &&
                        !filename.EndsWith(".bak") &&
                        !filename.EndsWith(".db")).ToArray();
    }

    /// <summary>
    /// Adds steps to the workflow file for each apsimx file present in the directory
    /// </summary>
    /// <param name="workFloFileContents"></param>
    /// <param name="indent"></param>
    /// <param name="apsimFilePaths"></param>
    /// <param name="dockerImageTag"></param>
    /// <returns></returns>
    private static string AddStepsToWorkFloFile(string workFloFileContents, string indent, List<string> apsimFilePaths, string dockerImageTag = "latest")
    {
        workFloFileContents += $"{indent}steps: "+ Environment.NewLine;
        foreach(string filePath in apsimFilePaths)
        {
            string apsimFileName = Path.GetFileName(filePath);
            // TODO: Replace ric394 with apsiminitiative when the docker image is available
            workFloFileContents += $"""

                {indent}  - uses: ric394/apsimx:{dockerImageTag}
                {indent}    args: {Path.GetFileName(apsimFileName)} --csv 
                
                """;
        }
        return workFloFileContents;
    }

    /// <summary>
    /// Initializes the workflow file with the name and input files statement.
    /// </summary>
    /// <param name="workFloName">Name for the WorkFlo</param>
    public static string InitializeWorkFloFile(string workFloName)
    {
        string workFloFileContents = $"""
        name: {workFloName}
        inputfiles:{Environment.NewLine}
        """;
        return workFloFileContents;
    }

    /// <summary>
    /// Adds input file lines to the workflow file with correct indentation.
    /// </summary>
    /// <param name="workfloFileText"></param>
    /// <param name="inputFiles"></param>
    /// <param name="indent"></param>
    public static string AddInputFilesToWorkFloFile(string workfloFileText, string[] inputFiles, string indent = "")
    {
        foreach (string file in inputFiles)
        {
            // TODO: remove this when fully working
            Console.WriteLine($"Adding {file} as an inputFile to the workflow yml file");
            string inputFileName = Path.GetFileName(file);
            workfloFileText += indent + "- " + inputFileName + Environment.NewLine;
        }
        return workfloFileText;
    }

    /// <summary>
    /// Gets the directory name from the specified directory path.
    /// </summary>
    /// <param name="directoryPathString"></param>
    public static string GetDirectoryName(string directoryPathString)
    {
        if (string.IsNullOrEmpty(directoryPathString))
        {
            throw new ArgumentNullException("directoryPathString");
        }
        string dirName = Path.GetFileName(Path.GetDirectoryName(directoryPathString))!;
        return dirName;
    }

    /// <summary>
    /// Adds a task to the workflow yml file.
    /// </summary>
    /// <param name="workFloFileContents"></param>
    /// <param name="inputFiles"></param>
    /// <returns></returns>
    public static string AddTaskToWorkFloFile(string workFloFileContents, string[] inputFiles)
    {
        workFloFileContents += $"""
        tasks:
        - name: 0001
          inputfiles:{Environment.NewLine}
        """;
        return workFloFileContents; 
    }

    /// <summary>
    /// Add a PO Stats step to the workflow yml file with arguments.
    /// </summary>
    /// <param name="workFloFileContents">the existing content for the workflow yml file.</param>
    /// <param name="indent">the amount of space used for formatting the yml file step</param>
    /// <param name="githubAuthorID">The author's GitHub username for the pull request</param>
    /// <returns>The existing content of a workflow yml file with a new po stats step appended</returns>
    public static string AddPOStatsStepToWorkFloFile(string workFloFileContents, string indent, string githubAuthorID)
    {
        // string currentBuildNumber = Task.Run(GetCurrentBuildNumberAsync).Result; // TODO: Uncomment currentBuildNumber once development is complete
        string currentBuildNumber = "10017"; // Placeholder for development, replace with actual call to GetCurrentBuildNumberAsync
        string timeFormat = "yyyy.M.d-HH:mm";
        TimeZoneInfo brisbaneTZ = TimeZoneInfo.FindSystemTimeZoneById("E. Australia Standard Time");
        DateTime brisbaneDatetimeNow = TimeZoneInfo.ConvertTime(DateTime.Now, brisbaneTZ);
        const string azureWorkingDirectory = ".";
        workFloFileContents += $"""

        {indent}  - uses: apsiminitiative/postats-collector:latest
        {indent}    args: {currentBuildNumber} {brisbaneDatetimeNow.ToString(timeFormat)} {githubAuthorID} {azureWorkingDirectory}

        """;
        return workFloFileContents;
    }

    /// <summary>
    /// Gets the current build number.
    /// </summary>
    /// <returns></returns>
    public static async Task<string> GetCurrentBuildNumberAsync()
    {
        using HttpClient client = new()
        {
            BaseAddress = new Uri("https://builds.apsim.info/api/"),
        };
        using HttpResponseMessage response = await client.GetAsync("nextgen/nextversion/");
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        return jsonResponse;


    }



}
