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
    private readonly MatchType matchType;

    /// <summary>Name given to model after replacement</summary>
    [JsonProperty]
    private readonly string newName;

    /// <summary>Definition of how to </summary>
    public enum MatchType { Name, NameAndType, NameOrType };

    /// <summary>
    /// Constructor. Add a new model to a parent model and optionally name it.
    /// </summary>
    /// <param name="modelReference">The model to add.</param>
    /// <param name="replacementPath">The path of models to replace.</param>
    /// <param name="multiple">Do as many replacements as possible?</param>
    /// <param name="matchType">Match on name AND type? If false, will match on type OR name.</param>
    /// <param name="newName">Name given to model after replacement.</param>
    public ReplaceCommand(IModelReference modelReference, string replacementPath, bool multiple, MatchType matchType, string newName = null)
    {
        this.modelReference = modelReference;
        this.replacementPath = replacementPath;
        this.multiple = multiple;
        this.matchType = matchType;
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
            if (matchType == MatchType.NameAndType && modelToReplace.GetType().IsAssignableFrom(modelToAdd.GetType()))
                throw new Exception($"Model {replacementPath} is not of type {replacementPath}");
            modelsToReplace = [modelToReplace];
        }
        else
        {
            var replacementPathWithoutBrackets = replacementPath.Replace("[", string.Empty)
                                                                .Replace("]", string.Empty);
            modelsToReplace = relativeTo.Node.FindAll(name: replacementPathWithoutBrackets);
            if (matchType == MatchType.NameAndType)
            {
                modelsToReplace = modelsToReplace.Where(model => model.GetType().IsAssignableFrom(modelToAdd.GetType()));
            }
            else if (matchType == MatchType.NameOrType && !modelsToReplace.Any())
            {
                // didn't find any matches using name so try by type.
                Type t = ModelRegistry.ModelNameToType(replacementPathWithoutBrackets);
                if (t != null)
                    modelsToReplace = relativeTo.Node.FindAll(type: t);
            }
        }

        if (!multiple)
            modelsToReplace = modelsToReplace.Take(1);

        // Do model replacement.
        foreach (var modelToReplace in modelsToReplace.ToArray())  // Need the ToArray because modelsToReplace changes because of the ReplaceChild call.
        {
            var newModel = ReflectionUtilities.Clone(modelToAdd) as INodeModel;
            if (string.IsNullOrEmpty(newName))
                newModel.Rename(modelToReplace.Name);
            else
                newModel.Rename(newName);
            newModel.Enabled = modelToReplace.Enabled;
            modelToReplace.Node.Parent.ReplaceChild(modelToReplace, newModel);
        }

        return relativeTo;
    }

    /// <summary>
    /// Return a hash code - useful for unit testing.
    /// </summary>
    public override int GetHashCode()
    {
        return (modelReference.GetHashCode(), replacementPath, multiple, matchType, newName).GetHashCode();
    }
}