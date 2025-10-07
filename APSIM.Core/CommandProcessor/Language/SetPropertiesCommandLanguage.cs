using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class SetPropertiesCommand: IModelCommand
{
    /// <summary>
    /// Create a set properties command.
    /// </summary>
    /// <param name="command">Command string.</param>
    /// <returns>A new model instance</returns>
    /// <remarks>
    /// [Simulation].Name=NewName
    /// </remarks>
    public static IModelCommand Create(string command, INodeModel _)
    {
        string modelNameWithBrackets = @"[\w\d\[\]\.]+";

        string pattern = $@"(?<keyword>{modelNameWithBrackets})=(?<value>.+)";

        Match match;
        if ((match = Regex.Match(command, pattern)) == null)
            throw new Exception($"Invalid set property command: {command}");

        return new SetPropertiesCommand(match.Groups["keyword"]?.ToString(),
                                        match.Groups["value"]?.ToString());
    }

    /// <summary>
    /// Convert an set properties command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()=> $"{name}={value}";

}