using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
        private protected IEnumerable<Func<IFilterable, bool>> filterRules = null;

        /// <summary>
        /// The properties available for filtering
        /// </summary>
        [NonSerialized]
        protected Dictionary<string, PropertyInfo> properties;

        /// <summary>
        /// Constructor, for objects created for UI
        /// </summary>
        public FilterGroup()
        {
            InitialiseFilters();
        }

        /// <inheritdoc/>
        [JsonIgnore]
        public IEnumerable<string> Parameters => properties.Keys;

        /// <inheritdoc/>
        public PropertyInfo GetProperty(string name) => properties[name];

        ///<inheritdoc/>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            InitialiseFilters();
        }

        /// <summary>
        /// Initialise filter rules and dropdown lists of properties available for TFilter
        /// </summary>
        private void InitialiseFilters()
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
                {
                    string key = prop.DeclaringType.Name;
                    if (key.StartsWith(typeof(TFilter).Name))
                        key = key.Substring(typeof(TFilter).Name.Length);
                    properties.Add($"{key}.{prop.Name}", prop);
                }
            }
        }

        /// <inheritdoc/>
        public virtual IEnumerable<T> Filter<T>(IEnumerable<T> source) where T : IFilterable
        {
            if (source is null)
                throw new NullReferenceException("Cannot filter a null object");

            if (filterRules is null)
                filterRules = FindAllChildren<Filter>().Select(filter => filter.Compile<IFilterable>());

            // add sorting

            // calculate the specified number/proportion of the filtered group to take from group
            int number = source.Count();
            foreach (var take in FindAllChildren<TakeFromFiltered>())
                // cummulative take through all TakeFromFiltered components
                number = take.NumberToTake(number);

            return filterRules.Any() ? source.Where(item => filterRules.All(rule => rule(item))).Take(number) : source.Take(number);
        }

        #region descriptive summary

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            return "\r\n</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\">");
                if (FindAllChildren<Filter>().Count() == 0)
                    htmlWriter.Write("<div class=\"filter\">All individuals</div>");

                return htmlWriter.ToString();
            }
        }


        #endregion
    }
}
