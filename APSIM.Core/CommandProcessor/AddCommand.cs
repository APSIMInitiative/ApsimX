namespace APSIM.Core;

/// <summary>An add commnd</summary>
public class AddCommand : IModelCommand
{
    /// <summary>The name of a model to add.</summary>
    private readonly string modelName;

    /// <summary>The name of a .apsimx file to get new model from.</summary>
    private readonly string fileName;

    /// <summary>The path of a model to add a model to.</summary>
    private readonly string toPath;

    /// <summary>A new name for the added model.</summary>
    private readonly string newName;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="modelName">The name of a model to add.</param>
    /// <param name="toPath">The path of a model to add a model to.</param>
    /// <param name="fileName">The name of a .apsimx file to get new model from.</param>
    /// <param name="newName">A new name for the added model</param>
    public AddCommand(string modelName, string toPath, string fileName = null, string newName = null)
    {
        this.modelName = modelName;
        this.toPath = toPath;
        this.fileName = fileName;
        this.newName = newName;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    void IModelCommand.Run(INodeModel relativeTo)
    {
        var toModel = (INodeModel) relativeTo.Node.Get(toPath, relativeTo: relativeTo)
                        ?? throw new Exception($"Cannot find model {toPath}");

        INodeModel modelToAdd;
        if (fileName == null)
            modelToAdd = ModelRegistry.CreateModel(modelName);
        else
        {
            var simulationsType = ModelRegistry.ModelNameToType("Simulations");
            string json = File.ReadAllText(fileName);
            (var externalRootNode, var didConvert, var jsonObject) = FileFormat.ReadFromStringAndReturnConvertState(json, simulationsType);
            modelToAdd = (INodeModel) relativeTo.Node.Get(modelName, relativeTo: externalRootNode.Model)
                            ?? throw new Exception($"Cannot find model {toPath}");
        }
        if (!string.IsNullOrEmpty(newName))
            modelToAdd.Rename(newName);

        toModel.Node.AddChild(modelToAdd);
    }
}