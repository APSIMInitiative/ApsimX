using System;
using Models.Core;

namespace Models.PMF
{

    /// <summary>
    /// The class that holds the states for resource supplies from ReAllocation, Uptake, Fixation and ReTranslocation
    /// </summary>
    [Serializable]
    public class OrganNutrientSupplies : Model
    {
        /// <summary>Gets or sets the fixation.</summary>
        public double Fixation { get; set; }
        /// <summary>Gets or sets the reallocation.</summary>
        public NutrientPoolsState ReAllocation { get; set; }
        /// <summary>Gets or sets the uptake.</summary>
        public double Uptake { get; set; }
        /// <summary>Gets or sets the retranslocation.</summary>
        public NutrientPoolsState ReTranslocation { get; set; }

        /// <summary>Gets the total supply.</summary>
        public double Total
        { get { return Fixation + ReAllocation.Total + ReTranslocation.Total + Uptake; } }

        /// <summary>The constructor.</summary>
        public OrganNutrientSupplies()
        {
            Fixation = new double();
            ReAllocation = new NutrientPoolsState(0, 0, 0);
            Uptake = new double();
            ReTranslocation = new NutrientPoolsState(0, 0, 0);
        }

        internal void Clear()
        {
            Fixation = 0;
            ReAllocation = new NutrientPoolsState(0, 0, 0);
            Uptake = 0;
            ReTranslocation = new NutrientPoolsState(0, 0, 0);
        }
    }
}
