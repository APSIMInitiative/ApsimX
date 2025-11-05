using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class SaveCommand: IModelCommand
{
    /// <summary>
    /// Create a save command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <param name="relativeToDirectory">Directory name that the command filenames are relative to</param>
    /// <returns>A new model instance</returns>
    /// <remarks>
    /// save modifiedSim.apsimx
    /// </remarks>
    public static IModelCommand Create(string command, string relativeToDirectory)
    {
        string fileNamePattern = @"[\w\d-_\.\\:/]+";

        string pattern = $@"save\s+" +
                         $@"(?<filename>{fileNamePattern})";

        Match match = Regex.Match(command, pattern);
        if (match == null || !match.Success)
            throw new Exception($"Invalid command: {command}");

        // Get filename to convert to absolute path if necessary.
        string fileName = match.Groups["filename"].ToString();
        if (relativeToDirectory != null)
            fileName = Path.GetFullPath(fileName, relativeToDirectory);

        return new SaveCommand(fileName);
    }

    /// <summary>
    /// Convert an save command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()=> $"save {fileName}";

}