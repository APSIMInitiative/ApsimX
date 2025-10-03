namespace APSIM.Core;

internal interface IModelReference
{
    /// <summary>Get the model.</summary>
    INodeModel GetModel();
}