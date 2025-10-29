namespace APSIM.Core;

/// <summary>A load file command</summary>
internal partial class LoadCommand : IModelCommand
{
    /// <summary>The name of the file to load into memory.</summary>
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
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunner runner)
    {
        var simulationsType = ModelRegistry.ModelNameToType("Simulations");
        var externalRootNode = FileFormat.ReadFromFile(fileName, simulationsType);
        return externalRootNode.Model;
    }
}