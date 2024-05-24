using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Xml.Serialization;

namespace Models.CLEM
{
    /// <summary>
    /// Implements IFilterGroup for a specific set of filter parameters
    /// </summary>
    [Serializable]
    public abstract class FilterGroup<TFilter> : CLEMModel, IFilterGroup, IActivityCompanionModel
        where TFilter : IFilterable
    {
        [NonSerialized]
        private protected IEnumerable<Func<IFilterable, bool>> filterRules = null;
        [NonSerialized]
        private protected IEnumerable<ISort> sortList = null;

        /// <inheritdoc/>
        [Description("Remove inherent order before sorting")]
        [Core.Display(Order = 7000)]
        public bool RandomiseBeforeSorting { get; set; }

        /// <summary>
        /// The properties available for filtering
        /// </summary>
        [NonSerialized]
        protected Dictionary<string, List<PropertyInfo>> properties;

        /// <summary>
        /// An identifier for this FilterGroup based on parent requirements
        /// </summary>
        [Description("Group identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers", VisibleCallback = "ParentSuppliedIdentifiersPresent")]
        public string Identifier { get; set; }

        /// <inheritdoc/>
        [XmlIgnore]
        public virtual string Measure
        {
            get { return ""; }
            set {; }
        }

        /// <inheritdoc/>
        [JsonIgnore]
        public IEnumerable<string> Parameters => properties?.Keys;

                /// <inheritdoc/>
        public IEnumerable<string> GetParameterNames()
        {
            if (properties is null)
                InitialiseFilters(false);

            return properties.Keys.OrderBy(k => k);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public FilterGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.Filter;
        }

        /// <inheritdoc/>
        public IEnumerable<PropertyInfo> GetProperty(string name) 
        {
            if (properties is null)
                InitialiseFilters(false);

            if(properties.ContainsKey(name))
                return properties[name]; 
            else
                return new List<PropertyInfo>();
        }

        /// <summary>
        /// Clear all rules
        /// </summary>
        public void ClearRules()
        {
            foreach (Filter filter in FindAllChildren<Filter>())
                filter.ClearRule();
        }

        ///<inheritdoc/>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            filterRules = null;
            sortList = null;
            if(!GetType().Name.Contains("Linked"))
                InitialiseFilters();
        }

        /// <summary>
        /// Initialise filter rules and dropdown lists of properties available for TFilter
        /// </summary>
        public void InitialiseFilters(bool includeBuildRules = true)
        {
            properties = GetNestedProperties(typeof(TFilter), "", new List<PropertyInfo>());
            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.Namespace != null && t.Namespace.Contains(nameof(Models.CLEM)))
                .Where(t => t.IsSubclassOf(typeof(TFilter)));
            foreach (var subtype in types)
            {
                foreach (var sub in GetNestedProperties(subtype, subtype.Name, new List<PropertyInfo>()))
                    properties.Add(sub.Key, sub.Value);
            }

            properties =  properties.OrderBy(x => x.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

            //properties = typeof(TFilter)
            //    .GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
            //    .Where(prop => Attribute.IsDefined(prop, typeof(FilterByPropertyAttribute)))
            //    .ToDictionary(prop => prop.Name, prop => prop);

            //foreach (var property in properties.Values.ToList())
            //{
            //    var subProperties = property.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
            //        .Where(prop => Attribute.IsDefined(prop, typeof(FilterByPropertyAttribute)))
            //        .ToDictionary(prop => $"{property.Name}.{prop.Name}", prop => prop);
            //    foreach (var item in subProperties)
            //        properties.Add(item.Key, item.Value);
            //}

            //var types = Assembly.GetExecutingAssembly()
            //    .GetTypes()
            //    .Where(t => t.Namespace != null && t.Namespace.Contains(nameof(Models.CLEM)))
            //    .Where(t => t.IsSubclassOf(typeof(TFilter)));

            //foreach (var type in types)
            //{
            //    var props = type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
            //                        .Where(prop => Attribute.IsDefined(prop, typeof(FilterByPropertyAttribute)));
            //    foreach (var prop in props)
            //    {
            //        string key = prop.DeclaringType.Name;
            //        if (key.StartsWith(typeof(TFilter).Name))
            //            key = key.Substring(typeof(TFilter).Name.Length);
            //        properties.Add($"{key}.{prop.Name}", prop);
            //    }
            //}

            foreach (Filter filter in FindAllChildren<Filter>())
            {
                filter.Initialise();
                if (includeBuildRules)
                    filter.BuildRule();
            }

            sortList = FindAllChildren<ISort>();
        }

        ///// <summary>
        ///// Recursively gets all properties of a type where FilterByPropertyAttribute is present.
        ///// </summary>
        ///// <param name="type">The type to get properties from</param>
        ///// <param name="header">Recursive name header tracker</param>
        ///// <param name="nestedPropertyList"></param>
        ///// <returns>A list of PropertyInfo objects representing the properties</returns>
        //public static IEnumerable<string, List<PropertyInfo>> GetNestedPropertiesYield(Type type, string header, List<PropertyInfo> nestedPropertyList)
        //{
        //     var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
        //        .Where(prop => Attribute.IsDefined(prop, typeof(FilterByPropertyAttribute)));
        //        //.ToDictionary(prop => $"{(header == string.Empty ? "" : $"{header}.")}{prop.Name}", prop => (nestedPropertyList is null) ? new List<PropertyInfo> { prop } : nestedPropertyList.Concat(new List<PropertyInfo> { prop }).ToList());

