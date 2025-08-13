namespace APSIM.Core;

public interface IStructure
{

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
