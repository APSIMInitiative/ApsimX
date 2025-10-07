namespace APSIM.Core;

public interface IModelCommand
{
    internal void Run(INodeModel relativeTo);
}