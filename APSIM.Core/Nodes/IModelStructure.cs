namespace APSIM.Core;

public interface IModelStructure
{
    string FileName { get; set; }
    string Name { get; }
    string FullNameAndPath { get; }

    /// <summary>
    /// Find a child.
    /// </summary>
    /// <typeparam name="T">Type of child to find.</typeparam>
    /// <param name="name">Optional name of child.</param>
    /// <param name="recurse">Recursively look for child?</param>
    /// <param name="relativeTo">The node to make the find relative to.</param>
    /// <returns>Child or null if not found.</returns>
    T FindChild<T>(string name = null, bool recurse = false, INodeModel relativeTo = null);

    /// <summary>
    /// Find all children direct and/or recursively.
    /// </summary>
    /// <typeparam name="T">Type of child nodes to find.</typeparam>
    /// <param name="name">Optional name of child.</param>
    /// <param name="recurse">Recursively look for children?</param>
    /// <param name="relativeTo">The node to make the find relative to.</param>
    /// <returns>Collection of child nodes or empty collection</returns>
    IEnumerable<T> FindChildren<T>(string name = null, bool recurse = false, INodeModel relativeTo = null);

    /// <summary>
    /// Find a sibling
    /// </summary>
    /// <typeparam name="T">Type of sibling to find.</typeparam>
    /// <param name="name">Optional name of child.</param>
    /// <param name="relativeTo">The node to make the find relative to.</param>
    /// <returns>Sibling or null if not found.</returns>
    T FindSibling<T>(string name = null, INodeModel relativeTo = null);

    /// <summary>
    /// Find all siblings.
    /// </summary>
    /// <typeparam name="T">Type of siblings to find.</typeparam>
    /// <param name="name">Optional name of siblings.</param>
    /// <param name="relativeTo">The node to make the find relative to.</param>
    /// <returns>Collection of sibling nodes or empty collection</returns>
    IEnumerable<T> FindSiblings<T>(string name = null, INodeModel relativeTo = null);

    /// <summary>
    /// Find a parent
    /// </summary>
    /// <typeparam name="T">Type of parent to find.</typeparam>
    /// <param name="name">Optional name of parent.</param>
    /// <param name="relativeTo">The node to make the find relative to.</param>
    /// <returns>Parent or null if not found.</returns>
    T FindParent<T>(string name = null, bool recurse = false, INodeModel relativeTo = null);


    void Rename(string name);
    void AddChild(INodeModel childModel);
    void InsertChild(int index, INodeModel childModel);
    void RemoveChild(INodeModel childModel);
    void ReplaceChild(INodeModel oldModel, INodeModel newModel);
}