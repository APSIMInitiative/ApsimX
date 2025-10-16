namespace APSIM.Core;

internal class ModelReference : IModelReference
{
    /// <summary>Parent node containing commands..</summary>
    private readonly INodeModel parent;

    /// <summary>Parent node containing commands..</summary>
    internal readonly string childModelName;

    /// <summary>
    /// Constructor - model is a child of a parent.
    /// </summary>
    /// <param name="parent">Parent model.</param>
    /// <param name="childModelName">Name of child model.</param>
    public ModelReference(INodeModel parent, string childModelName)
    {
        this.parent = parent;
        this.childModelName = childModelName;
    }

    /// <summary>
    /// Get the model. Throws if model not found.
    /// </summary>
    /// <returns>The model</returns>
    INodeModel IModelReference.GetModel()
    {
        return parent.Node.Get(childModelName) as INodeModel
            ?? throw new Exception($"Cannot find a child model named {childModelName} with a parent named {parent.Name}");
    }
}