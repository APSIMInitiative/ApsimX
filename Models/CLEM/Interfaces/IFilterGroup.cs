using Models.Core;
using Newtonsoft.Json;
using System;
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
        /// Maps the property name to its reflected PropertyInfo
        /// </summary>
        [JsonIgnore]
        IEnumerable<string> Parameters { get; }

        /// <summary>
        /// Retrieves information on a property
        /// </summary>
        PropertyInfo GetProperty(string name);

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
