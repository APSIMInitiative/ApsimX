using System;
using System.Collections.Generic;
using System.Linq;
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
    public class Factors : Model
    {
        /// <summary>Gets the factors.</summary>
        /// <value>The factors.</value>
        [JsonIgnore]
        public List<Factor> factors
        {
            get
            {
                List<Factor> f = new List<Factor>();
                foreach (Factor factor in Node.FindChildren<Factor>(recurse: true))
                    if (factor.Node.FindParent<Permutation>(recurse: true) == null)
                        f.Add(factor);

                return f;
            }
        }
    }
}
