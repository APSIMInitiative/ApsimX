namespace APSIM.Core;

/// <summary>
/// Defines an interface for a class that can run APSIM.
/// </summary>
public interface IRunAPSIM
{
    /// <summary>
    /// Run APSIM for all simulations.
    /// </summary>
    /// <param name="relativeTo">The model that defines the scope for running APSIM.</param>
    void Run(INodeModel relativeTo);
}