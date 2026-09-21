namespace APSIM.Core;

/// <summary>A load file command</summary>
internal partial class LoadCommand : IModelCommand, ICommandReferenceExternalFile
{
    /// <summary>The name of the file to load into memory.</summary>
    private readonly string _fileName;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="fileName">The name of a file to load.</param>
    public LoadCommand(string fileName)
    {
        this._fileName = fileName;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunner runner, List<Node> externalFileCache)
    {
        if (externalFileCache != null)
            foreach(Node node in externalFileCache)
                if (node.FileName == _fileName)
                    return node.Model;
        
        //if file wasn't in cache, try loading now
        Type simulationsType = ModelRegistry.ModelNameToType("Simulations");
        Node externalRootNode = FileFormat.ReadFromFile(_fileName, simulationsType);
        return externalRootNode.Model;
    }

    /// <summary>
    /// Returns the potential filepath for this command.
    /// </summary>
    public string GetFilePath()
    {
        return _fileName;
    }
}