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
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        object CombinedRules { get; set; }

        /// <summary>
        /// The proportion of the filtered group to use
        /// </summary>
        double Proportion { get; set; }

        /// <summary>
        /// Retrieves infortmation on a property
        /// </summary>
        PropertyInfo GetProperty(string name);

        /// <summary>
        /// Filters the source using the group items
        /// </summary>
        IEnumerable<T> Filter<T>(IEnumerable<T> source);
    }
}
