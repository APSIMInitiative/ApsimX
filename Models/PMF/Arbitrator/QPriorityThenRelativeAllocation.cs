using System;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.PMF
{
    /// <summary>
    /// Priority then Relative allocation rules used to determine partitioning.
    ///
    /// Arbitration is performed in two passes for each of the biomass supply sources.
    /// On the first pass, structural and metabolic biomass is allocated to each organ
    /// based on their order of priority with higher priority organs recieving their
    /// full demand first. On the second pass any remaining biomass is allocated to
    /// non-structural demands based on the relative demand from all organs.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    [ValidParent(ParentType = typeof(IArbitrator))]
    public class QPrioritythenRelativeAllocation : Model, IArbitrationMethod
    {
        /// <summary>Relatives the allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="TypeSupply">The biomass supply for the current supply type.</param>
        /// <param name="TotalAllocated">The total allocated.</param>
        /// <param name="BAT">The bat.</param>
        public void DoAllocation(IArbitration[] Organs, double TypeSupply, ref double TotalAllocated, BiomassArbitrationType BAT)
        {
            BiomassPoolType[] PriorityScalledDemands = new BiomassPoolType[Organs.Length];
            double TotalPlantPriorityScalledDemand = 0;
            for (int i = 0; i < Organs.Length; i++)
            {
                PriorityScalledDemands[i] = new BiomassPoolType();
                PriorityScalledDemands[i].Structural = BAT.StructuralDemand[i] * BAT.QStructural[i];
                PriorityScalledDemands[i].Metabolic = BAT.MetabolicDemand[i] * BAT.QMetabolic[i];
                PriorityScalledDemands[i].Storage = BAT.StorageDemand[i] * BAT.QStorage[i];
                TotalPlantPriorityScalledDemand += PriorityScalledDemands[i].Total;
            }

            double NotAllocated = TypeSupply;
            ////First time round allocate with priority factors applied so higher priority sinks get more allocation
            for (int i = 0; i < Organs.Length; i++)
            {
                double StructuralRequirement = Math.Max(0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]);
                double MetabolicRequirement = Math.Max(0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
                double StorageRequirement = Math.Max(0, BAT.StorageDemand[i] - BAT.StorageAllocation[i]);
                if ((StructuralRequirement + MetabolicRequirement + StorageRequirement) > 0.0)
                {
                    double StructuralAllocation = Math.Min(StructuralRequirement, TypeSupply * MathUtilities.Divide(PriorityScalledDemands[i].Structural, TotalPlantPriorityScalledDemand, 0));
                    double MetabolicAllocation = Math.Min(MetabolicRequirement, TypeSupply * MathUtilities.Divide(PriorityScalledDemands[i].Metabolic, TotalPlantPriorityScalledDemand, 0));
                    double StorageAllocation = Math.Min(StorageRequirement, TypeSupply * MathUtilities.Divide(PriorityScalledDemands[i].Storage, TotalPlantPriorityScalledDemand, 0));

                    BAT.StructuralAllocation[i] += StructuralAllocation;
                    BAT.MetabolicAllocation[i] += MetabolicAllocation;
                    BAT.StorageAllocation[i] += StorageAllocation;
                    NotAllocated -= (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                    TotalAllocated += (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                }
            }
            double FirstPassNotallocated = NotAllocated;
            double RemainingDemand = BAT.TotalPlantDemand - BAT.TotalPlantAllocation;
            // Second time round if there is still biomass to allocate do it based on relative demands so lower priority organs have the change to be allocated full demand
            for (int i = 0; i < Organs.Length; i++)
            {
                double StructuralRequirement = Math.Max(0, BAT.StructuralDemand[i] - BAT.StructuralAllocation[i]);
                double MetabolicRequirement = Math.Max(0, BAT.MetabolicDemand[i] - BAT.MetabolicAllocation[i]);
                double StorageRequirement = Math.Max(0, BAT.StorageDemand[i] - BAT.StorageAllocation[i]);
                if ((StructuralRequirement + MetabolicRequirement + StorageRequirement) > 0.0)
                {
                    double StructuralAllocation = 0;
                    double MetabolicAllocation = 0;
                    double StorageAllocation = 0;
                    if (!MathUtilities.FloatsAreEqual(RemainingDemand, 0.0, 0.000001))
                    {
                        StructuralAllocation = Math.Min(StructuralRequirement, FirstPassNotallocated * MathUtilities.Divide(StructuralRequirement, RemainingDemand, 0));
                        MetabolicAllocation = Math.Min(MetabolicRequirement, FirstPassNotallocated * MathUtilities.Divide(MetabolicRequirement, RemainingDemand, 0));
                        StorageAllocation = Math.Min(StorageRequirement, FirstPassNotallocated * MathUtilities.Divide(StorageRequirement, RemainingDemand, 0));
                    }

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
