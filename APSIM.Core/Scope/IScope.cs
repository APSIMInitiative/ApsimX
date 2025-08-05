namespace APSIM.Core;

public interface IScope
{
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
