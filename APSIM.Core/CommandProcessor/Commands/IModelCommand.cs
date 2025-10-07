namespace APSIM.Core;

public interface IModelCommand
{
    internal INodeModel Run(INodeModel relativeTo);
}