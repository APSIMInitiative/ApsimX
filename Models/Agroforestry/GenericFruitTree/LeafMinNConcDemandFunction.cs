using System;
using APSIM.Core;
using Newtonsoft.Json;
using Models.Core;
using Models.Functions;
using Models.PMF.Organs;

namespace Models.Agroforestry
{
    /// <summary>
    /// Ensures leaf N demand is at least sufficient to build new structural DM
    /// at the leaf's minimum N concentration.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Models.PMF.NutrientPoolFunctions))]
    public class LeafMinNConcDemandFunction : Model, IFunction
    {
        /// <summary>Underlying (configured) N demand function.</summary>
        [JsonIgnore]
        public IFunction BaseDemand { get; set; }

        /// <summary>Target leaf organ for DM demand and min N conc.</summary>
        [JsonIgnore]
        public PerennialLeaf Leaf { get; set; }

        /// <inheritdoc/>
        public double Value(int arrayIndex = -1)
        {
            double baseVal = Math.Max(0.0, BaseDemand?.Value(arrayIndex) ?? 0.0);
            if (Leaf == null)
                return baseVal;

            double minNConc = Leaf.MinimumNConc?.Value() ?? 0.0;
            if (minNConc <= 0.0)
                return baseVal;

            double minDemand = Math.Max(0.0, Leaf.DMDemand.Structural) * minNConc;
            return Math.Max(baseVal, minDemand);
        }
    }
}
