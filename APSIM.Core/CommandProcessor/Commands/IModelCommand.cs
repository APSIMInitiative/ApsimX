namespace APSIM.Core;

/// <summary>
/// An interface for a model command.
/// </summary>
public interface IModelCommand
{
    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    /// <param name="runner">An instance of an APSIM runner.</param>
    internal INodeModel Run(INodeModel relativeTo, IRunner runner);
}