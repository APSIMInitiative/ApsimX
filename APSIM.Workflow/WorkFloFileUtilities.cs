
namespace APSIM.Workflow;
public static class WorkFloFileUtilities
{
    /// <summary>
    /// Creates a validation workflow file in the specified directory.
    /// </summary>
    /// <param name="directoryPathString"></param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static void CreateValidationWorkFloFile(string directoryPathString, List<string> apsimFilePaths, string githubAuthorID, string dockerImageTag = "latest")
    {
        if (!Directory.Exists(directoryPathString))
        {
            throw new DirectoryNotFoundException("Directory not found: " + directoryPathString);
        }

        // Path inside azure virtual machine to apsimx file(s)
        string apsimxDir = "/wd/";
        string workFloFileName = "workflow.yml";
        string workFloName = GetDirectoryName(directoryPathString);
        string[] inputFiles = GetInputFileNames(directoryPathString);
        string workFloFileContents = InitializeWorkFloFile(workFloName);
        workFloFileContents = AddInputFilesToWorkFloFile(workFloFileContents, inputFiles);
        workFloFileContents = AddTaskToWorkFloFile(workFloFileContents, inputFiles);
        string indent = "  ";
        workFloFileContents = AddInputFilesToWorkFloFile(workFloFileContents, inputFiles, indent);
        workFloFileContents = AddStepsToWorkFloFile(workFloFileContents, indent, apsimFilePaths, dockerImageTag);
        // workFloFileContents = AddPOStatsStepToWorkFloFile(workFloFileContents, indent, githubAuthorID, apsimxDir);
        File.WriteAllText(Path.Combine(directoryPathString, workFloFileName), workFloFileContents);
    }

    /// <summary>
    /// Gets the input file names from the specified directory path.
    /// </summary>
    /// <param name="directoryPathString"></param>
    /// <returns></returns>
    private static string[] GetInputFileNames(string directoryPathString)
    {
        return Directory.GetFiles(directoryPathString).Where(
            filename => !filename.EndsWith(".yml") && 
                        !filename.EndsWith("payload.zip") && 
                        !filename.EndsWith(".bak")).ToArray();
    }

    /// <summary>
    /// Adds steps to the workflow file for each apsimx file present in the directory
    /// </summary>
    /// <param name="workFloFileContents"></param>
    /// <param name="indent"></param>
    /// <param name="apsimFilePaths"></param>
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
    /// <param name=""></param>
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
    /// <param name="pullRequestID">The pull request number</param>
    /// <param name="githubAuthorID">The author's GitHub username for the pull request</param>
    /// <param name="apsimxDir">The root directory for ApsimX</param>
    /// <returns>The existing content of a workflow yml file with a new po stats step appended</returns>
    public static string AddPOStatsStepToWorkFloFile(string workFloFileContents, string indent, string githubAuthorID, string apsimxDir)
    {
        string currentBuildNumber = Task.Run(GetCurrentBuildNumberAsync).Result;
        string timeFormat = "yyyy.M.d-HH:mm";
        TimeZoneInfo brisbaneTZ = TimeZoneInfo.FindSystemTimeZoneById("E. Australia Standard Time");
        DateTime brisbaneDatetimeNow = TimeZoneInfo.ConvertTime(DateTime.Now, brisbaneTZ);
        workFloFileContents += $"""

        {indent}  - uses: apsiminitiative/postats
        {indent}    args: {currentBuildNumber} {brisbaneDatetimeNow.ToString(timeFormat)} {githubAuthorID} {apsimxDir}
                
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
