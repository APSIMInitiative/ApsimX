using System.Text.RegularExpressions;
using APSIM.Shared.Utilities;
using SQLitePCL;

namespace APSIM.Core;

internal partial class SetPropertiesCommand: IModelCommand
{
    /// <summary>
    /// Create a set properties command.
    /// </summary>
    /// <param name="command">Command string.</param>
    /// <param name="relativeToDirectory">Directory name that the command filenames are relative to</param>
    /// <returns>A new model instance</returns>
    /// <remarks>
    /// [Simulation].Name=NewName
    /// </remarks>
    public static IModelCommand Create(string command, string relativeToDirectory)
    {
        string modelNameWithBrackets = @"[\w\d\[\]\.]+";

        string pattern = $@"(?<keyword>{modelNameWithBrackets})\s*=\s*(?<pipe>\<)*\s*(?<value>[^\<]+)";

        Match match = Regex.Match(command, pattern);
        if (match == null || !match.Success)
            throw new Exception($"Invalid command: {command}");

        string fileName = null;
        string value = match.Groups["value"]?.ToString();
        if (match.Groups["pipe"].ToString() == "<")
        {
            fileName = value;
            value = null;
            if (relativeToDirectory != null)
                fileName = Path.GetFullPath(fileName, relativeToDirectory);
        }

        return new SetPropertiesCommand(match.Groups["keyword"]?.ToString(),
                                        value,
                                        fileName);
    }

    /// <summary>
    /// Convert an set properties command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()
    {
        if (fileName == null)
            return $"{name}={value}";
        else
            return $"{name}=<{fileName}";
    }

}