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

    /// <summary>
    /// Create a NodeTree instance from a model instance.
    /// </summary>
    /// <param name="model">Model instance</param>
    /// <param name="errorHandler">A callback function that is invoked when an exception is thrown.</param>
    /// <param name="initInBackground">Initialise the node on a background thread?</param>
    /// <param name="didConvert">Was the .apsimx file converted to the latest format?</param>
    /// <param name="fileName">The name of the .apsimx file</param>
    public static Node Create(INodeModel model, Action<Exception> errorHandler = null, bool didConvert = false, bool initInBackground = false, string fileName = null)
    {
        NodeTree tree = new();
        tree.FileName = fileName;
        tree.ConstructNodeTree(model, errorHandler, didConvert, initInBackground);
        return tree.Head;
    }

    /// <summary>Return the current version of JSON used in .apsimx files.</summary>
    public static int JSONVersion => Converter.LatestVersion;

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

    /// <summary>Compiler instance.</summary>
    public ScriptCompiler Compiler { get; internal set; }

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

    /// <summary>
    /// Build the parent / child map.
    /// </summary>
    /// <param name="root">The root node.</param>
    /// <param name="errorHandler">The handler to call when exceptions are thrown</param>
    /// <param name="didConvert">Was the json converted to the newest version when creating the tree?</param>
    /// <param name="initInBackground">Initialise on a background thread?</param>
    internal void ConstructNodeTree(INodeModel model, Action<Exception> errorHandler, bool didConvert, bool initInBackground, bool doInitialise = true)
    {
        try
        {
            IsInitialising = true;
            Head = new(this, model, null);
            AddToNodeMap(Head);
            foreach (var childModel in model.GetChildren())
                Head.AddChild(childModel);
            DidConvert = didConvert;
            if (Compiler == null) // Will be not null for cloned trees.
                Compiler = new();

            if (doInitialise)
                InitialiseModel(this,
                                initInBackground: initInBackground,
                                errorHandler: errorHandler);
        }
        finally
        {
            IsInitialising = false;
        }
    }

    /// <summary>
    /// Initialise the simulation.
    /// </summary>
    private void InitialiseModel(NodeTree tree, bool initInBackground, Action<Exception> errorHandler)
    {
        try
        {
            IsInitialising = true;

            // Replace all models that have a ResourceName with the official, released models from resources.
            Resource.Instance.Replace(tree);

            // Call created in all models.
            if (initInBackground)
                Task.Run(() => tree.Head.InitialiseModel(errorHandler));
            else
                tree.Head.InitialiseModel(errorHandler);
        }
        finally
        {
            IsInitialising = false;
        }
    }


}