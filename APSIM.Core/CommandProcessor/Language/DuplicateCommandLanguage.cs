using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class DuplicateCommand: IModelCommand
{
    /// <summary>
    /// Create a duplicate command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    /// <returns>A new model instance</returns>
    /// <remarks>
    /// duplicate [Simulation] name SimulationCopy
    /// </remarks>
    public static IModelCommand Create(string command)
    {
        string modelNameWithBrackets = @"[\w\d\[\]\.]+";
        string modelName = @"[\w\d]+";

        string pattern = $@"duplicate (?<modelname>{modelNameWithBrackets})" +
                         $@"(?:\s+name\s+(?<name>{modelName}))*";

        Match match = Regex.Match(command, pattern);
        if (match == null || !match.Success)
            throw new Exception($"Invalid command: {command}");

        return new DuplicateCommand(match.Groups["modelname"]?.ToString(),
                                    match.Groups["name"]?.ToString());
    }

    /// <summary>
    /// Convert an Duplicate command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()
    {
        string command = $"duplicate {modelName}";
        if (!string.IsNullOrEmpty(newName))
            command += $" name {newName}";
        return command;
    }

}