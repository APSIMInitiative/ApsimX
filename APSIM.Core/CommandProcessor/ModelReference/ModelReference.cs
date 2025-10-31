namespace APSIM.Core;

/// <summary>
/// This implementation of IModelReference will locate a model using the locator.
/// As such the model will be located in scope if it has square brackets around it,
/// or it will follow a path if one is specified.
/// </summary>
internal class ModelReference : IModelReference
{
    /// <summary>The node the model reference is relative to.</summary>
    private readonly INodeModel relativeTo;

    /// <summary>Parent node containing commands..</summary>
    internal readonly string modelName;

    /// <summary>
    /// Constructor - model is a child of a parent.
    /// </summary>
    /// <param name="relativeTo">The node the model reference is relative to.</param>
    /// <param name="modelName">Name/path of model.</param>
    public ModelReference(INodeModel relativeTo, string modelName)
    {
        this.relativeTo = relativeTo;
        this.modelName = modelName;
    }

    /// <summary>
    /// Get the model. Throws if model not found.
    /// </summary>
    /// <returns>The model</returns>
    INodeModel IModelReference.GetModel()
    {
        return relativeTo.Node.Get(modelName) as INodeModel
            ?? throw new Exception($"Cannot find a child model named {modelName} with a parent named {relativeTo.Name}");
    }
}