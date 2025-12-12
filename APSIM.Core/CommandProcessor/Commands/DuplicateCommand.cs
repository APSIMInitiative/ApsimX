using DeepCloner.Core;

namespace APSIM.Core;

/// <summary>A duplicate model command</summary>
internal partial class DuplicateCommand : IModelCommand
{
    /// <summary>The name of the model to duplicate.</summary>
    private readonly string modelName;

    /// <summary>The name of the new model.</summary>
    public readonly string newName;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="modelName">The name of a model to duplicate.</param>
    /// <param name="newName">The name of the new model.</param>
    public DuplicateCommand(string modelName, string newName)
    {
        this.modelName = modelName;
        this.newName = newName;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunner runner)
    {
        var modelToDuplicate = (INodeModel)relativeTo.Node.Get(modelName, relativeTo: relativeTo)
                               ?? throw new Exception($"Cannot find model {modelName}");
        var duplicatedModel = modelToDuplicate.DeepClone();
        if (!string.IsNullOrEmpty(newName))
            duplicatedModel.Rename(newName);
        modelToDuplicate.Node.Parent.AddChild(duplicatedModel);
        return relativeTo;
    }
}