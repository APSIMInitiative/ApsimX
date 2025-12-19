using APSIM.Shared.Utilities;

namespace APSIM.Core;

/// <summary>An add command</summary>
internal partial class AddCommand : IModelCommand
{
    /// <summary>A reference to a model.</summary>
    private readonly IModelReference modelReference;

    /// <summary>The path of a model to add a model to.</summary>
    private readonly string toPath;

    /// <summary>Do as many replacements as possible?</summary>
    private readonly bool multiple;

    /// <summary>A new name for the added model.</summary>
    private readonly string newName;

    /// <summary>
    /// Constructor. Add a new model to a parent model and optionally name it.
    /// </summary>
    /// <param name="modelReference">The model to add.</param>
    /// <param name="toPath">The path of a model to add a model to.</param>
    /// <param name="multiple">Do as many replacements as possible?</param>
    /// <param name="newName">A new name for the added model</param>
    public AddCommand(IModelReference modelReference, string toPath, bool multiple, string newName = null)
    {
        this.modelReference = modelReference;
        this.toPath = toPath;
        this.multiple = multiple;
        this.newName = newName;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunner runner)
    {
        INodeModel modelToAdd = modelReference.GetModel();
        if (!string.IsNullOrEmpty(newName))
            modelToAdd.Rename(newName);

        IEnumerable<INodeModel> toModels;

        if (multiple)
        {
            var toPathWithoutBrackets = toPath.Replace("[", string.Empty)
                                              .Replace("]", string.Empty);
            Type t = ModelRegistry.ModelNameToType(toPathWithoutBrackets);
            toModels = relativeTo.Node.FindAll(type: t);
            if (!toModels.Any())
                 throw new Exception($"Cannot find any models that match: {toPath}");
        }
        else
        {
            var toModel = (INodeModel)relativeTo.Node.Get(toPath, relativeTo: relativeTo)
                 ?? throw new Exception($"Cannot find model: {toPath}");
            toModels = [toModel];
        }

        // Add a model to all toModels.
        foreach (var toModel in toModels.ToArray())  // Need the ToArray because toModels changes because of the AddChild.
            toModel.Node.AddChild(ReflectionUtilities.Clone(modelToAdd) as INodeModel);

        return relativeTo;
    }
}