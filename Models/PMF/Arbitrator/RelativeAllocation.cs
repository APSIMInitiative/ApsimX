﻿using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.PMF
{
    /// <summary>
    /// Relative allocation rules used to determine partitioning.
    /// 
    /// Arbitration is performed in two passes for each of the supply sources.
    /// On the first pass, biomass or nutrient supply is allocated to structural
    /// and metabolic pools of each organ based on their demand relative to the
    /// demand from all organs.  On the second pass any remaining supply is
    /// allocated to non-structural pool based on the organ's relative demand.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    [ValidParent(ParentType = typeof(IArbitrator))]
    public class RelativeAllocation : Model, IArbitrationMethod
    {
        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TotalSupply">The total supply.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        public void DoAllocation(IArbitration[] Organs, double TotalSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            double NotAllocated = TotalSupply;

            // Save Totals for use inside the for loops below
            double totalStructuralDemand = BAT.TotalStructuralDemand;
            double totalMetabolicDemand = BAT.TotalMetabolicDemand;
            double totalStorageDemand = BAT.TotalStorageDemand;


            ////allocate to structural and metabolic Biomass first
            for (int i = 0; i < Organs.Length; i++)
            {
                double StructuralRequirement = Math.Max(0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]); //N needed to get to Minimum N conc and satisfy structural and metabolic N demands
                double MetabolicRequirement = Math.Max(0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
                if ((StructuralRequirement + MetabolicRequirement) > 0.0)
                {
                    double StructuralFraction = totalStructuralDemand / (totalStructuralDemand + totalMetabolicDemand);
                    double StructuralAllocation = Math.Min(StructuralRequirement, TotalSupply * StructuralFraction * MathUtilities.Divide(BAT.StructuralDemand[i], totalStructuralDemand, 0));
                    double MetabolicAllocation = Math.Min(MetabolicRequirement, TotalSupply * (1 - StructuralFraction) * MathUtilities.Divide(BAT.MetabolicDemand[i], totalMetabolicDemand, 0));
                    BAT.StructuralAllocation[i] += StructuralAllocation;
                    BAT.MetabolicAllocation[i] += MetabolicAllocation;
                    NotAllocated -= (StructuralAllocation + MetabolicAllocation);
                    TotalAllocated += (StructuralAllocation + MetabolicAllocation);
                }
            }
            // Second time round if there is still Biomass to allocate let organs take N up to their Maximum
            double FirstPassNotAllocated = NotAllocated;
            for (int i = 0; i < Organs.Length; i++)
            {
                double StorageRequirement = Math.Max(0.0, BAT.StorageDemand[i] - BAT.StorageAllocation[i]); //N needed to take organ up to maximum N concentration, Structural, Metabolic and Luxury N demands
                if (StorageRequirement > 0.0)
                {
                    double StorageAllocation = Math.Min(FirstPassNotAllocated * MathUtilities.Divide(BAT.StorageDemand[i], totalStorageDemand, 0), StorageRequirement);
                    BAT.StorageAllocation[i] += Math.Max(0, StorageAllocation);
                    NotAllocated -= StorageAllocation;
                    TotalAllocated += StorageAllocation;
                }
            }
            //Set the amount of biomass not allocated.  Note, that this value is overwritten following by each arbitration step so if it is to be used correctly 
            //it must be caught in that step.  Currently only using to catch DM not allocated so we can report as sink limitaiton
            BAT.NotAllocated = NotAllocated;
        }
    }
}
