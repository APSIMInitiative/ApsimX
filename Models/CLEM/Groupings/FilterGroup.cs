using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Models.CLEM
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFilterable
    {

    }

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
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        PropertyInfo GetProperty(string name);

        /// <summary>
        /// Filters the source using the group items
        /// </summary>
        IEnumerable<T> Filter<T>(IEnumerable<T> source);
    }

    /// <summary>
    /// Implements IFilterGroup for a specific set of filter parameters
    /// </summary>
    [Serializable]
    public abstract class FilterGroup<TFilter> : CLEMModel, IFilterGroup
        where TFilter : IFilterable
    {
        /// <summary>
        /// 
        /// </summary>
        [NonSerialized]
        protected Dictionary<string, PropertyInfo> properties;

        /// <summary>
        /// 
        /// </summary>
        public FilterGroup()
        {
            properties = typeof(TFilter)
                .GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
                .ToDictionary(prop => prop.Name, prop => prop);

            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.Namespace != null && t.Namespace.Contains(nameof(Models.CLEM)))
                .Where(t => t.IsSubclassOf(typeof(TFilter)));

            foreach (var type in types)
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                foreach (var prop in props)
                    properties.Add(prop.DeclaringType.Name + "." + prop.Name, prop);
            }
        }

        /// <inheritdoc/>
        [JsonIgnore]
        public IEnumerable<string> Parameters => properties.Keys;         

        /// <inheritdoc/>
        [JsonIgnore]
        public object CombinedRules { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public double Proportion { get; set; }

        /// <inheritdoc/>
        public PropertyInfo GetProperty(string name) => properties[name];

        /// <inheritdoc/>
        public IEnumerable<T> Filter<T>(IEnumerable<T> source)
        {
            if (source is null)
                throw new NullReferenceException("Cannot filter a null object");

            var rules = FindAllChildren<Filter>().Select(filter => filter.CompileRule<T>());

            return rules.Any() ? source.Where(item => rules.All(rule => rule(item))) : source;
        }        
    }

}
