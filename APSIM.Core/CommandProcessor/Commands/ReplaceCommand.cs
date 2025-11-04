using APSIM.Shared.Utilities;
using Newtonsoft.Json;

namespace APSIM.Core;

/// <summary>A replace command</summary>
/// <remarks>
/// The JsonProperty attributes below are needed for JSON serialisation which the APSIM.Server uses.
/// </remarks>
public partial class ReplaceCommand : IModelCommand
{
    /// <summary>A reference to a model.</summary>
    [JsonProperty]
    private readonly IModelReference modelReference;

    /// <summary>The path of models to replace.</summary>
    [JsonProperty]
    private readonly string replacementPath;

    /// <summary>Do as many replacements as possible?</summary>
    [JsonProperty]
    private readonly bool multiple;

    /// <summary>Match on name and type? If false, will only match when names match.</summary>
    [JsonProperty]
    private readonly bool matchOnNameAndType;

    /// <summary>Name given to model after replacement</summary>
    [JsonProperty]
    private readonly string newName;


    /// <summary>
    /// Constructor. Add a new model to a parent model and optionally name it.
    /// </summary>
    /// <param name="modelReference">The model to add.</param>
    /// <param name="replacementPath">The path of models to replace.</param>
    /// <param name="multiple">Do as many replacements as possible?</param>
    /// <param name="matchOnNameAndType">Match on name and type? If false, will only match when names match.</param>
    /// <param name="newName">Name given to model after replacement.</param>
    public ReplaceCommand(IModelReference modelReference, string replacementPath, bool multiple, bool matchOnNameAndType, string newName = null)
    {
        this.modelReference = modelReference;
        this.replacementPath = replacementPath;
        this.multiple = multiple;
        this.matchOnNameAndType = matchOnNameAndType;
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

        IEnumerable<INodeModel> modelsToReplace;
        if (replacementPath.Contains('.'))
        {
            var modelToReplace = (INodeModel)relativeTo.Node.Get(replacementPath)
                 ?? throw new Exception($"Cannot find model: {replacementPath}");
            if (matchOnNameAndType && modelToReplace.GetType().IsAssignableFrom(modelToAdd.GetType()))
                throw new Exception($"Model {replacementPath} is not of type {replacementPath}");
            modelsToReplace = [modelToReplace];
        }
        else
        {
            var replacementPathWithoutBrackets = replacementPath.Replace("[", string.Empty)
                                                                .Replace("]", string.Empty);
            modelsToReplace = relativeTo.Node.FindAll(name: replacementPathWithoutBrackets);
            if (matchOnNameAndType)
            {
                modelsToReplace = modelsToReplace.Where(model => model.GetType().IsAssignableFrom(modelToAdd.GetType()));
            }
        }

        if (!multiple)
            modelsToReplace = modelsToReplace.Take(1);

        // Do model replacement.
        foreach (var modelToReplace in modelsToReplace.ToArray())  // Need the ToArray because modelsToReplace changes because of the ReplaceChild call.
        {
            var newModel = ReflectionUtilities.Clone(modelToAdd) as INodeModel;
            if (!string.IsNullOrEmpty(newName))
                newModel.Rename(newName);
            modelToReplace.Node.Parent.ReplaceChild(modelToReplace, newModel);
        }

        return relativeTo;
    }

    /// <summary>
    /// Return a hash code - useful for unit testing.
    /// </summary>
    public override int GetHashCode()
    {
        return (modelReference.GetHashCode(), replacementPath, multiple, matchOnNameAndType, newName).GetHashCode();
    }
}