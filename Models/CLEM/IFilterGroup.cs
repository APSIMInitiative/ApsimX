using Newtonsoft.Json;
using System;

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
    }
}
