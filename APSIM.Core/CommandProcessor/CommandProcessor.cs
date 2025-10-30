namespace APSIM.Core;

/// <summary>
/// A command processor for running commands.
/// </summary>
public class CommandProcessor
{
    /// <summary>
    /// Run all commands.
    /// </summary>
    /// <param name="relativeTo">The commands will be run relative to this argument.</param>
    public static void Run(IEnumerable<IModelCommand> commands, INodeModel relativeTo, IRunner runner)
    {
        var localRelativeTo = relativeTo;

        foreach (var command in commands)
            localRelativeTo = command.Run(localRelativeTo, runner);
    }
}