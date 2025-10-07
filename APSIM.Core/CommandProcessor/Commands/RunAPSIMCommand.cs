namespace APSIM.Core;

/// <summary>A duplicate model commnd</summary>
public class RunAPSIMCommand : IModelCommand
{
    INodeModel IModelCommand.Run(INodeModel relativeTo)
    {
        throw new NotImplementedException();
    }
}