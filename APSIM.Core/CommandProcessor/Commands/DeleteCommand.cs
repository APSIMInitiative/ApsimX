namespace APSIM.Core;

/// <summary>A delete model command</summary>
internal partial class DeleteCommand : IModelCommand
{
    /// <summary>The name of the model to delete.</summary>
    private readonly string _modelName;

    /// <summary>Do as many deletes as possible?</summary>
    private readonly bool _multiple;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="modelName">The name of a model to delete.</param>
    /// <param name="multiple">Wether to delete everything that matches</param>
    public DeleteCommand(string modelName, bool multiple)
    {
        _modelName = modelName;
        _multiple = multiple;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunner runner, List<Node> externalFileCache)
    {
        List<INodeModel> modelsToDelete = new List<INodeModel>();
        if (_multiple)
        {
            IEnumerable<VariableComposite> matches = relativeTo.Node.GetAllObjects(_modelName, LocatorFlags.ModelsOnly);
            foreach(VariableComposite match in matches)
                modelsToDelete.Add(match.Value as INodeModel);
        }
        else
        {
            INodeModel model = (INodeModel)relativeTo.Node.Get(_modelName, relativeTo: relativeTo);
            if (model != null)
                modelsToDelete.Add(model);
            else
                throw new Exception($"Cannot find model {_modelName}");
            if (!modelsToDelete.Any())
                throw new Exception($"Cannot find any models that match: {_modelName}");
        }

        foreach (INodeModel model in modelsToDelete)
        {
             // Throw exception if root node.
            if (model.Node.Parent == null)
                throw new Exception($"Command 'delete [Simulations]' is an invalid command. [Simulations] node is the top-level node and cannot be deleted. Remove the command from your config file.");
            model.Node.Parent.RemoveChild(model);
        }
       
        return relativeTo;
    }
}