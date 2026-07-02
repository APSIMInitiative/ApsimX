using System;
using APSIM.Core;
using Newtonsoft.Json;
using Models.Core;
using Models.Functions;

namespace Models.Agroforestry
{
    /// <summary>
    /// Reserve storage demand driven by daily carbon surplus and reserve capacity.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Models.PMF.NutrientDemandFunctions))]
    public class ReserveStorageDemandFunction : Model, IFunction
    {
        /// <summary>Parent tree that computes reserve balance signals.</summary>
        [JsonIgnore]
        public GenericFruitTree Tree { get; set; }

        /// <inheritdoc/>
        public double Value(int arrayIndex = -1)
        {
            if (Tree == null || !Tree.IsAlive)
                return 0.0;

            return Tree.GetReserveStorageDemandDM();
        }
    }
}
