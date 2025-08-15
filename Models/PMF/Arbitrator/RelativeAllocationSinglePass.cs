using System;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.PMF
{
    /// <summary>
    /// Single Pass Relative allocation rules used to determine partitioning.
    ///
    /// Arbitration is performed in a single pass for each of the biomass supply
    /// sources. Biomass is partitioned between organs based on their relative
    /// demand in a single pass so non-structural demands compete dirrectly with
    /// structural demands.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class RelativeAllocationSinglePass : Model, IArbitrationMethod
    {
        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        public void DoAllocation(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;
            ////allocate to all pools based on their relative demands
            for (int i = 0; i < Organs.Length; i++)
            {
                double StructuralRequirement = Math.Max(0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
                double MetabolicRequirement = Math.Max(0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
                double StorageRequirement = Math.Max(0, BAT.StorageDemand[i] - BAT.StorageAllocation[i]);
                if ((StructuralRequirement + MetabolicRequirement + StorageRequirement) > 0.0)
                {
                    double StructuralAllocation = Math.Min(StructuralRequirement, TotalSupply * MathUtilities.Divide(BAT.StructuralDemand[i], BAT.TotalPlantDemand, 0));
                    double MetabolicAllocation = Math.Min(MetabolicRequirement, TotalSupply * MathUtilities.Divide(BAT.MetabolicDemand[i], BAT.TotalPlantDemand, 0));
                    double StorageAllocation = Math.Min(StorageRequirement, TotalSupply * MathUtilities.Divide(BAT.StorageDemand[i], BAT.TotalPlantDemand, 0));

                    BAT.StructuralAllocation[i] += StructuralAllocation;
                    BAT.MetabolicAllocation[i] += MetabolicAllocation;
                    BAT.StorageAllocation[i] += StorageAllocation;
                    NotAllocated -= (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                    TotalAllocated += (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                }
            }
        }
    }
}
