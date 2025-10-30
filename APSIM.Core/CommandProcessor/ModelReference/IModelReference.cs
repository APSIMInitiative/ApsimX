namespace APSIM.Core;

/// <summary>
/// This interface defines a class that can get a model. The interface doesn't define HOW it
/// gets the model.
/// </summary>
internal interface IModelReference
{
    /// <summary>Get the model.</summary>
    INodeModel GetModel();
}