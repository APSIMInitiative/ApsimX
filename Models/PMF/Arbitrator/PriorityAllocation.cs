﻿using System;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.PMF
{
    /// <summary>
    /// Priority allocation rules used to determine partitioning.
    /// 
    /// Arbitration is performed in two passes for each of the biomass supply sources.
    /// On the first pass, structural and metabolic biomass is allocated to each organ
    /// based on their order of priority with higher priority organs recieving their
    /// full demand first.  On the second pass any remaining biomass is allocated to
    /// non-structural demands based on the same order of priority.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    [ValidParent(ParentType = typeof(IArbitrator))]
    public class PriorityAllocation : Model, IArbitrationMethod
    {
        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        public void DoAllocation(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;
            ////First time round allocate to met priority demands of each organ
            for (int i = 0; i < Organs.Length; i++)
            {
                double StructuralRequirement = Math.Max(0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
                double MetabolicRequirement = Math.Max(0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
                if ((StructuralRequirement + MetabolicRequirement) > 0.0)
                {
                    double StructuralFraction = BAT.StructuralDemand[i] / (BAT.StructuralDemand[i] + BAT.MetabolicDemand[i]);
                    double StructuralAllocation = Math.Min(StructuralRequirement, NotAllocated * StructuralFraction);
                    double MetabolicAllocation = Math.Min(MetabolicRequirement, NotAllocated * (1 - StructuralFraction));
                    BAT.StructuralAllocation[i] += StructuralAllocation;
                    BAT.MetabolicAllocation[i] += MetabolicAllocation;
                    NotAllocated -= (StructuralAllocation + MetabolicAllocation);
                    TotalAllocated += (StructuralAllocation + MetabolicAllocation);
                }
            }
            // Second time round if there is still N to allocate let organs take N up to their Maximum
            for (int i = 0; i < Organs.Length; i++)
            {
                double StorageRequirement = Math.Max(0, BAT.StorageDemand[i] - BAT.StorageAllocation[i]);
                if (StorageRequirement > 0.0)
                {
                    double StorageAllocation = Math.Min(StorageRequirement, NotAllocated);
                    BAT.StorageAllocation[i] += Math.Max(0, StorageAllocation);
                    NotAllocated -= StorageAllocation;
                    TotalAllocated += StorageAllocation;
                }
            }
        }
    }
}
