using System;
using APSIM.Core;
using Newtonsoft.Json;
using Models.Core;
using Models.Functions;
using Models.PMF.Organs;

namespace Models.Agroforestry
{
    /// <summary>
    /// Reserve storage N demand tied to reserve DM storage demand and wood minimum N concentration.
    /// Ensures storage DM sink is accompanied by a non-zero N demand when wood min N conc is non-zero.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Models.PMF.NutrientDemandFunctions))]
    public class ReserveStorageNDemandFunction : Model, IFunction
    {
        /// <summary>Optional configured baseline N demand to preserve user-provided behavior.</summary>
        [JsonIgnore]
        public IFunction BaseDemand { get; set; }

        /// <summary>Parent tree that computes reserve DM storage demand.</summary>
        [JsonIgnore]
        public GenericFruitTree Tree { get; set; }

        /// <summary>Wood organ used to read minimum N concentration.</summary>
        [JsonIgnore]
        public GenericOrgan Wood { get; set; }

        /// <inheritdoc/>
        public double Value(int arrayIndex = -1)
        {
            double baseVal = Math.Max(0.0, BaseDemand?.Value(arrayIndex) ?? 0.0);
            if (Tree == null || Wood == null || !Tree.IsAlive)
                return baseVal;

            double minNConc = Math.Max(0.0, Wood.MinNConc);
            if (minNConc <= 0.0)
                return baseVal;

            double reserveStorageDMDemand = Math.Max(0.0, Tree.GetReserveStorageDemandDM());
            double reserveStorageNDemand = reserveStorageDMDemand * minNConc;
            return Math.Max(baseVal, reserveStorageNDemand);
        }
    }
}
