using System.Text.RegularExpressions;

namespace APSIM.Core;

public partial class SetPropertyCommand: IModelCommand
{
    /// <summary>
    /// Create a set properties command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <param name="relativeToDirectory">Directory name that the command filenames are relative to</param>
    /// <returns>A new model instance</returns>
    /// <remarks>
    /// [Simulation].Name=NewName
    /// </remarks>
    public static IModelCommand Create(string command, string relativeToDirectory)
    {
        string modelNameWithBrackets = @"[\w\d\[\]\.\:]+";

        string pattern = $@"(?<keyword>{modelNameWithBrackets})\s*(?<operator>[-+=]+)\s*(?<pipe>\<)*\s*(?<value>[^\<]+)*";

        Match match = Regex.Match(command, pattern);
        if (match == null || !match.Success)
            throw new Exception($"Invalid command: {command}");

        string fileName = null;
        string value = match.Groups["value"]?.ToString();
        if (match.Groups["pipe"].ToString() == "<")
        {
            if (string.IsNullOrEmpty(value))
                throw new Exception($"Invalid command: {command}");
            fileName = value;
            value = null;
            if (relativeToDirectory != null)
                fileName = Path.GetFullPath(fileName, relativeToDirectory);
        }

        return new SetPropertyCommand(match.Groups["keyword"]?.ToString(),
                                        match.Groups["operator"]?.ToString(),
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
            return $"{name}{oper}{value}";
        else
            return $"{name}=<{fileName}";
    }

}