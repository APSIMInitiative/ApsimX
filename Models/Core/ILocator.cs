using Models.Core;

namespace Models.Core
{
    /// <summary>
    /// An interface for locating variables/models at runtime.
    /// </summary>
    public interface ILocator
    {
        /// <summary>
        /// Gets the value of a variable or model. Case insensitive. 
        /// </summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <returns>The found object or null if not found</returns>
        object Get(string namePath);

        /// <summary>
        /// Get the underlying variable object for the given path.
        /// </summary>
        /// <param name="namePath">The name of the variable to return</param>
        /// <returns>The found object or null if not found</returns>
        IVariable GetObject(string namePath);


    }
}