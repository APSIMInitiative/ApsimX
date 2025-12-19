namespace APSIM.Core;


/// <summary>
/// This implementation of IModelReference will create a new instance of a model when GetModel is called.
/// </summary>
internal class NewModelReference : IModelReference
{
    /// <summary>Type name of model to create.</summary>
    internal readonly string newModelType;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="newModelType">Type type name of model to create.</param>
    public NewModelReference(string newModelType)
    {
        this.newModelType = newModelType;
    }

    /// <summary>
    /// Get the model by creating a new instance. Throws if model not found.
    /// </summary>
    /// <returns>The model</returns>
    INodeModel IModelReference.GetModel() => ModelRegistry.CreateModel(newModelType);
}