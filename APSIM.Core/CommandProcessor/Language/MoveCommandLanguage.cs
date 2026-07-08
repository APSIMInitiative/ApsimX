using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class MoveCommand: IModelCommand
{
    /// <summary>
    /// Create a Move command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <returns>A new model instance</returns>
    /// <remarks>
    /// move [Soil] before SurfaceOrganicMatter
    /// move [Soil] after SurfaceOrganicMatter
    /// </remarks>
    public static IModelCommand Create(string command)
    {
        string modelNameWithBrackets = @"[\w\d\[\]\.]+";
        string position = @"before|after";

        string pattern = $@"^move\s+(?<from>{modelNameWithBrackets})" +
                         $@"\s+(?<position>{position})" +
                         $@"\s+(?<to>{modelNameWithBrackets})$";

        Match match = Regex.Match(command, pattern);
        if (match == null || !match.Success)
            throw new Exception($"Invalid command: {command}");

        string fromPath = match.Groups["from"]?.ToString();
        string toPath = match.Groups["to"]?.ToString();
        bool before = true;
        if (match.Groups["position"]?.ToString() == "after")
            before = false;

        return new MoveCommand(fromPath, toPath, before);
    }

    /// <summary>
    /// Convert a Move command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()
    {
        string position = "before";
        if (!_placeBefore)
            position = "after";
        return $"move {_fromPath} {position} {_toPath}";
    }

}