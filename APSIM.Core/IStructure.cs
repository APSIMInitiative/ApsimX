namespace APSIM.Core;

public interface IStructure
{

    // PROPERITES

    string FileName { get; set; }
    string Name { get; }
    string FullNameAndPath { get; }

    // FIND/MANIPULATE PARENT/CHILD MODELS


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

    /// <summary>
    /// Find a parent
    /// </summary>
    /// <typeparam name="T">Type of parent to find.</typeparam>
    /// <param name="name">Optional name of parent.</param>
    /// <param name="relativeTo">The node to make the find relative to.</param>
    /// <returns>Parent or null if not found.</returns>
    IEnumerable<T> FindParents<T>(string name = null, INodeModel relativeTo = null);

    void Rename(string name);
    void AddChild(INodeModel childModel);
    void InsertChild(int index, INodeModel childModel);
    void RemoveChild(INodeModel childModel);
    void ReplaceChild(INodeModel oldModel, INodeModel newModel);

    // LOCATOR METHODS

    /// <summary>
    /// Get the value of a variable or model.
    /// </summary>
    /// <param name="namePath">The name of the object to return</param>
    /// <param name="flags">Flags controlling the search</param>
    /// <returns>The found object or null if not found</returns>
    public object Get(string path, LocatorFlags flags = LocatorFlags.None, INodeModel relativeTo = null);

    /// <summary>
    /// Get the value of a variable or model.
    /// </summary>
    /// <param name="namePath">The name of the object to return</param>
    /// <param name="flags">Flags controlling the search</param>
    /// <returns>The found object or null if not found</returns>
    public VariableComposite GetObject(string path, LocatorFlags flags = LocatorFlags.None, INodeModel relativeTo = null);

    /// <summary>Clear the cache</summary>
    public void ClearLocator();

    /// <summary>
    /// Remove a single entry from the cache.
    /// Should be called if the old path may become invalid.
    /// </summary>
    /// <param name="path"></param>
    public void ClearEntry(string path);

    /// <summary>
    /// Set the value of a variable. Will throw if variable doesn't exist.
    /// </summary>
    /// <param name="namePath">The name of the object to set</param>
    /// <param name="value">The value to set the property to</param>
    public void Set(string namePath, object value, INodeModel relativeTo = null);

    // SCOPE METHODS

    /// <summary>
    /// Get a model in scope.
    /// </summary>
    /// <param name="name">The name of the model to return. Can be null.</param>
    /// <param name="relativeTo">The model to use when determining scope.</param>
    /// <returns>The found model or null if not found</returns>
    T Find<T>(string name = null, INodeModel relativeTo = null);

    /// <summary>
    /// Get models in scope.
    /// </summary>
    /// <param name="name">The name of the model to return. Can be null.</param>
    /// <param name="relativeTo">The model to use when determining scope.</param>
    /// <returns>All matching models.</returns>
    IEnumerable<T> FindAll<T>(string name = null, INodeModel relativeTo = null);
}
