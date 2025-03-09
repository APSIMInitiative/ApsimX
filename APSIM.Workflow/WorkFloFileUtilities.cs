
namespace APSIM.Workflow;
public static class WorkFloFileUtilities
{
    /// <summary>
    /// Creates a validation workflow file in the specified directory.
    /// </summary>
    /// <param name="directoryPathString"></param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static void CreateValidationWorkFloFile(string directoryPathString, List<string> apsimFilePaths)
    {
        if (!Directory.Exists(directoryPathString))
        {
            throw new DirectoryNotFoundException("Directory not found: " + directoryPathString);
        }

        string workFloFileName = "workflow.yml";
        string workFloName = GetDirectoryName(directoryPathString);
        string exclusionPattern = "*.yml";
        string[] inputFiles = Directory.GetFiles(directoryPathString).Except(Directory.GetFiles(directoryPathString, exclusionPattern)).ToArray();
        string workFloFileContents = InitializeWorkFloFile(workFloName);
        workFloFileContents = AddInputFilesToWorkFloFile(workFloFileContents, inputFiles);
        workFloFileContents = AddTaskToWorkFloFile(workFloFileContents, inputFiles);
        string indent = "  ";
        workFloFileContents = AddInputFilesToWorkFloFile(workFloFileContents, inputFiles, indent);
        workFloFileContents = AddStepsToWorkFloFile(workFloFileContents, indent, apsimFilePaths);
        File.WriteAllText(Path.Combine(directoryPathString, workFloFileName), workFloFileContents);
    }

    /// <summary>
    /// Adds steps to the workflow file for each apsimx file present in the directory
    /// </summary>
    /// <param name="workFloFileContents"></param>
    /// <param name="indent"></param>
    /// <param name="apsimFilePaths"></param>
    /// <returns></returns>
    private static string AddStepsToWorkFloFile(string workFloFileContents, string indent, List<string> apsimFilePaths)
    {
        workFloFileContents += $"{indent}steps: "+ Environment.NewLine;
        foreach(string filePath in apsimFilePaths)
        {
            string apsimFileName = Path.GetFileName(filePath);
            workFloFileContents += $"""

                {indent}  - uses: apsiminitiative/apsimng
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
    /// 
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


}
