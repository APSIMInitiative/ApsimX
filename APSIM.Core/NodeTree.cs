using System.Linq.Expressions;

namespace APSIM.Core;

/// <summary>
/// Constructs and maintains a tree (parent/child) of ModelNode instances for all models in a .apsimx file.
/// </summary>
public class NodeTree
{
    /// <summary>Dictionary that maps object instances to ModelNodes. This is done for quick access to a ModelNode, given an object (e.g. IModel or POCO class) instance.</summary>
    private Dictionary<INodeModel, Node> nodeMap = [];

    /// <summary>The POCO discovery function delegate</summary>
    public delegate (string name, IEnumerable<INodeModel> children) DiscoveryFuncDelegate(object obj);

    internal NodeTree() { }

    /// <summary>
    ///
    /// </summary>[]
    /// <param name="fileName"></param>
    /// <param name="errorHandler"></param>
    /// <param name="initInBackground"></param>
    public static NodeTree CreateFromFile<T>(string fileName, Action<Exception> errorHandler, bool initInBackground)
    {
        NodeTree tree = FileFormat.ReadFromFile1<T>(fileName, errorHandler, initInBackground);
        return tree;
    }

    /// <summary>
    ///
    /// </summary>[]
    /// <param name="st"></param>
    /// <param name="errorHandler">The handler to call when exceptions are thrown</param>
    /// <param name="initInBackground">Initialise on a background thread?</param>
    public static NodeTree CreateFromString<T>(string st, Action<Exception> errorHandler, bool initInBackground)
    {
        return FileFormat.ReadFromString1<T>(st, errorHandler, initInBackground: initInBackground);
    }

    /// <summary>
    /// Create a tree from a simulations instance.
    /// </summary>
    /// <param name="simulations">Simulations instance</param>
    /// <param name="errorHandler">The handler to call when exceptions are thrown</param>
    /// <param name="initInBackground">Initialise on a background thread?</param>
    /// <param name="didConvert">Was the .apsimx file converted?</param>
    /// <param name="fileName">The name of the .apsimx file</param>
    public static NodeTree Create(INodeModel simulations, Action<Exception> errorHandler = null, bool didConvert = false, bool initInBackground = false, string fileName = null)
    {
        NodeTree tree = new();
        tree.FileName = fileName;
        tree.ConstructNodeTree(simulations, errorHandler, didConvert, initInBackground);

        return tree;
    }

    /// <summary>
    /// Name of the .apsimx file the node tree came from.
    /// </summary>
    public string FileName { get; internal set; }

    /// <summary>Root node for tree hierarchy.</summary>
    public Node Root { get; private set; }

    /// <summary>WasRoot node for tree hierarchy.</summary>
    public bool DidConvert { get; internal set; }

    /// <summary>All nodes in tree. Order is not guarenteed.</summary>
    public IEnumerable<Node> Nodes => nodeMap.Values;

    /// <summary>All nodes in tree. Order is not guarenteed.</summary>
    public IEnumerable<INodeModel> Models => nodeMap.Keys;

    /// <summary>Walk models in tree (ordered depth first).</summary>
    public IEnumerable<INodeModel> WalkModels => WalkNodes(Root).Select(node => node.Model);

    /// <summary>Compiler instance.</summary>
    public ScriptCompiler Compiler => ScriptCompiler.Instance;

    /// <summary>Is initialisation underway?</summary>
    public bool IsInitialising { get; private set; }

    /// <summary>
    /// Walk nodes (depth first), returing each node. Uses recursion.
    /// </summary>
    /// <param name="node">The node to start walking</param>
    public IEnumerable<Node> WalkNodes(Node node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var childNode in WalkNodes(child))
                yield return childNode;
    }

    /// <summary>
    /// Get a ModelNode for a given model instance
    /// </summary>
    /// <param name="instance">The model instance to retrieve the node for or null for root node.</param>
    /// <returns>The ModelNode or throws if not found.</returns>
    public Node GetNode(INodeModel instance = null)
    {
        if (instance == null)
            return Root;
        else if (nodeMap.TryGetValue(instance, out var details))
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
    private void ConstructNodeTree(INodeModel model, Action<Exception> errorHandler, bool didConvert, bool initInBackground)
    {
        try
        {
            IsInitialising = true;
            Root = new(this, model, null);
            AddToNodeMap(Root);
            foreach (var childModel in model.GetChildren())
                Root.AddChild(childModel);
            DidConvert = didConvert;

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

            // Give services to all models that need it.
            foreach (var n in Root.Walk().Where(n => n.Model is IServices))
                (n.Model as IServices).SetServices(tree);

            // Call created in all models.
            if (initInBackground)
                Task.Run(() => tree.Root.InitialiseModel(errorHandler));
            else
                tree.Root.InitialiseModel(errorHandler);
        }
        finally
        {
            IsInitialising = false;
        }
    }


}