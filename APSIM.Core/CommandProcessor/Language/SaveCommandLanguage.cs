using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class SaveCommand: IModelCommand
{
    /// <summary>
    /// Create a save command.
    /// </summary>
    /// <param name="command">Command string.</param>
    /// <returns>A new model instance</returns>
    /// <remarks>
    /// save modifiedSim.apsimx
    /// </remarks>
    public static IModelCommand Create(string command, INodeModel _)
    {
        string fileName = @"[\w\d\.\\:]+";

        string pattern = $@"save\s+" +
                         $@"(?<filename>{fileName})";

        Match match;
        if ((match = Regex.Match(command, pattern)) == null)
            throw new Exception($"Invalid save command: {command}");

        return new SaveCommand(match.Groups["filename"]?.ToString());
    }

    /// <summary>
    /// Convert an save command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()=> $"save {fileName}";

}