        //    foreach (var prop in allProperties)
        //    {
        //        var subProperties = GetNestedPropertiesYield(prop.PropertyType, $"{(header == string.Empty ? "" : $"{header}.")}{prop.Name}");
        //        if (!subProperties.Any())
        //            yield return new KeyValuePair<string, List<PropertyInfo>>($"{(header == string.Empty ? "" : $"{header}.")}{prop.Name}", new List<PropertyInfo> { prop });
        //            //properties.Remove(prop.Key);

        //        foreach (var sub in subProperties)
        //        {
        //            properties.Add(sub.Key, sub.Value);
        //        }
        //    }
        //    return properties;
        //}




        /// <summary>
        /// Recursively gets all properties of a type where FilterByPropertyAttribute is present.
        /// </summary>
        /// <param name="type">The type to get properties from</param>
        /// <param name="header">Recursive name header tracker</param>
        /// <param name="nestedPropertyList"></param>
        /// <returns>A list of PropertyInfo objects representing the properties</returns>
        public static Dictionary<string, List<PropertyInfo>> GetNestedProperties(Type type, string header, List<PropertyInfo> nestedPropertyList)
        {
            Dictionary<string, List<PropertyInfo>> properties = type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
                .Where(prop => Attribute.IsDefined(prop, typeof(FilterByPropertyAttribute)))
                .ToDictionary(prop => $"{(header == string.Empty ? "" : $"{header}.")}{prop.Name}", prop => (nestedPropertyList is null) ? new List<PropertyInfo> { prop } : nestedPropertyList.Concat(new List<PropertyInfo> { prop }).ToList());

            foreach (var prop in properties.ToList())
            {
                var subProperties = GetNestedProperties(prop.Value.Last().PropertyType, $"{(header == string.Empty ? "" : $"{header}.")}{prop.Value.Last().Name}", prop.Value);
                if (subProperties.Any())
                    properties.Remove(prop.Key);

                foreach (var sub in subProperties)
                {
                    properties.Add(sub.Key, sub.Value);
                }
            }
            return properties;
        }

        /// <summary>
        /// Recursively gets all properties of a type where FilterByPropertyAttribute is present.
        /// </summary>
        /// <param name="type">The type to get properties from</param>
        /// <param name="header">Recursive name header tracker</param>
        /// <returns>A list of PropertyInfo objects representing the properties</returns>
        public static Dictionary<string, PropertyInfo> GetNestedProperties(Type type, string header)
        {
            Dictionary<string, PropertyInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
                .Where(prop => Attribute.IsDefined(prop, typeof(FilterByPropertyAttribute)))
                .ToDictionary(prop => $"{(header == string.Empty ? "" : $"{header}.")}{prop.Name}", prop => prop);

            foreach (var prop in properties.Values.ToList())
            {
                var subProperties = GetNestedProperties(prop.PropertyType, $"{(header == string.Empty?"":$"{header}.")}{prop.Name}");
                if(subProperties.Any())
                    properties.Remove(prop.Name);

                foreach (var sub in subProperties)
                {
                    properties.Add(sub.Key, sub.Value);
                }
            }
            return properties;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<T> Filter<T>(IEnumerable<T> source) where T : IFilterable
        {
            if (source is null)
                throw new NullReferenceException("Cannot filter a null object");

            filterRules ??= FindAllChildren<Filter>().Select(filter => filter.Rule);

            var filtered = filterRules.Any() ? source.Where(item => filterRules.All(rule => rule is null ? false : rule(item))) : source;

            if(sortList?.Any()??false)
                // add sorting and take specified
                filtered = filtered.Sort(sortList, RandomiseBeforeSorting);

            // do all takes and skips
            foreach (var take in FindAllChildren<TakeFromFiltered>())
            {
                int number = 0;
                switch (take.TakeStyle)
                {
                    case TakeFromFilterStyle.TakeProportion:
                    case TakeFromFilterStyle.SkipProportion:
                        number = take.NumberToTake(filtered.Count());
                        break;
                    case TakeFromFilterStyle.TakeIndividuals:
                    case TakeFromFilterStyle.SkipIndividuals:
                        number = take.NumberToTake(filtered.Count());
                        break;
                }
                switch (take.TakeStyle)
                {
                    case TakeFromFilterStyle.TakeProportion:
                    case TakeFromFilterStyle.TakeIndividuals:
                        if (take.TakePositionStyle == TakeFromFilteredPositionStyle.Start)
                            filtered = filtered.Take(number);
                        else
                            filtered = filtered.TakeLast(number);
                        break;
                    case TakeFromFilterStyle.SkipProportion:
                    case TakeFromFilterStyle.SkipIndividuals:
                        if (take.TakePositionStyle == TakeFromFilteredPositionStyle.Start)
                            filtered = filtered.Skip(number);
                        else
                            filtered = filtered.SkipLast(number);
                        break;
                }
            }
            return filtered;
        }

        ///<inheritdoc/>
        public virtual bool Filter<T>(T item) where T : IFilterable
        {
            if (item == null)
                throw new NullReferenceException("Cannot filter a null object");

            filterRules ??= FindAllChildren<Filter>().Select(filter => filter.Rule);

            return filterRules.All(rule => rule is null ? false : rule(item));
        }

        /// <inheritdoc/>
        public virtual void PrepareForTimestep()
        {
        }

        /// <inheritdoc/>
        public virtual List<ResourceRequest> RequestResourcesForTimestep(double activityMetric)
        {
            return null;
        }

        /// <inheritdoc/>
        public virtual void PerformTasksForTimestep(double activityMetric)
        {
        }

        #region descriptive summary

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags()
        {
            return "\r\n</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"filterborder clearfix\">");
            if (!FindAllChildren<Filter>().Any())
                htmlWriter.Write("<div class=\"filter\">All individuals</div>");

            return htmlWriter.ToString();
        }
        #endregion
    }
}
