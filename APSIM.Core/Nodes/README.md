# NodeTree

_NodeTree_ is responsible to maintaining the model hierarchy that is central to all APSIM simulations and the user interface. A _NodeTree_ instance has static classes to create a _NodeTree_ instance from a .apsimx file, a collection of models or from a string (e.g. from a resource). A _NodeTree_ instance has a _Root_ node.

```c#
/// <summary>
/// Create a NodeTree instance from a .apsimx file.
/// </summary>[]
/// <param name="fileName">The name of a file.</param>
/// <param name="errorHandler">A callback function that is invoked when an exception is thrown.</param>
/// <param name="initInBackground">Initialise the node on a background thread?</param>
public static NodeTree CreateFromFile<T>(string fileName, Action<Exception> errorHandler, bool initInBackground);

/// <summary>
/// Create a NodeTree instance from a JSON string.
/// </summary>[]
/// <param name="st">The JSON string</param>
/// <param name="errorHandler">A callback function that is invoked when an exception is thrown.</param>
/// <param name="initInBackground">Initialise the node on a background thread?</param>
public static NodeTree CreateFromString<T>(string st, Action<Exception> errorHandler, bool initInBackground);

/// <summary>
/// Create a NodeTree instance from a model instance.
/// </summary>
/// <param name="model">Model instance</param>
/// <param name="errorHandler">A callback function that is invoked when an exception is thrown.</param>
/// <param name="initInBackground">Initialise the node on a background thread?</param>
/// <param name="didConvert">Was the .apsimx file converted to the latest format?</param>
/// <param name="fileName">The name of the .apsimx file</param>
public static NodeTree Create(INodeModel model, Action<Exception> errorHandler = null, bool didConvert = false, bool initInBackground = false, string fileName = null);

/// <summary>Return the current version of JSON used in .apsimx files.</summary>
public static int JSONVersion;

/// <summary>Name of the .apsimx file the node tree came from.</summary>
public string FileName { get; }

/// <summary>Root node for tree hierarchy.</summary>
public Node Root { get; }

/// <summary>WasRoot node for tree hierarchy.</summary>
public bool DidConvert { get; }

/// <summary>All nodes in tree. Order is not guarenteed.</summary>
public IEnumerable<Node> Nodes;

/// <summary>All nodes in tree. Order is not guarenteed.</summary>
public IEnumerable<INodeModel> Models;

/// <summary>Walk models in tree (ordered depth first).</summary>
public IEnumerable<INodeModel> WalkModels;

/// <summary>Compiler instance.</summary>
public ScriptCompiler Compiler { get; }

/// <summary>Resource instance.</summary>
public static Resource Resources { get; }

/// <summary>Is initialisation underway?</summary>
public bool IsInitialising { get; }

/// <summary>Convert tree to JSON string (in APSIM format).</summary>
public string ToJSONString();

/// <summary>
/// Get the node instance for a given model instance
/// </summary>
/// <param name="model">The model instance to retrieve the node for or null for root node.</param>
/// <returns>The Node or throws if not found.</returns>
public Node GetNode(INodeModel model = null);
```

# Node

A _Node_ instance encapsulates the concept of a model in a tree of models. The nodes are represeted in the user interface tree view. Each _Node_ has a link to a model (currently a INodeModel but will be a POCO object), a _Children_ property that contains the child nodes and a _Parent_ property that links to the parent node. There are also methods for adding, removing and replace child nodes and for walking the node tree.

```c#
/// <summary>The node name.</summary>
public string Name { get; }

/// <summary>The full path and name.</summary>
public string FullNameAndPath { get; }

/// <summary>The owning tree.</summary>
public NodeTree Tree { get; }

/// <summary>The parent Node.</summary>
public Node Parent { get; }

/// <summary>The model instance.</summary>
public INodeModel Model { get; }

/// <summary>The collection of child Node instances.</summary>
public IEnumerable<Node> Children;

/// <summary>Convert node to JSON string.</summary>
public string ToJSONString();

/// <summary>Rename the node.</summary>
/// <param name="name">The new name for the node.</param>
public void Rename(string name);

/// <summary>Clone a node and its child nodes.</summary>
public static Node Clone();

/// <summary>Walk nodes (depth first), returing each node. Uses recursion.</summary>
public IEnumerable<Node> Walk();

/// <summary>Walk parent nodes, returing each node. Uses recursion.</summary>
public IEnumerable<Node> WalkParents();

/// <summary>Add child model.</summary>
/// <param name="childModel">The child model to add.</param>
public Node AddChild(INodeModel childModel);

/// <summary>Add child model but don't initialise it.</summary>
/// <param name="childModel">The child node to add.</param>
public Node AddChildDontInitialise(INodeModel childModel);

/// <summary>Remove a child model.</summary>
/// <param name="childModel">The child model to remove.</param>
public void RemoveChild(INodeModel childModel);

/// <summary>Replace a child model with another child.</summary>
/// <param name="oldModel">The child model to remove.</param>
/// <param name="newModel">The new child model to insert into same position.</param>
public void ReplaceChild(INodeModel oldModel, INodeModel newModel);

/// <summary>Insert a child node</summary>
/// <param name="index">The position of the child in the children list.</param>
/// <param name="childModels">The child model to add.</param>
public Node InsertChild(int index, INodeModel childModel);

/// <summary>Clear all child nodes.</summary>
public void Clear()
```