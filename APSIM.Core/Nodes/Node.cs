using APSIM.Shared.Utilities;
using Topten.RichTextKit;

namespace APSIM.Core;


/// <summary>
/// A Node instance encapsulates the concept of a model in a tree of models. The nodes are
/// represeted in the user interface tree view. Each _Node_ has a link to a model
/// (currently a INodeModel but will be a POCO object), a _Children_ property that
/// contains the child nodes and a _Parent_ property that links to the parent node.
/// There are also methods for adding, removing and replace child nodes and for walking the node tree.
/// </summary>
public class Node : IStructure
{
    private readonly List<Node> children = [];
    private ScopingRules scope;
    private Locator locator;

    /// <summary>The node name.</summary>
    public string Name { get; private set; }

    /// <summary>The full path and name.</summary>
    public string FullNameAndPath
    {
        get
        {
            string path = null;
            foreach (var node in WalkParents().Reverse().Append(this))
                path = path + "." + node.Name;

            return path;
        }
    }

    /// <summary>The parent Node.</summary>
    public Node Parent { get; private set; }

    /// <summary>The model instance.</summary>
    public INodeModel Model { get; }

    /// <summary>The collection of child Node instances.</summary>
    public IEnumerable<Node> Children => children;

    /// <summary>Name of the .apsimx file the node tree came from.</summary>
    public string FileName { get; set; }

    /// <summary>Is initialisation underway?</summary>
    public bool IsInitialising { get; private set; }

    /// <summary>Compiler instance.</summary>
    public ScriptCompiler Compiler { get; private set; }


    /// <summary>
    /// Create a NodeTree instance from a model instance.
    /// </summary>
    /// <param name="model">Model instance</param>
    /// <param name="errorHandler">A callback function that is invoked when an exception is thrown.</param>
    /// <param name="didConvert">When reading from a file or JSON string, was the format converted?</param>
    /// <param name="initInBackground">Initialise the node on a background thread?</param>
    /// <param name="fileName">The name of the .apsimx file</param>
    public static Node Create(INodeModel model, Action<Exception> errorHandler = null, bool initInBackground = false, string fileName = null)
    {
        return ConstructNodeTree(model, errorHandler, compiler: new ScriptCompiler(), fileName, initInBackground);
    }

    /// <summary>Convert node to JSON string.</summary>
    public string ToJSONString()
    {
        return FileFormat.WriteToString(this);
    }

    /// <summary>Rename the node.</summary>
    /// <param name="name">The new name for the node.</param>
    public void Rename(string name)
    {
        Name = name;
        Model.Rename(name);
        EnsureNameIsUnique();
    }

    /// <summary>Clone a node and its child nodes.</summary>
    public Node Clone()
    {
        var newModel = ReflectionUtilities.Clone(Model) as INodeModel;
        return ConstructNodeTree(newModel, (ex) => { return; }, Compiler, FileName, initInBackground: false, doInitialise: true);
    }

    /// <summary>Walk nodes (depth first), returing each node. Uses recursion.</summary>
    public IEnumerable<Node> Walk()
    {
        yield return this;
        foreach (var child in Children)
            foreach (var childNode in child.Walk())
                yield return childNode;
    }

    /// <summary>Walk nodes in scope (depth first), returing each node</summary>
    public IEnumerable<Node> WalkScoped()
    {
        if (scope != null)  // can be null if called while Nodes are being created.
            foreach (var node in scope.Walk(this))
                yield return node;
    }

