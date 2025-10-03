using System.Text.RegularExpressions;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using Microsoft.CodeAnalysis;

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
/// save modifiedSim.apsimx
///
/// use [BaseSimulation]
/// define factor sow
///     [Sowing].Date = 2020-06-01, 2020-07-01, 2020-08-01
/// define factor nrate
///     [FertiliseOnFixedDate].Amount = 0 to 200 step 10
/// apply sow x nrate
/// </remarks>
public class CommandLanguage
{
    public static IEnumerable<IModelCommand> StringToCommands(IEnumerable<string> lines, INodeModel parent)
    {
        List<IModelCommand> commands = new();

        // Convert lines like:
        //    define factor sow
        //       [Sowing].Date = 2020-06-01, 2020-07-01, 2020-08-01
        // into a command (e.g. define) and a series of parameters:
        //    factor
        //    sow
        //    [Sowing].Date = 2020-06-01, 2020-07-01, 2020-08-01
        // The command and parameters are then converted into a IModelCommand

        string command = null;
        List<string> parameters = new();
        foreach (var line in lines)
        {
            if (line.StartsWith(' '))
            {
                // add to existing command.
                parameters.Add(line);
            }
            else
            {
                // new command. If there is an existing command then add it to the
                if (command != null)
                {
                    commands.Add(ConvertCommandParameterToModelCommand(command, parameters, parent));
                    command = null;
                    parameters.Clear();
                }

                string[] tokens = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 2)
                    throw new Exception($"Invalid command line: {line}");
                command = tokens.First();             // first token is the command e.g. add
                parameters.AddRange(tokens.Skip(1));  // the remaining tokens are each a parameter
            }
        }
        if (command != null)
            commands.Add(ConvertCommandParameterToModelCommand(command, parameters, parent));

        return commands;
    }

    public static IEnumerable<string> CommandsToString(IEnumerable<IModelCommand> commands)
    {
        List<string> lines = new();
        foreach (var command in commands)
        {
            lines.Add(command.ToString());
        }
        return lines;
    }

    private static IModelCommand ConvertCommandParameterToModelCommand(string command, List<string> parameters, INodeModel parent)
    {
        string commandLower = command.ToLower();
        if (commandLower == "add")
            return AddCommand.Create(parameters, parent);

        throw new Exception($"Unknown command: {command}");
    }
}