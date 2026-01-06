using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class RunCommand: IModelCommand
{
    /// <summary>
    /// Create a run command.
    /// </summary>
    /// <param name="command">The command to parse.</param>
    public static IModelCommand Create(string command)
    {
        string pattern = $@"run";

        Match match = Regex.Match(command, pattern);
        if (match == null || !match.Success)
            throw new Exception($"Invalid run command: {command}");

        return new RunCommand();
    }

    /// <summary>
    /// Convert an save command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()=> $"run";

}