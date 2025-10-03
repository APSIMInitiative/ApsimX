namespace APSIM.Core;

internal class NewModelReference : IModelReference
{
    /// <summary>Type name of model to create.</summary>
    internal readonly string newModelType;

    /// <summary>
    /// Constructor - model is a child of a parent.
    /// </summary>
    /// <param name="newModelName">Type name of model to create.</param>
    public NewModelReference(string newModelType)
    {
        this.newModelType = newModelType;
    }

    /// <summary>
    /// Get the model. Throws if model not found.
    /// </summary>
    /// <returns>The model</returns>
    INodeModel IModelReference.GetModel() => ModelRegistry.CreateModel(newModelType);
}