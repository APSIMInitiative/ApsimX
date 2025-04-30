
namespace APSIM.Workflow;

public static class InputUtilities
{
    /// <summary>
    /// Gets the path of the apsimx file from the directory.
    /// </summary>
    /// <param name="zipFile"></param>
    /// <returns>a string</returns>
    /// <returns>a string</returns>
    /// <exception cref="Exception"></exception>
    public static string GetApsimXFileTextFromFile(string apsimFilePath)
    {
        string apsimxFileText;
        try
        {
        
            if (string.IsNullOrWhiteSpace(apsimFilePath))
            {
                throw new Exception("Error: APSIMX file not found while searching the directory.");
            }

            apsimxFileText = File.ReadAllText(apsimFilePath);

            if (string.IsNullOrWhiteSpace(apsimxFileText))
            {
                throw new Exception("Error: While getting apsimx file text, it was found to be null.");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error: Unable to read the APSIMX file at {apsimFilePath}.\n{ex.Message}");
        }

        return apsimxFileText;
    }


    /// <summary>
    /// Checks if the source and destination paths are the same.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static bool CheckIfSourceIsDestination(string source, string destination)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
            throw new Exception("Error: Source or destination path is null.");

        if (source.Equals(destination, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return false;
    }


    /// <summary>
    /// Standardises the file paths to use forward slashes.
    /// </summary>
    /// <param name="apsimFilePaths"></param>
    /// <returns></returns>
    public static List<string> StandardiseFilePaths(List<string> apsimFilePaths)
    {
        List<string> fixedPaths = new();
        foreach (string path in apsimFilePaths)
        {
            string newPath = path.Replace("\\", "/");
            fixedPaths.Add(newPath);
        }
        return fixedPaths;
    }


    /// <summary>
    /// Updates the weather file name path in the APSIMX file.
    /// </summary>
    /// <param name="apsimxFileText">The APSIMX file text.</param>
    /// <param name="oldPath">The old path of the weather file.</param>
    /// <param name="newPath">The new path of the weather file.</param>
    /// <param name="directoryPath">The directory path.</param>
    public static void UpdateWeatherFileNamePathInApsimXFile(string apsimxFileText, string oldPath, string newPath, Options options, string apsimFilePath)
    {
        string newApsimxFileText = apsimxFileText.Replace("\\\\", "\\").Replace(oldPath, newPath);

        if (string.IsNullOrWhiteSpace(options.DirectoryPath))
            throw new Exception("Error: Directory path is null while trying to update weather file path in APSIMX file.");

        string savePath = Path.Combine(options.DirectoryPath, Path.GetFileName(apsimFilePath)).Replace("\\", "/");

        try
        {
            File.WriteAllText(savePath, newApsimxFileText);
        }
        catch (Exception)
        {
            throw new Exception($"Unable to save new weather file path to weather file at :{savePath}");
        }

        if(options.Verbose)
        {
            Console.WriteLine("Successfully updated weather file path in " + savePath);
        }
    }
}