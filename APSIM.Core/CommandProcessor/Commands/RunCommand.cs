namespace APSIM.Core;

/// <summary>A run APSIM model command</summary>
/// <remarks>
/// Currently, APSIM.Core doesn't have any code for running APSIM. To get around this, this class
/// relies on a IRunner interface to run APSIM which can be implemented somewhere else e.g. Models.
/// </remarks>
internal partial class RunCommand : IModelCommand
{
    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunner runner)
    {
        runner.Run(relativeTo);
        return relativeTo;
    }
}