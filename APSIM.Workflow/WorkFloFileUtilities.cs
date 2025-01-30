using System;
using System.IO;

namespace APSIM.Workflow;
public static class WorkFloFileUtilities
{
    /// <summary>
    /// Creates a validation workflow file in the specified directory.
    /// </summary>
    /// <param name="directoryPathString"></param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static void CreateValidationWorkFloFile(string directoryPathString)
    {
        if (!Directory.Exists(directoryPathString))
        {
            throw new DirectoryNotFoundException("Directory not found: " + directoryPathString);
        }

        string workFloFileName = "workflow.yml";
        string workFloName = GetDirectoryName(directoryPathString);
        string exclusionPattern = "*.yml";
        string[] inputFiles = Directory.GetFiles(directoryPathString).Except(Directory.GetFiles(directoryPathString, exclusionPattern)).ToArray();
        // Initialize the workflow file with the name and input files statement.
        string workFloFileContents = $"""
        name: {workFloName}
        inputfiles:{Environment.NewLine}
        """;
        AddInputFilesToWorkFloFile(ref workFloFileContents, inputFiles);
        File.WriteAllText(workFloFileName, workFloFileContents);
    }

    /// <summary>
    /// Adds input file lines to the workflow file with correct indentation.
    /// </summary>
    /// <param name="workfloFileText"></param>
    public static void AddInputFilesToWorkFloFile(ref string workfloFileText, string[] inputFiles)
    {
        foreach (string file in inputFiles)
        {
            string inputFileName = GetDirectoryName(file);
            workfloFileText += "- " + inputFileName + Environment.NewLine;
        }
    }

    /// <summary>
    /// Gets the directory name from the specified directory path.
    /// </summary>
    /// <param name="directoryPathString"></param>
    public static string GetDirectoryName(string directoryPathString)
    {
        string dirName = Path.GetFileName(Path.GetDirectoryName(directoryPathString));
        return dirName;
    }
}
