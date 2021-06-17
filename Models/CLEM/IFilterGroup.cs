using Models.CLEM.Groupings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public abstract class FilterGroup : CLEMModel
    {
        /// <summary>
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        [JsonIgnore]
        public object CombinedRules { get; set; }

        /// <summary>
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        [JsonIgnore]
        public double Proportion { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public IEnumerable<T> Filter<T>(IEnumerable<T> source)
        {
            var rules = FindAllChildren<Filter>().Select(filter => filter.CompileRule<T>());

            if (rules.Any())
                return source?.Where(item => rules.All(rule => rule(item)));
            else
                return source;
        }
    }
}
