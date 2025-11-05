using System.Text;
using APSIM.Shared.Utilities;

namespace APSIM.Core;

/// <summary>
/// Implements the command language
/// </summary>
/// <remarks>
/// add Report to [Zone]
/// add child Report to [Zone] name MyReport
/// add Report to all [Zone]
/// add [Report] from anotherfile.apsimx to [Zone]
/// replace all [Wheat] with child NewWheat
/// replace all [Wheat] with NewWheat from anotherfile.apsimx
/// replace all [Wheat] with child NewWheat
/// delete [Zone].Report
/// duplicate [Simulation] name SimulationCopy
/// [Simulation].Name=NewName
/// run APSIM
/// load base.apsimx
/// save modifiedSim.apsimx
/// </remarks>
public class CommandLanguage
{
    /// <summary>
    /// Parse a collection of lines into a collection of model commands.
    /// </summary>
    /// <param name="lines">Collection of strings, one for each line.</param>
    /// <param name="relativeTo">The node owning the collection of strings.</param>
    /// <param name="relativeToDirectory">Directory that file names are relative to.</param>
    /// <returns></returns>
    public static IEnumerable<IModelCommand> StringToCommands(IEnumerable<string> lines, INodeModel relativeTo, string relativeToDirectory)
    {
        List<IModelCommand> commands = [];

        // Combine indented lines with above line. e.g. convert lines like:
        //    define factor sow
        //       [Sowing].Date = 2020-06-01, 2020-07-01, 2020-08-01
        // into:
        //    define factor sow [Sowing].Date = 2020-06-01, 2020-07-01, 2020-08-01
        // The command and parameters are then converted into a IModelCommand

        StringBuilder command = null;
        foreach (var line in lines)
        {
            // Strip commented characters.
            string sanitisedLine = line.Replace("//", "#");
            int posComment = sanitisedLine.IndexOf('#');
            if (posComment != -1)
                sanitisedLine = sanitisedLine.Remove(posComment);

            sanitisedLine = sanitisedLine.TrimEnd();

            if (sanitisedLine.StartsWith(' '))
            {
                // Add to existing command.
                command.Append(sanitisedLine.Trim());
            }
            else if (sanitisedLine != string.Empty)
            {
                // New command. If there is an existing command then add it to the commands collection.
                if (command == null)
                    command = new();
                else
                    commands.Add(ConvertCommandParameterToModelCommand(command.ToString(), relativeTo, relativeToDirectory));

                command.Clear();
                command.Append(sanitisedLine.Trim());
            }
        }
        if (command != null)
            commands.Add(ConvertCommandParameterToModelCommand(command.ToString(), relativeTo, relativeToDirectory));

        return commands;
    }

    /// <summary>
    /// Convert a collection of commands into strings.
    /// </summary>
    /// <param name="commands">The model commands.</param>
    /// <returns>A collection of lines.</returns>
    public static IEnumerable<string> CommandsToStrings(IEnumerable<IModelCommand> commands)
    {
        List<string> lines = [];
        foreach (var command in commands)
            lines.Add(command.ToString());
        return lines;
    }

    /// <summary>
    /// Create a model command from a string.
    /// </summary>
    /// <param name="command">Command string.</param>
    /// <param name="relativeTo">The node that owns the command string.</param>
    /// <param name="relativeToDirectory">Directory name that the command filenames are relative to</param>
    /// <returns>An instance of a model command.</returns>
    private static IModelCommand ConvertCommandParameterToModelCommand(string command, INodeModel relativeTo, string relativeToDirectory)
    {
        command = command.Trim();
        if (command.StartsWith("add", StringComparison.InvariantCultureIgnoreCase))
            return AddCommand.Create(command, relativeTo, relativeToDirectory);
        else if (command.StartsWith("delete", StringComparison.InvariantCultureIgnoreCase))
            return DeleteCommand.Create(command);
        else if (command.StartsWith("duplicate", StringComparison.InvariantCultureIgnoreCase))
            return DuplicateCommand.Create(command);
        else if (command.StartsWith("save", StringComparison.InvariantCultureIgnoreCase))
            return SaveCommand.Create(command, relativeToDirectory);
        else if (command.StartsWith("load", StringComparison.InvariantCultureIgnoreCase))
            return LoadCommand.Create(command, relativeToDirectory);
        else if (command.StartsWith("run", StringComparison.InvariantCultureIgnoreCase))
            return RunCommand.Create(command);
        else if (command.Contains('='))
            return SetPropertyCommand.Create(command, relativeToDirectory);

        throw new Exception($"Unknown command: {command}");
    }
}