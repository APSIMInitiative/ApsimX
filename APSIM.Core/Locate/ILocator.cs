namespace APSIM.Core;

public interface ILocator
{
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
    public void Set(string namePath, object value);
}
