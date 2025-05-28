using APSIM.Shared.Utilities;

namespace APSIM.Core;

/// <summary>
/// NodeTree is responsible to maintaining the model hierarchy that is central to all APSIM simulations
/// and the user interface. A NodeTree instance has static classes to create a NodeTree instance from
/// a .apsimx file, a collection of models or from a string (e.g. from a resource). A NodeTree instance
/// has a _Root_ node.
/// </summary>
public class NodeTree
{
    /// <summary>Dictionary that maps object instances to ModelNodes. This is done for quick access to a ModelNode, given an object (e.g. IModel or POCO class) instance.</summary>
    private Dictionary<INodeModel, Node> nodeMap = [];

    /// <summary>The POCO discovery function delegate</summary>
    public delegate (string name, IEnumerable<INodeModel> children) DiscoveryFuncDelegate(object obj);

    internal NodeTree() { }



    /// <summary>Name of the .apsimx file the node tree came from.</summary>
    public string FileName { get; internal set; }

    /// <summary>Root node for tree hierarchy.</summary>
    public Node Head { get; private set; }

    /// <summary>WasRoot node for tree hierarchy.</summary>
    public bool DidConvert { get; private set; }

    /// <summary>All nodes in tree. Order is not guarenteed.</summary>
    public IEnumerable<Node> Nodes => nodeMap.Values;

    /// <summary>All nodes in tree. Order is not guarenteed.</summary>
    public IEnumerable<INodeModel> Models => nodeMap.Keys;

    /// <summary>Is initialisation underway?</summary>
    public bool IsInitialising { get; private set; }

    /// <summary>
    /// Get the node instance for a given model instance
    /// </summary>
    /// <param name="model">The model instance to retrieve the node for or null for root node.</param>
    /// <returns>The Node or throws if not found.</returns>
    public Node GetNode(INodeModel model = null)
    {
        if (model == null)
            return Head;
        else if (nodeMap.TryGetValue(model, out var details))
            return details;
        else
            throw new Exception($"Cannot find details for object");  // shouldn't happen.
    }

    /// <summary>
    /// Add a model/node to nodemap.
    /// </summary>
    /// <param name="node">The node to add.</param>
    internal void AddToNodeMap(Node node)
    {
        nodeMap.Add(node.Model, node);
    }

    /// <summary>
    /// Remove a model/node from nodemap.
    /// </summary>
    /// <param name="model">The model to remove.
    internal void RemoveFromNodeMap(INodeModel model)
    {
        nodeMap.Remove(model);
    }



}