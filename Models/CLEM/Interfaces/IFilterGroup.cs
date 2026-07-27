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
        /// Filters the source using the group items
        /// </summary>
        IEnumerable<T> Filter<T>(IEnumerable<T> source) where T : IFilterable;

        /// <summary>
        /// Determines if an item is in the filter group
        /// </summary>
        bool Filter<T>(T item) where T : IFilterable;

    }
}
