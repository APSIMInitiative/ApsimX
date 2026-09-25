using APSIM.Shared.Utilities;
using Newtonsoft.Json;

namespace APSIM.Core;

/// <summary>A replace command</summary>
/// <remarks>
/// The JsonProperty attributes below are needed for JSON serialisation which the APSIM.Server uses.
/// </remarks>
public partial class ReplaceCommand : IModelCommand, ICommandReferenceExternalFile
{
    /// <summary>A reference to a model.</summary>
    [JsonProperty]
    private readonly IModelReference _modelReference;

    /// <summary>The path of models to replace.</summary>
    [JsonProperty]
    private readonly string _replacementPath;

    /// <summary>Do as many replacements as possible?</summary>
    [JsonProperty]
    private readonly bool _multiple;

    /// <summary>Match on name and type? If false, will only match when names match.</summary>
    [JsonProperty]
    private readonly MatchType _matchType;

    /// <summary>Name given to model after replacement</summary>
    [JsonProperty]
    private readonly string _newName;

    /// <summary>
    /// Specifies how models are matched for replacement: by name, by both name and type, or by either name or type.
    /// </summary>
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
        _modelReference = modelReference;
        _replacementPath = replacementPath;
        _multiple = multiple;
        _matchType = matchType;
        _newName = newName;
    }

    /// <summary>
    /// Run the command.
    /// </summary>
    /// <param name="relativeTo">The model the commands are relative to.</param>
    /// <param name="runner">An instance of an APSIM runner.</param>
    INodeModel IModelCommand.Run(INodeModel relativeTo, IRunner runner, List<Node> externalFileCache)
    {

        INodeModel modelToAdd = null;
        if (_modelReference is ModelInFileReference fileReference)
        {
            if (externalFileCache != null)
                foreach(Node node in externalFileCache)
                    if (node.FileName == fileReference.GetFilePath())
                        modelToAdd = fileReference.GetModelUsingCache(node);

            //if file wasn't in cache, try loading now
            if (modelToAdd == null)
                 modelToAdd = _modelReference.GetModel();
        }
        else
        {
            modelToAdd = _modelReference.GetModel();
        }

        IEnumerable<INodeModel> modelsToReplace;
        if (_replacementPath.Contains('.'))
        {
            var modelToReplace = (INodeModel)relativeTo.Node.Get(_replacementPath)
                 ?? throw new Exception($"Cannot find model: {_replacementPath}");
            if (_matchType == MatchType.NameAndType && !modelToReplace.GetType().IsAssignableFrom(modelToAdd.GetType()))
                throw new Exception($"Model {_replacementPath} is not of type {modelToAdd.GetType().Name}");
            modelsToReplace = [modelToReplace];
        }
        else
        {
            var replacementPathWithoutBrackets = _replacementPath.Replace("[", string.Empty)
                                                                .Replace("]", string.Empty);
            modelsToReplace = relativeTo.Node.FindAll(name: replacementPathWithoutBrackets);
            if (_matchType == MatchType.NameAndType)
            {
                modelsToReplace = modelsToReplace.Where(model => model.GetType().IsAssignableFrom(modelToAdd.GetType()));
            }
            else if (_matchType == MatchType.NameOrType && !modelsToReplace.Any())
            {
                // didn't find any matches using name so try by type.
                Type t = ModelRegistry.ModelNameToType(replacementPathWithoutBrackets);
                if (t != null)
                    modelsToReplace = relativeTo.Node.FindAll(type: t);
            }
        }

        if (!_multiple)
            modelsToReplace = modelsToReplace.Take(1);

        // Do model replacement.
        foreach (var modelToReplace in modelsToReplace.ToArray())  // Need the ToArray because modelsToReplace changes because of the ReplaceChild call.
        {
            var newModel = ReflectionUtilities.Clone(modelToAdd) as INodeModel ?? throw new Exception("Cloning the model failed or did not return an INodeModel instance.");
            if (string.IsNullOrEmpty(_newName))
                newModel.Rename(modelToReplace.Name);
            else
                newModel.Rename(_newName);
            CopyEnabledStateRecursively(modelToReplace, newModel);
            modelToReplace.Node.Parent.ReplaceChild(modelToReplace, newModel);
        }

        return relativeTo;
    }

    /// <summary>
    /// Copy enabled state from an original model tree into a replacement model tree.
    /// </summary>
    /// <remarks>Traversal is index-based and only recurses while both models have children.</remarks>
    private static void CopyEnabledStateRecursively(INodeModel originalModel, INodeModel replacementModel)
    {
        if (originalModel is null || replacementModel is null)
            return;

        var originalChildren = originalModel.GetChildren()?.ToArray();
        var replacementChildren = replacementModel.GetChildren()?.ToArray();

        replacementModel.Enabled = originalModel.Enabled;

        if (originalChildren is null || replacementChildren is null || originalChildren.Length == 0 || replacementChildren.Length == 0)
            return;

        foreach(var replacementChild in replacementChildren)
        {
            // Finds the first child where the name is the same.
            // Replacements can differ on type.
            var originalChild = originalChildren.FirstOrDefault(c => c.Name == replacementChild.Name);
            if (originalChild != null)
                CopyEnabledStateRecursively(originalChild, replacementChild);
        }
    }

    /// <summary>
    /// Return a hash code - useful for unit testing.
    /// </summary>
    public override int GetHashCode()
    {
        return (_modelReference.GetHashCode(), _replacementPath, _multiple, _matchType, _newName).GetHashCode();
    }

    /// <summary>
    /// Returns the potential filepath for this command.
    /// </summary>
    public string GetFilePath()
    {
        if (_modelReference is ModelInFileReference fileReference)
            return fileReference.GetFilePath();
        else
            return null;
    }
}