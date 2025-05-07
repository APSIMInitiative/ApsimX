namespace APSIM.Core;

/// <summary>
/// Constructs and maintains a tree (parent/child) of ModelNode instances for all models in a .apsimx file.
/// </summary>
public class NodeTree
{
    private readonly ModelDiscovery modelDiscovery = new();

    /// <summary>Dictionary that maps object instances to ModelNodes. This is done for quick access to a ModelNode, given an object (e.g. IModel or POCO class) instance.</summary>
    private Dictionary<object, Node> nodeMap = [];

    /// <summary>The POCO discovery function delegate</summary>
    public delegate (string name, IEnumerable<object> children) DiscoveryFuncDelegate(object obj);

    /// <summary>
    /// Build the parent / child map.
    /// </summary>
    /// <param name="root">The root node.</param>
    /// <param name="didConvert">Was the json converted to the newest version when creating the tree?</param>
    public void Initialise(object root, bool didConvert)
    {
        Root = AddChildNode(root, null);
        DidConvert = didConvert;
    }

    /// <summary>Root node for tree hierarchy.</summary>
    public Node Root { get; private set; }

    /// <summary>WasRoot node for tree hierarchy.</summary>
    public bool DidConvert { get; internal set; }

    /// <summary>All nodes in tree. Order is not guarenteed.</summary>
    public IEnumerable<Node> Nodes => nodeMap.Values;

    /// <summary>All nodes in tree. Order is not guarenteed.</summary>
    public IEnumerable<object> Models => nodeMap.Keys;

    /// <summary>Walk models in tree (ordered depth first).</summary>
    public IEnumerable<object> WalkModels => WalkNodes(Root).Select(node => node.Model);

    /// <summary>
    /// Register a discovery function for a POCO object.
    /// </summary>
    /// <param name="t">The POCO type</param>
    /// <param name="f">The function that can return name and children of the POCO type</param>
    public void RegisterDiscoveryFunction(Type t, DiscoveryFuncDelegate f)
    {
        modelDiscovery.RegisterType(t, f);
    }

    /// <summary>
    /// Get a ModelNode for a given model instance
    /// </summary>
    /// <param name="instance">The model instance to retrieve the node for or null for root node.</param>
    /// <returns>The ModelNode or throws if not found.</returns>
    public Node GetNode(object instance = null)
    {
        if (instance == null)
            return Root;
        else if (nodeMap.TryGetValue(instance, out var details))
            return details;
        else
            throw new Exception($"Cannot find details for object");  // shouldn't happen.
    }

    /// <summary>
    /// Rescan a node. This is necessary when an IModel instance has changed it's children
    /// (e.g. when a model is replaced from a resource).
    /// </summary>
    /// <param name="node">The node to rescan.</param>
    public void Rescan(Node node)
    {
        var (_, children) = modelDiscovery.GetNameAndChildrenOfObj(node.Model);

        // remove existing child nodes from the nodes map so that we can add new ones.
        ClearChildNodes(node);
        AddChildNodes(node, children);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="node"></param>
    private void ClearChildNodes(Node node)
    {
        foreach (var childNode in node.Walk().Skip(1))
            nodeMap.Remove(childNode.Model);
        node.ClearChildren();
    }

    /// <summary>
    /// Add a ModelNode to the nodeMap for the specified object. NOTE: This is recursive.
    /// </summary>
    /// <param name="modelInstance">The model instance to create a Node for.</param>
    /// <param name="parent">The parent ModelNode.</param>
    /// <returns>The newly created ModelNode</returns>
    private Node AddChildNode(object modelInstance, Node parent)
    {
        var (name, childModels) = modelDiscovery.GetNameAndChildrenOfObj(modelInstance);

        Node node = new(name, $"{parent?.FullNameAndPath}.{name}", modelInstance, parent);
        nodeMap.Add(modelInstance, node);
        AddChildNodes(node, childModels);
        return node;
    }

    /// <summary>
    /// Add child model instances to a node.
    /// </summary>
    /// <param name="node">The node to add child nodes to.</param>
    /// <param name="children">The child nodes.</param>
    /// <returns></returns>
    private List<Node> AddChildNodes(Node node, IEnumerable<object> children)
    {
        List<Node> childNodes = new();
        if (children != null)
        {
            foreach (var child in children)
            {
                if (child is IPOCOModel poco)
                    childNodes.Add(AddChildNode(poco.Obj, node));
                else
                    childNodes.Add(AddChildNode(child, node));
            }
        }
        return childNodes;
    }

    /// <summary>
    /// Walk nodes (depth first), returing each node. Uses recursion.
    /// </summary>
    /// <param name="node">The node to start walking</param>
    private IEnumerable<Node> WalkNodes(Node node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var childNode in WalkNodes(child))
                yield return childNode;
    }
}