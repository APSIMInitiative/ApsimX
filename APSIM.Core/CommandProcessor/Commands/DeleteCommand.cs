namespace APSIM.Core;

/// <summary>A delete model command</summary>
internal partial class DeleteCommand : IModelCommand
{
    /// <summary>The name of the model to delete.</summary>
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
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunner runner)
    {
        var modelToDelete = (INodeModel)relativeTo.Node.Get(modelName, relativeTo: relativeTo)
                           ?? throw new Exception($"Cannot find model {modelName}");

        // Throw exception if root node.
        if (modelToDelete.Node.Parent == null)
            throw new Exception($"Command 'delete [Simulations]' is an invalid command. [Simulations] node is the top-level node and cannot be deleted. Remove the command from your config file.");

        modelToDelete.Node.Parent.RemoveChild(modelToDelete);
        return relativeTo;
    }
}