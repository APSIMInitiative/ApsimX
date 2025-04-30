using System;
using System.Collections.Generic;

namespace Models.Core;

/// <summary>
/// Constructs and maintains a tree of ModelNode instances for all models in a .apsimx file.
/// </summary>
public class ModelNodeTree
{
    private readonly ModelDiscovery modelDiscovery = new();

    /// <summary>Dictionary that maps object instances to ModelNodes. This is done for quick access to a ModelNode, given an object (e.g. IModel or POCO class) instance.</summary>
    private Dictionary<object, ModelNode> nodeMap = [];

    /// <summary>Root node for tree hierarchy.</summary>
    private ModelNode rootNode;

    /// <summary>The POCO discovery function delegate</summary>
    public delegate (string name, IEnumerable<object> children) DiscoveryFuncDelegate(object obj);


    /// <summary>
    /// Build the parent / child map.
    /// </summary>
    /// <param name="root">The root node.</param>
    public void Initialise(object root)
    {
        rootNode = AddNode(root, null);
    }

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
    public ModelNode GetNode(object instance = null)
    {
        if (instance == null)
            return rootNode;
        else if (nodeMap.TryGetValue(instance, out var details))
            return details;
        else
            throw new Exception($"Cannot find details for object");  // shouldn't happen.
    }

    /// <summary>
    /// Add a ModelNode to the nodeMap for the specified object. NOTE: This is recursive.
    /// </summary>
    /// <param name="obj">The object to create a Node for.</param>
    /// <param name="parent">The parent ModelNode.</param>
    /// <returns>The newly created ModelNode</returns>
    private ModelNode AddNode(object obj, ModelNode parent)
    {
        var (name, children) = modelDiscovery.GetNameAndChildrenOfObj(obj);

        ModelNode parentChildren = new()
        {
            Name = name,
            FullNameAndPath = $"{parent?.FullNameAndPath}.{name}",
            Parent = parent,
            Instance = obj
        };

        // Replace class adaptors with their instance. This will remove all ClassAdaptors from the parent child tree.
        if (children != null)
        {
            parentChildren.Children = [];
            foreach (var child in children)
            {
                if (child is ClassAdaptor classAdaptor)
                    parentChildren.Children.Add(AddNode(classAdaptor.Obj, parentChildren));
                else
                    parentChildren.Children.Add(AddNode(child, parentChildren));
            }
        }

        nodeMap.Add(obj, parentChildren);
        return parentChildren;
    }
}