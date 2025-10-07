using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class DeleteCommand: IModelCommand
{
    /// <summary>
    /// Create a delete command.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    /// <remarks>
    /// delete [Zone].Report
    /// </remarks>
    public static IModelCommand Create(string command, INodeModel parent)
    {
        string modelNameWithBrackets = @"[\w\d\[\]\.]+";

        string pattern = $@"delete (?<modelname>{modelNameWithBrackets})";

        Match match;
        if ((match = Regex.Match(command, pattern)) == null)
            throw new Exception($"Invalid delete command: {command}");

        return new DeleteCommand(match.Groups["modelname"]?.ToString());
    }

    /// <summary>s
    /// Convert an DeleteCommand instance to a string.
    /// </summary>
    /// <param name="command">The DeleteCommand instance.</param>
    /// <returns>A command language string.</returns>
    public override string ToString() => $"delete {modelName}";
}