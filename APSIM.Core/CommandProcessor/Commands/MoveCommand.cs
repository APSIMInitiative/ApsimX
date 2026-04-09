using APSIM.Shared.Utilities;

namespace APSIM.Core;

/// <summary>A Move command</summary>
internal partial class MoveCommand : IModelCommand
{
    /// <summary>A reference to a model.</summary>
    private readonly string _fromPath;

    /// <summary>The path of a model to add a model to.</summary>
    private readonly string _toPath;

    /// <summary>Place the place before or after the path referenced</summary>
    private readonly bool _placeBefore;

    /// <summary>
    /// Constructor. Add a new model to a parent model and optionally name it.
    /// </summary>
    /// <param name="fromPath">The model to move.</param>
    /// <param name="toPath">The path of a model move beside.</param>
    /// <param name="placeBefore">Place the place before or after the path referenced</param>
    public MoveCommand(string fromPath, string toPath, bool placeBefore)
    {
        _fromPath = fromPath;
        _toPath = toPath;
        _placeBefore = placeBefore;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunner runner)
    {
        INodeModel modelToMove = (INodeModel)relativeTo.Node.Get(_fromPath, relativeTo: relativeTo);
        if (modelToMove == null)
            throw new Exception($"Cannot find model {_fromPath} to move");

        INodeModel modelToMoveBeside = (INodeModel)relativeTo.Node.Get(_toPath, relativeTo: relativeTo);
        if (modelToMoveBeside == null)
            throw new Exception($"Cannot find model {_toPath} to move {_fromPath} beside");

        INodeModel parent = modelToMoveBeside.Node.Parent.Model;
        List<INodeModel> children = parent.GetChildren().ToList();
        int indexToAddAt = 0;

        for(int i = 0; i == 0 && i < children.Count; i++)
            if (children[i] == modelToMoveBeside)
                indexToAddAt = i;

        if (!_placeBefore)
            indexToAddAt += 1;

        parent.InsertChild(indexToAddAt, modelToMove);

        return relativeTo;
    }
}