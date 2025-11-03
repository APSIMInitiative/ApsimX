namespace APSIM.Core;

/// <summary>
/// This implementation of IModelReference will return a model model instance.
/// </summary>
public class ModelReference : IModelReference
{
    private readonly INodeModel model;

    /// <summary>Constructor</summary>
    public ModelReference(INodeModel model)
    {
        this.model = model;
    }

    /// <summary>
    /// Get the model. Throws if model not found.
    /// </summary>
    /// <returns>The model</returns>
    INodeModel IModelReference.GetModel() => model;
}