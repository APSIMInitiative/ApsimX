namespace APSIM.Core;

/// <summary>A load model commnd</summary>
internal partial class LoadCommand : IModelCommand
{
    // <summary>The name of the model to delete.</summary>
    private readonly string fileName;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fileName">The name of a file to load.</param>
    public LoadCommand(string fileName)
    {
        this.fileName = fileName;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo)
    {
        var simulationsType = ModelRegistry.ModelNameToType("Simulations");
        string json = File.ReadAllText(fileName);
        (var externalRootNode, var didConvert, var jsonObject) = FileFormat.ReadFromStringAndReturnConvertState(json, simulationsType);
        return externalRootNode.Model;
    }
}