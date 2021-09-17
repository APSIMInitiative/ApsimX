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
    [ValidParent(Exclude = true)]
    public abstract class FilterGroup<TFilter> : CLEMModel, IFilterGroup
        where TFilter : IFilterable
    {
        private protected IEnumerable<Func<IFilterable, bool>> filterRules = null;
        [NonSerialized]
        private protected IEnumerable<ISort> sortList = null;

        /// <summary>
        /// The properties available for filtering
        /// </summary>
        [NonSerialized]
        protected Dictionary<string, PropertyInfo> properties;

        /// <inheritdoc/>
        [JsonIgnore]
        public IEnumerable<string> Parameters => properties.Keys;

        /// <inheritdoc/>
        public PropertyInfo GetProperty(string name) => properties[name];

        /// <summary>
        /// Constructor
        /// </summary>
        public FilterGroup()
        {
            // needed for UI to access property lists
            InitialiseFilters();
        }

        ///<inheritdoc/>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            InitialiseFilters();
        }

        /// <summary>
        /// Initialise filter rules and dropdown lists of properties available for TFilter
        /// </summary>
        public void InitialiseFilters()
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

            foreach (Filter filter in FindAllChildren<Filter>())
            {
                filter.Initialise();
            }

            sortList = FindAllChildren<ISort>();
        }

        /// <inheritdoc/>
        public virtual IEnumerable<T> Filter<T>(IEnumerable<T> source) where T : IFilterable
        {
            if (source is null)
                throw new NullReferenceException("Cannot filter a null object");

            if (filterRules is null)
                filterRules = FindAllChildren<Filter>().Select(filter => filter.Compile<IFilterable>());

            // calculate the specified number/proportion of the filtered group to take from group
            int number = source.Count();
            foreach (var take in FindAllChildren<TakeFromFiltered>())
                number = take.NumberToTake(number);

            var filtered = (filterRules.Any() ? source.Where(item => filterRules.All(rule => rule(item))) : source);

            if(sortList?.Any()??false)
                // add sorting and take specified
                return filtered.Sort(sortList).Take(number); 
            else
                return filtered.Take(number);
        }

        ///<inheritdoc/>
        public virtual bool Filter<T>(T item) where T : IFilterable
        {
            if (item == null)
                throw new NullReferenceException("Cannot filter a null object");

            if (filterRules is null)
                filterRules = FindAllChildren<Filter>().Select(filter => filter.Compile<IFilterable>());

            return filterRules.All(rule => rule(item));
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
