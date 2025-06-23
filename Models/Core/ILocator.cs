using System;
using APSIM.Core;

namespace Models.Core
{
    /// <summary>
    /// Flags to control options in the Locator
    /// </summary>
    [Flags]
    public enum LocatorFlags
    {
        /// <summary>
        /// The default - treats the other options as "false"
        /// </summary>
        None = 0,

        /// <summary>
        /// If set, does a case-sensitive search; otherwise is case-insensitive
        /// </summary>
        CaseSensitive = 1,

        /// <summary>
        /// If set, fetch only property information, but not the value
        /// </summary>
        PropertiesOnly = 2,

        /// <summary>
        /// If set, disabled models are included in the search; otherwise they are excluded
        /// </summary>
        IncludeDisabled = 4,

        /// <summary>
        /// If set, any "errors" will result in an exception being thrown; otherwise null is returned
        /// </summary>
        ThrowOnError = 8,

        /// <summary>
        /// If set, Report columns will be considered in the search; otherwise these are ignored
        /// </summary>
        IncludeReportVars = 16,

        /// <summary>
        /// If set, fetch only model references, do not return properties or methods of the same name
        /// </summary>
        ModelsOnly = 32,
    };

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
        /// <param name="flags"><see cref="LocatorFlags"/> controlling the search</param>
        object Get(string namePath, LocatorFlags flags = LocatorFlags.None);

        /// <summary>
        /// Get the underlying variable object for the given path.
        /// </summary>
        /// <param name="namePath">The name of the variable to return</param>
        /// <param name="flags">LocatorFlags controlling the search</param>
        /// <returns>The found object or null if not found</returns>
        IVariable GetObject(string namePath, LocatorFlags flags);

        /// <summary>
        /// Get the properties of the underlying variable object for the given path.
        /// Unlike the GetObject method, this does not return the data value of the object.
        /// </summary>
        /// <param name="namePath">The name of the variable to return</param>
        /// <param name="flags">LocatorFlags controlling the search</param>
        /// <returns>The found object or null if not found</returns>
        IVariable GetObjectProperties(string namePath, LocatorFlags flags = LocatorFlags.PropertiesOnly);

    }
}