namespace APSIM.Core;

/// <summary>A delete model commnd</summary>
public class DeleteCommand : IModelCommand
{
    // <summary>The name of the model to delete.</summary>
    private readonly string modelName;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="modelName">The name of a model to delete.</param>
    public DeleteCommand(string modelName)
    {
        this.modelName = modelName;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    void IModelCommand.Run(INodeModel relativeTo)
    {
        var modelToDelete = (INodeModel)relativeTo.Node.Get(modelName, relativeTo: relativeTo)
                           ?? throw new Exception($"Cannot find model {modelName}");
        modelToDelete.Node.Parent.RemoveChild(modelToDelete);
    }
}