using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Models.CLEM
{
    /// <summary>
    /// Implements IFilterGroup for a specific set of filter parameters
    /// </summary>
    [Serializable]
    public abstract class FilterGroup<TFilter> : CLEMModel, IFilterGroup
        where TFilter : IFilterable
    {
        /// <summary>
        /// The properties available for filtering
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
        public double Proportion { get; set; }

        /// <inheritdoc/>
        public PropertyInfo GetProperty(string name) => properties[name];

        /// <summary>
        /// Return some proportion of a ruminant collection after filtering
        /// </summary>
        public IEnumerable<T> FilterProportion<T>(IEnumerable<T> source)
        {
            double proportion = Proportion <= 0 ? 1 : Proportion;
            int number = Convert.ToInt32(Math.Ceiling(proportion * source.Count()));

            return Filter(source).Take(number);
        }

        /// <inheritdoc/>
        public virtual IEnumerable<T> Filter<T>(IEnumerable<T> source)
        {
            if (source is null)
                throw new NullReferenceException("Cannot filter a null object");

            var rules = FindAllChildren<Filter>().Select(filter => filter.Compile<T>());

            return rules.Any() ? source.Where(item => rules.All(rule => rule(item))) : source;
        }
    }
}
