using Topten.RichTextKit.Utils;

namespace APSIM.Core;

/// <summary>
/// A command processor for running commands.
/// </summary>
public class CommandProcessor
{
    /// <summary>A collection of commands to run.</summary>
    private readonly List<IModelCommand> commands;

    /// <summary>A collection of commands to run.</summary>
    private readonly IRunner runner;


    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="commands">A collection of commands.</param>
    /// <param name="runner">An instance of a class that can run APSIM</param>
    public CommandProcessor(IEnumerable<IModelCommand> commands, IRunner runner)
    {
        this.commands = commands.ToList();
        this.runner = runner;
    }

    /// <summary>The node that the commands are relative to. The load command changes this.</summary>
    internal INodeModel RelativeTo { get; set; }

    /// <summary>
    /// Run all commands.
    /// </summary>
    /// <param name="relativeTo">The commands will be run relative to this argument.</param>
    public void Run(INodeModel relativeTo)
    {
        RelativeTo = relativeTo;

        foreach (var command in commands)
            RelativeTo = command.Run(RelativeTo, runner);
    }
}