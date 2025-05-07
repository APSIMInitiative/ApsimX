namespace APSIM.Core;

/// <summary>
/// Encapsulates a model node having a a parent/child relationship with other ModelNodes.
/// </summary>
public class Node
{
    private readonly List<Node> children = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="fullNameAndPath"></param>
    /// <param name="model"></param>
    /// <param name="parent"></param>
    internal Node(string name, string fullNameAndPath, object model, Node parent)
    {
        Name = name;
        FullNameAndPath = fullNameAndPath;
        Model = model;
        parent?.AddChild(this);
    }

    /// <summary>The name of the model.</summary>
    public string Name { get; }

    /// <summary>The full path and name of the model.</summary>
    public string FullNameAndPath { get; }

    /// <summary>The parent ModelNode.</summary>
    public Node Parent { get; private set;}

    /// <summary>The model instance. Can be INodeModel or POCO.</summary>
    public object Model { get; }

    /// <summary>The child ModelNode instances.</summary>
    public IEnumerable<Node> Children => children;

    /// <summary>
    /// Add child node
    /// </summary>
    /// <param name="child">The child node to add.</param>
    internal void AddChild(Node child)
    {
        child.Parent = this;
        if (child.Model is INodeModel nodeModel)
            nodeModel.SetParent(Model);
        children.Add(child);
    }

    /// <summary>
    /// Clear child nodes
    /// </summary>
    internal void ClearChildren()
    {
        children.Clear();
    }

    /// <summary>
    /// Walk nodes (depth first), returing each node. Uses recursion.
    /// </summary>
    public IEnumerable<Node> Walk()
    {
        yield return this;
        foreach (var child in Children)
            foreach (var childNode in child.Walk())
                yield return childNode;
    }

}