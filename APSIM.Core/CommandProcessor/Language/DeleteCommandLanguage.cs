using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class DeleteCommand: IModelCommand
{
    /// <summary>
    /// Create a delete command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <remarks>
    /// delete [Zone].Report
    /// </remarks>
    public static IModelCommand Create(string command)
    {
        string modelNameWithBrackets = @"[\w\d\[\]\.]+";

        string pattern = $@"delete (?<modelname>{modelNameWithBrackets})";

        Match match = Regex.Match(command, pattern);
        if (match == null || !match.Success)
            throw new Exception($"Invalid command: {command}");

        return new DeleteCommand(match.Groups["modelname"]?.ToString());
    }

    /// <summary>
    /// Convert an DeleteCommand instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString() => $"delete {modelName}";
}