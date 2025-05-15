using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace APSIM.Core;

/// <summary>
/// Encapsulates a model node having a a parent/child relationship with other ModelNodes.
/// </summary>
public class Node
{
    private readonly NodeTree tree;
    private readonly List<Node> children = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="fullNameAndPath"></param>
    /// <param name="model"></param>
    /// <param name="parent"></param>
    internal Node(NodeTree tree, INodeModel model, string parentFullNameAndPath)
    {
        this.tree = tree;

        if (model is IModelAdapter adapter)
            adapter.Initialise();

        Name = model.Name;
        FullNameAndPath = $"{parentFullNameAndPath}.{Name}";
        Model = model;
    }

    /// <summary>The name of the model.</summary>
    public string Name { get; }

    /// <summary>The full path and name of the model.</summary>
    public string FullNameAndPath { get; }

    /// <summary>The parent ModelNode.</summary>
    public Node Parent { get; private set;}

    /// <summary>The model instance. Can be INodeModel or POCO.</summary>
    public INodeModel Model { get; }

    /// <summary>The child ModelNode instances.</summary>
    public IEnumerable<Node> Children => children;

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


    /// <summary>
    /// Walk nodes (depth first), returing each node. Uses recursion.
    /// </summary>
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
    /// Add child model.
    /// </summary>
    /// <param name="child">The child node to add.</param>
    public Node AddChild(INodeModel childModel)
    {
        // Create a child node to contain the child model.
        var childNode = new Node(tree, childModel, FullNameAndPath);
        childNode.Parent = this;
        children.Add(childNode);

        // Ensure the model is inserted into parent model.
        childNode.Model.SetParent(Model);
        if (!Model.GetChildren().Contains(childModel))
            Model.AddChild(childModel);

        // Update node map.
        tree.AddToNodeMap(childNode);

        // If we arean't in an initial setup phase then initialise the new child model just created.
        if (!tree.IsInitialising)
            InitialiseModel();

        // Recurse through all children.
        foreach (var c in childNode.Model.GetChildren())
            childNode.AddChild(c);

        return childNode;
    }

    /// <summary>
    /// Remove a child model.
    /// </summary>
    /// <param name="childModel">The child model to remove.</param>
    public void RemoveChild(INodeModel childModel)
    {
        // Remove the model from our children collection.
        Node nodeToRemove = children.Find(c => c.Model == childModel);
        if (nodeToRemove == null)
            throw new Exception($"Cannot find node {childModel.Name}");

        // Update nodemap.
        foreach (var node in nodeToRemove.Walk())
            tree.RemoveFromNodeMap(node.Model);

        // remove the model from it's parent model.
        Model.RemoveChild(childModel);

        // remove child node.
        children.Remove(nodeToRemove);
    }

    /// <summary>
    /// Replace a child model with another child.
    /// </summary>
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

    /// <summary>
    /// Insert a child node
    /// </summary>
    /// <param name="index">The position of the child in the children list.</param>
    /// <param name="childModels">The child model to add.</param>
    public Node InsertChild(int index, INodeModel childModel)
    {
        // Add the child model to children collection. It will be added to the end of the collection.
        Node childNode = AddChild(childModel);

        // Move the node to the correct position
        children.Remove(childNode);
        children.Insert(index, childNode);

        // Move the model to the correct position
        Model.RemoveChild(childModel);
        Model.InsertChild(index, childModel);

        return childNode;
    }

    /// <summary>
    /// Initialise a model
    /// </summary>
    /// <param name="node"></param>
    /// <param name="errorHandler"></param>
    /// <param name="compileManagers"></param>
    internal void InitialiseModel(Action<Exception> errorHandler = null)
    {
        // Replace all models that have a ResourceName with the official, released models from resources.
        Resource.Instance.Replace(tree);

        // Give services to all models that need it.
        foreach (var n in Walk().Where(n => n.Model is IServices))
            (n.Model as IServices).SetServices(tree);

        foreach (var model in Walk().Select(n => n.Model))
        {
            try
            {
                model.OnCreated();
            }
            catch (Exception err)
            {
                if (errorHandler == null)
                    throw;
                errorHandler(err);
            }
        }
    }
}