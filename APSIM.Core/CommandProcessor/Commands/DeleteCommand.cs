namespace APSIM.Core;

/// <summary>A delete model command</summary>
internal partial class DeleteCommand : IModelCommand
{
    /// <summary>The name or path of the model to delete.</summary>
    private readonly string _modelName;

    /// <summary>Do as many deletes as possible?</summary>
    private readonly bool _multiple;

    /// <summary>The name or path of the model to delete children from.</summary>
    private readonly string _parentModelName;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="modelName">The name or path of a model to delete.</param>
    /// <param name="multiple">Whether to delete every matching model.</param>
    /// <param name="parentModelName">The name or path of the model containing models to delete.</param>
    public DeleteCommand(string modelName, bool multiple, string parentModelName = null)
    {
        _modelName = modelName;
        _multiple = multiple;
        _parentModelName = parentModelName;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunner runner, List<Node> externalFileCache)
    {
        INodeModel parentModel = relativeTo;
        if (!string.IsNullOrEmpty(_parentModelName))
        {
            parentModel = relativeTo.Node.Get(_parentModelName) as INodeModel;
            if (parentModel == null)
                throw new Exception($"Cannot find model {_parentModelName}");
        }

        IEnumerable<INodeModel> modelsToDelete;

        if (_multiple)
        {
            modelsToDelete = parentModel.Node.GetAllObjects(_modelName, LocatorFlags.ModelsOnly)
                .Select(match => match.Value as INodeModel)
                .Where(model => model != null)
                .ToArray();
        }
        else
        {
            INodeModel model = parentModel.Node.Get(_modelName) as INodeModel;
            if (model == null)
                throw new Exception($"Cannot find model {_modelName}");
            modelsToDelete = [model];
        }

        foreach (INodeModel model in modelsToDelete)
        {
            // Throw exception if root node.
            if (model.Node.Parent == null)
                throw new Exception($"Command 'delete [Simulations]' is an invalid command. [Simulations] node is the" +
                    $" top-level node and cannot be deleted. Remove the command from your config file.");
            model.Node.Parent.RemoveChild(model);
        }            
        return relativeTo;
    }
}