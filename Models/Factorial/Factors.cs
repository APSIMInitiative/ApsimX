using System;
using System.Collections.Generic;
using Models.Core;
using Newtonsoft.Json;
using APSIM.Core;

namespace Models.Factorial
{
    /// <summary>
    /// A model representing an experiment's factors
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Experiment))]
    public class Factors : Model, IScopedModel
    {
        /// <summary>Gets the factors.</summary>
        /// <value>The factors.</value>
        [JsonIgnore]
        public List<Factor> factors
        {
            get
            {
                List<Factor> f = new List<Factor>();
                foreach (Factor factor in this.FindAllChildren<Factor>())
                    f.Add(factor);
                return f;
            }
        }
    }
}
