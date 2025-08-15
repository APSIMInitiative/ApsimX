using System;
using System.Collections.Generic;
using APSIM.Core;
using Models.Core;
using Newtonsoft.Json;

namespace Models.Factorial
{
    /// <summary>
    /// A model representing an experiment's factors
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Experiment))]
    public class Factors : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>Gets the factors.</summary>
        /// <value>The factors.</value>
        [JsonIgnore]
        public List<Factor> factors
        {
            get
            {
                List<Factor> f = new List<Factor>();
                foreach (Factor factor in Structure.FindChildren<Factor>())
                    f.Add(factor);
                return f;
            }
        }
    }
}
