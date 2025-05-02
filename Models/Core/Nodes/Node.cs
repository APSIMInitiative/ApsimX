using System;
using System.Collections.Generic;

namespace Models.Core;

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
    public Node(string name, string fullNameAndPath, object model)
    {
        Name = name;
        FullNameAndPath = fullNameAndPath;
        Model = model;
    }

    /// <summary>The name of the model.</summary>
    public string Name { get; }

    /// <summary>The full path and name of the model.</summary>
    public string FullNameAndPath { get; }

    /// <summary>The parent ModelNode.</summary>
    public Node Parent { get; private set;}

    /// <summary>The model instance.</summary>
    public object Model { get; }

    /// <summary>The child ModelNode instances.</summary>
    public IEnumerable<Node> Children => children;

    /// <summary>
    /// Add child nodes
    /// </summary>
    /// <param name="childNodes">The child nodes to add.</param>
    public void AddChildNodes(IEnumerable<Node> childNodes)
    {
        foreach (var child in childNodes)
        {
            child.Parent = this;
            if (Model is IModel model)
            model.Parent = Parent?.Model as IModel;
            children.Add(child);
        }
    }
}