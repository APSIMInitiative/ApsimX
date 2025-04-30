using System.Collections.Generic;

namespace Models.Core;

/// <summary>
/// Encapsulates a model node having a a parent/child relationship with other ModelNodes.
/// </summary>
public class ModelNode
{
    /// <summary>The name of the model.</summary>
    internal string Name;

    /// <summary>The full path and name of the model.</summary>
    internal string FullNameAndPath;

    /// <summary>The parent ModelNode.</summary>
    internal ModelNode Parent;

    /// <summary>The model instance.</summary>
    internal object Instance;

    /// <summary>The child ModelNode instances.</summary>
    internal List<ModelNode> Children;
}