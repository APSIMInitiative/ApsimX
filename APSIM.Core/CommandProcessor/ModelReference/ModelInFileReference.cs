namespace APSIM.Core;

/// <summary>
/// This implementation of IModelReference will locate a model in an external file.
/// </summary>
internal class ModelInFileReference : IModelReference
{
    /// <summary>File name.</summary>
    internal readonly string fileName;

    /// <summary>The name of the model to locate in the file.</summary>
    internal readonly string modelName;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="fileName">File name.</param>
    /// <param name="modelName">The name of the model to locate in the file.</param>
    public ModelInFileReference(string fileName, string modelName)
    {
        this.fileName = fileName;
        this.modelName = modelName;
    }

    /// <summary>
    /// Get the model. Throws if model not found.
    /// </summary>
    /// <returns>The model</returns>
    INodeModel IModelReference.GetModel()
    {
        var simulationsType = ModelRegistry.ModelNameToType("Simulations");
        string json = File.ReadAllText(fileName);
        (var externalRootNode, _, _) = FileFormat.ReadFromStringAndReturnConvertState(json, simulationsType);
        return (INodeModel)externalRootNode.Get(modelName)
            ?? throw new Exception($"Cannot find model {modelName} in file {fileName}");
    }
}