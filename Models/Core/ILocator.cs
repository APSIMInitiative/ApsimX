using Models.Core;
using System;

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

        /// <summary>Gets a model in scope of the specified type</summary>
        /// <param name="typeToMatch">The type of the model to return</param>
        /// <returns>The found model or null if not found</returns>
        IModel Get(Type typeToMatch);

        /// <summary>
        /// Get the underlying variable object for the given path.
        /// </summary>
        /// <param name="namePath">The name of the variable to return</param>
        /// <returns>The found object or null if not found</returns>
        IVariable GetObject(string namePath);


    }
}