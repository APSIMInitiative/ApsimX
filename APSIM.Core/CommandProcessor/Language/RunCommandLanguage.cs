using System.Text.RegularExpressions;

namespace APSIM.Core;

internal partial class RunCommand: IModelCommand
{
    /// <summary>
    /// Create a run command.
    /// </summary>
    /// <param name="command">Command string.</param>
    /// <returns>A new model instance</returns>
    /// <remarks>
    /// run
    /// </remarks>
    public static IModelCommand Create(string command, INodeModel _)
    {
        string pattern = $@"run";

        Match match;
        if ((match = Regex.Match(command, pattern)) == null)
            throw new Exception($"Invalid run command: {command}");

        return new RunCommand();
    }

    /// <summary>
    /// Convert an save command instance to a string.
    /// </summary>
    /// <returns>A command language string.</returns>
    public override string ToString()=> $"run";

}