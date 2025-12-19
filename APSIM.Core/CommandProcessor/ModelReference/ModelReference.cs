using Newtonsoft.Json;

namespace APSIM.Core;

/// <summary>
/// This implementation of IModelReference will return a model model instance.
/// </summary>
/// <remarks>
/// The JsonProperty attribute below is needed for JSON serialisation which the APSIM.Server uses.
/// </remarks>
public class ModelReference : IModelReference
{
    [JsonProperty]
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

    /// <summary>
    /// Return a hash code - useful for unit testing.
    /// </summary>
    public override int GetHashCode()
    {
        return model.GetHashCode();
    }
}