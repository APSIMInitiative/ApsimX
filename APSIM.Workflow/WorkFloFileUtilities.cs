
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

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
    /// <param name="options">Command line argument values</param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static void CreateValidationWorkFloFile(Options options)
    {
        try
        {
            string indent = "  ";
            string workFloFileName = "workflow.yml";
            // Include the workflow yml file for debugging purposes
            string[] inputFiles = [".env", workFloFileName, "grid.csv"];
            string workFloFileContents = InitializeWorkFloFile();
            workFloFileContents = AddInputFilesToWorkFloFile(workFloFileContents, inputFiles);
            workFloFileContents = AddTaskToWorkFloFile(workFloFileContents);
            workFloFileContents = AddInputFilesToWorkFloFile(workFloFileContents, inputFiles, indent);
            workFloFileContents = AddGridToWorkFloFile(workFloFileContents, indent);
            workFloFileContents = AddStepsToWorkFloFile(workFloFileContents, indent, options);
            workFloFileContents = AddPOStatsStepToWorkFloFile(workFloFileContents, indent, options);
            File.WriteAllText(Path.Combine(options.DirectoryPath, workFloFileName), workFloFileContents);
            Console.WriteLine($"Workflow.yml contents:\n{workFloFileContents}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating validation workflow file: {ex.Message}\n{ex.StackTrace}");
        }

    }

    private static string AddGridToWorkFloFile(string workFloFileContents, string indent)
    {
        workFloFileContents += $"{indent}grid: grid.csv {Environment.NewLine}";
        return workFloFileContents;
    }

    /// <summary>
    /// Gets the input file names from the specified directory path.
    /// Excludes files with specific extensions such as .yml, .zip, .bak, .db, .db-shm, and .db-wal.
    /// </summary>
    /// <param name="directoryPathString"></param>
    /// <returns>a string[] of relevant files</returns>
    private static string[] GetInputFileNames(string directoryPathString)
    {
        Console.WriteLine($"All files in directory {directoryPathString}:");
        return Directory.GetFiles(directoryPathString).Where(
            filename => !filename.EndsWith(".yml") &&
                        !filename.EndsWith("payload.zip") &&
                        !filename.EndsWith(".bak") &&
                        !filename.EndsWith(".db") &&
                        !filename.EndsWith(".db-shm") &&
                        !filename.EndsWith(".db-wal")).ToArray();
    }

    /// <summary>
    /// Adds steps to the workflow file for each apsimx file present in the directory
    /// </summary>
    /// <param name="workFloFileContents">the existing workflo file contents</param>
    /// <param name="indent">a string used as the indent</param>
    /// <param name="options">command line argument values</param>
    /// <returns></returns>
    private static string AddStepsToWorkFloFile(string workFloFileContents, string indent, Options options)
    {
        workFloFileContents += $"{indent}steps: "+ Environment.NewLine;
        // TODO: Replace ric394 with apsiminitiative when the docker image is available
        workFloFileContents += $"""

            {indent}  - uses: ric394/apsimx:{options.DockerImageTag}
            {indent}    args: --recursive $Path*.apsimx

            """;
        
        return workFloFileContents;
    }

    /// <summary>
    /// Initializes the workflow file with the name and input files statement.
    /// </summary>
    public static string InitializeWorkFloFile()
    {
        string workFloFileContents = $"""
        name: workflo_apsim_validation
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
    /// <returns></returns>
    public static string AddTaskToWorkFloFile(string workFloFileContents)
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
    /// <param name="options">command line argument values</param>
    /// <returns>The existing content of a workflow yml file with a new po stats step appended</returns>
    public static string AddPOStatsStepToWorkFloFile(string workFloFileContents, string indent, Options options)
    {
        // string currentBuildNumber = Task.Run(GetCurrentBuildNumberAsync).Result; // TODO: Uncomment currentBuildNumber once development is complete
        string currentBuildNumber = "10018"; // Placeholder for development, replace with actual call to GetCurrentBuildNumberAsync
        string timeFormat = "yyyy.M.d-HH:mm";
        TimeZoneInfo brisbaneTZ = TimeZoneInfo.FindSystemTimeZoneById("E. Australia Standard Time");
        DateTime brisbaneDatetimeNow = TimeZoneInfo.ConvertTime(DateTime.Now, brisbaneTZ);
        const string azureWorkingDirectory = "/wd/";
        workFloFileContents += $"""

        {indent}  - uses: apsiminitiative/postats2-collector:latest
        {indent}    args: upload {currentBuildNumber} {options.CommitSHA} {options.GitHubAuthorID} {brisbaneDatetimeNow.ToString(timeFormat)}  {azureWorkingDirectory + "$Path"}

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
