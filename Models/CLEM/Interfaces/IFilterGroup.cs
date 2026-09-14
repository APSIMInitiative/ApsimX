using Models.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// The parent model for holding a collection of filters
    /// </summary>
    public interface IFilterGroup : IModel
    {
        /// <summary>
        /// Perform a shuffle before sorting to remove inherent order from adding to herd
        /// </summary>
        bool RandomiseBeforeSorting { get; set; }

        /// <summary>
        /// Maps the property name to its reflected PropertyInfo
        /// </summary>
        [JsonIgnore]
        IEnumerable<string> Parameters { get; }

        /// <summary>
        /// Retrieves a list of parameters available from the generic type being filtered
        /// </summary>
        IEnumerable<string> GetParameterNames();

        /// <summary>
        /// Retrieves information on a property
        /// </summary>
        IEnumerable<PropertyInfo> GetProperty(string name);

        /// <summary>
        /// Get the value of a property allowing for nested properties.
        /// </summary>
        /// <param name="name">
        /// Name of the property provided in properties list with and period separated nesting
        /// </param>
        /// <param name="parentTopLevel">The first level object to search from</param>
        /// <returns>Property value as an object</returns>
        object GetPropertyValue(string name, object parentTopLevel);

        /// <summary>
        /// Filters the source using the group items
        /// </summary>
        IEnumerable<T> Filter<T>(IEnumerable<T> source) where T : IFilterable;

        /// <summary>
        /// Determines if an item is in the filter group
        /// </summary>
        bool Filter<T>(T item) where T : IFilterable;

    }
}