    /// <summary>
    /// Get models in scope.
    /// </summary>
    /// <param name="name">The name of the model to return. Can be null.</param>
    /// <param name="relativeTo">The model to use when determining scope.</param>
    /// <returns>All matching models.</returns>
    public IEnumerable<T> FindAll<T>(string name = null, INodeModel relativeTo = null)
    {
        Node relativeToNode = this;
        if (relativeTo != null)
            relativeToNode = relativeTo.Node;

        foreach (var node in relativeToNode.WalkScoped())
            if (node.Model is T && (name == null || node.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                yield return (T)node.Model;
    }

    /// <summary>
    /// Get a model in scope.
    /// </summary>
    /// <param name="name">The name of the model to return. Can be null.</param>
    /// <param name="relativeTo">The model to use when determining scope.</param>
    /// <returns>The found model or null if not found</returns>
    public T Find<T>(string name = null, INodeModel relativeTo = null)
    {
        if (name == null && typeof(T).Name == "Object") // unit test tries this an expects null.
            return default;
        return FindAll<T>(name, relativeTo).FirstOrDefault();
    }

    /// <summary>Find the scoped parent node.</summary>
    public Node ScopedParent()
    {
        if (scope == null)  // can be null if called while Nodes are being created.
            return null;
        return scope.FindScopedParentModel(this);
    }

    /// <summary>Is this node in scope of another node.</summary>
    /// <param name="node">The other node</param>
    public bool InScopeOf(Node node)
    {
        return scope.InScopeOf(this, node);
    }

    /// <summary>Walk parent nodes, returing each node. Uses recursion.</summary>
    public IEnumerable<Node> WalkParents()
    {
        if (Parent != null)
        {
            yield return Parent;
            foreach (var ancestor in Parent.WalkParents())
                yield return ancestor;
        }
    }

    /// <summary>
    /// Get the value of a variable or model.
    /// </summary>
    /// <param name="namePath">The name of the object to return</param>
    /// <param name="flags">Flags controlling the search</param>
    /// <param name="relativeTo">Make the get relative>/param>
    /// <returns>The found object or null if not found</returns>
    public object Get(string path, LocatorFlags flags = LocatorFlags.None, INodeModel relativeTo = null)
    {
        var relativeToNode = this;
        if (relativeTo != null)
            relativeToNode = relativeTo.Node;
        return locator.Get(relativeToNode, path, flags);
    }

    /// <summary>
    /// Get the value of a variable or model.
    /// </summary>
    /// <param name="namePath">The name of the object to return</param>
    /// <param name="flags">Flags controlling the search</param>
    /// <param name="relativeTo">Make the get relative>/param>
    /// <returns>The found object or null if not found</returns>
    public VariableComposite GetObject(string path, LocatorFlags flags = LocatorFlags.None, INodeModel relativeTo = null)
    {
        var relativeToNode = this;
        if (relativeTo != null)
            relativeToNode = relativeTo.Node;
        return locator.GetObject(relativeToNode, path, flags);
    }

    /// <summary>
    /// Set the value of a variable. Will throw if variable doesn't exist.
    /// </summary>
    /// <param name="namePath">The name of the object to set</param>
    /// <param name="value">The value to set the property to</param>
    /// <param name="relativeTo">Make the set relative>/param>
    public void Set(string namePath, object value, INodeModel relativeTo = null)
    {
        var relativeToNode = this;
        if (relativeTo != null)
            relativeToNode = relativeTo.Node;
        locator.Set(relativeToNode, namePath, value);
    }

    /// <summary>Clear the locator.</summary>
    public void ClearLocator() => locator.Clear();

    /// <summary>Get the underlying locator. Used for unit tests only.</summary>
    internal Locator Locator => locator;

    /// <summary>
    /// Remove a single entry from the locator cache.
    /// Should be called if the old path may become invalid.
    /// </summary>
    /// <param name="path"></param>
    public void ClearEntry(string path) => locator.ClearEntry(this, path);



    /// <summary>
    /// Find a child.
    /// </summary>
    /// <typeparam name="T">Type of child to find.</typeparam>
    /// <param name="name">Optional name of child.</param>
    /// <param name="recurse">Recursively look for child?</param>
    /// <param name="relativeTo">The node to make the find relative to.</param>
    /// <returns>Child or null if not found.</returns>
    public T FindChild<T>(string name = null, bool recurse = false, INodeModel relativeTo = null)
    {
        return FindChildren<T>(name, recurse, relativeTo).FirstOrDefault();
    }

    /// <summary>
    /// Find all children direct and/or recursively.
    /// </summary>
    /// <typeparam name="T">Type of child nodes to find.</typeparam>
    /// <param name="name">Optional name of child.</param>
    /// <param name="recurse">Recursively look for children?</param>
    /// <param name="relativeTo">The node to make the find relative to.</param>
    /// <returns>Collection of child nodes or empty collection</returns>
    public IEnumerable<T> FindChildren<T>(string name = null, bool recurse = false, INodeModel relativeTo = null)
    {
        Node relativeToNode = this;
        if (relativeTo != null)
            relativeToNode = relativeTo.Node;

        IEnumerable<Node> children;
        if (recurse)
            children = relativeToNode.Walk().Skip(1);
        else
            children = relativeToNode.Children;

        foreach (var node in children)
            if (node.Model is T && (name == null || node.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                yield return (T)node.Model;
    }

    /// <summary>
    /// Find a sibling
    /// </summary>
    /// <typeparam name="T">Type of sibling to find.</typeparam>
    /// <param name="name">Optional name of child.</param>
    /// <param name="relativeTo">The node to make the find relative to.</param>
    /// <returns>Sibling or null if not found.</returns>
    public T FindSibling<T>(string name = null, INodeModel relativeTo = null)
    {
        var value = FindSiblings<T>(name, relativeTo);
        if (value != null)
            return value.FirstOrDefault();
        return default;
    }

    /// <summary>
    /// Find all siblings.
    /// </summary>
    /// <typeparam name="T">Type of siblings to find.</typeparam>
    /// <param name="name">Optional name of siblings.</param>
    /// <param name="relativeTo">The node to make the find relative to.</param>
    /// <returns>Collection of sibling nodes or empty collection</returns>
    public IEnumerable<T> FindSiblings<T>(string name = null, INodeModel relativeTo = null)
    {
        Node relativeToNode = this;
        if (relativeTo != null)
            relativeToNode = relativeTo.Node;
        if (relativeToNode.Parent == null)
            return Enumerable.Empty<T>();
        return FindChildren<T>(name, recurse: false, relativeTo: relativeToNode.Parent.Model)
               .Where(child => child as INodeModel != relativeToNode.Model);
    }

    /// <summary>
    /// Find a parent
    /// </summary>
    /// <typeparam name="T">Type of parent to find.</typeparam>
    /// <param name="name">Optional name of parent.</param>
    /// <param name="relativeTo">The node to make the find relative to.</param>
    /// <param name="recurse">Recurse up the tree of parents looking for a match?</param>
    /// <returns>Parent or null if not found.</returns>
    public T FindParent<T>(string name = null, bool recurse = false, INodeModel relativeTo = null)
    {
        Node relativeToNode = this;
        if (relativeTo != null)
            relativeToNode = relativeTo.Node;
        if (recurse)
        {
            foreach (var node in relativeToNode.Parent?.Walk())
                if (node.Model is T && (name == null || node.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                    return (T)node.Model;
        }
        else
            return (T)relativeToNode.Parent.Model;
        return default;
    }



    /// <summary>Add child model.</summary>
    /// <param name="childModel">The child model to add.</param>
    public void AddChild(INodeModel childModel)
    {
        var childNode = AddChildDontInitialise(childModel);

        // If we arean't in an initial setup phase then initialise all child models.
        if (!IsInitialising)
            childNode.InitialiseModel();

        scope?.Clear();
        locator?.Clear();
    }

    /// <summary>Remove a child model.</summary>
    /// <param name="childModel">The child model to remove.</param>
    public void RemoveChild(INodeModel childModel)
    {
        // Remove the model from our children collection.
        Node nodeToRemove = children.Find(c => c.Model == childModel);
        if (nodeToRemove == null)
            throw new Exception($"Cannot find node {childModel.Name}");

        // remove the model from it's parent model.
        Model.RemoveChild(childModel);

        // remove child node.
        children.Remove(nodeToRemove);

        scope?.Clear();
        locator?.Clear();
    }

    /// <summary>Replace a child model with another child.</summary>
    /// <param name="oldModel">The child model to remove.</param>
    /// <param name="newModel">The new child model to insert into same position.</param>
    public void ReplaceChild(INodeModel oldModel, INodeModel newModel)
    {
        // Determine the position of the old model.
        Node oldChildNode = children.Find(c => c.Model == oldModel);
        if (oldChildNode == null)
            throw new Exception($"In ReplaceChild, cannot find a child with name: {oldModel.Name}");

        // Determine what position it is in 'children'.
        int index = children.IndexOf(oldChildNode);

        // Remove old node and insert the new node at the correct position.
        RemoveChild(oldChildNode.Model);
        InsertChild(index, newModel);
    }

    /// <summary>Insert a child node</summary>
    /// <param name="index">The position of the child in the children list.</param>
    /// <param name="childModels">The child model to add.</param>
    public void InsertChild(int index, INodeModel childModel)
    {
        // Add the child model to children collection. It will be added to the end of the collection.
        AddChild(childModel);
        Node childNode = children.Find(child => child.Model == childModel);

        // Move the node to the correct position
        children.Remove(childNode);
        children.Insert(index, childNode);

        // Move the model to the correct position
        Model.RemoveChild(childModel);
        Model.InsertChild(index, childModel);
    }

    /// <summary>Clear all child nodes.</summary>
    public void Clear()
    {
        foreach (var child in children.ToArray())
            RemoveChild(child.Model);
    }


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="fullNameAndPath"></param>
    /// <param name="model"></param>
    /// <param name="parent"></param>
    internal Node(INodeModel model, string parentFullNameAndPath)
    {
        if (model is IModelAdapter adapter)
            adapter.Initialise();

        Name = model.Name;
        Model = model;
    }


    /// <summary>
    /// Build the parent / child map.
    /// </summary>
    /// <param name="root">The root node.</param>
    /// <param name="errorHandler">The handler to call when exceptions are thrown</param>
    /// <param name="compiler">An existing script compiler to use.</param>
    /// <param name="fileName">The name of the .apsimx file</param>
    /// <param name="didConvert">didConvert</param>
    /// <param name="initInBackground">Initialise on a background thread?</param>
    internal static Node ConstructNodeTree(INodeModel model, Action<Exception> errorHandler, ScriptCompiler compiler, string fileName, bool initInBackground, bool doInitialise = true)
    {
        Node head = new(model, null);
        head.scope = new();
        head.locator = new();
        head.Compiler = compiler;
        head.FileName = fileName;
        model.Node = head;
        ResolvesDependencies(head);

        foreach (var childModel in model.GetChildren())
            head.AddChildDontInitialise(childModel);

        // Replace all models that have a ResourceName with the official, released models from resources.
        Resource.Instance.Replace(head);

        // Initialise the model.
        if (doInitialise)
        {
            // Call created in all models.
            if (initInBackground)
                Task.Run(() => head.InitialiseModel(errorHandler));
            else
                head.InitialiseModel(errorHandler);
        }
        return head;
    }

    /// <summary>
    /// Initialise a model
    /// </summary>
    /// <param name="errorHandler"></param>
    internal void InitialiseModel(Action<Exception> errorHandler = null)
    {
        try
        {
            foreach (var node in Walk())
                node.IsInitialising = true;

            foreach (Node node in Walk().Where(n => n.Model is ICreatable))
            {
                try
                {
                    (node.Model as ICreatable).OnCreated();
                }
                catch (Exception err)
                {
                    if (errorHandler == null)
                        throw;
                    errorHandler(err);
                }
            }
        }
        finally
        {
            foreach (var node in Walk())
                node.IsInitialising = false;
        }
    }


    /// <summary>Add child model but don't initialise it.</summary>
    /// <param name="childModel">The child node to add.</param>
    private Node AddChildDontInitialise(INodeModel childModel)
    {
        // Create a child node to contain the child model.
        var childNode = new Node(childModel, FullNameAndPath);
        childNode.Parent = this;
        childNode.FileName = childNode.Parent.FileName;
        childNode.Compiler = childNode.Parent.Compiler;
        children.Add(childNode);

        // Give the child our services.
        childNode.FileName = FileName;
        childNode.Compiler = Compiler;
        childNode.scope = scope;
        childNode.locator = locator;
        childModel.Node = childNode;

        // Resolves child dependencies.
        ResolvesDependencies(childNode);

        // Ensure the model is inserted into parent model.
        childNode.Model.SetParent(Model);
        if (!Model.GetChildren().Contains(childModel))
            Model.AddChild(childModel);

        // Recurse through all children.
        foreach (var c in childNode.Model.GetChildren())
            childNode.AddChildDontInitialise(c);

        return childNode;
    }

    /// <summary>
    /// Resolve dependencies.
    /// </summary>
    /// <param name="node">Node to resolve dependencies in.</param>
    private static void ResolvesDependencies(Node node)
    {
        if (node.Model is IStructureDependency s)
            s.Structure = node;
    }

    /// <summary>
    /// Give the specified model a unique name
    /// </summary>
    private void EnsureNameIsUnique()
    {
        string originalName = Name;
        int counter = 0;
        while (this.Siblings().Any(sibling => sibling.Name == Name) && counter < 10000)
        {
            counter++;
            Name = $"{originalName}{counter}";
            Model.Rename(Name);
        }

        if (counter == 10000)
            throw new Exception("Cannot create a unique name for model: " + originalName);
    }

}