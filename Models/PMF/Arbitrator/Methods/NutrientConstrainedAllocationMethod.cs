using System;
using System.Linq;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.PMF.Arbitrator
{
    /// <summary>Determines Nutrient limitations to DM allocations</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class NutrientConstrainedAllocationMethod : Model, IAllocationMethod
    {
        /// <summary>Determines Nutrient limitations to DM allocations</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        protected IArbitrator Arbitrator = null;

        /// <summary>Determines Nutrient limitations to DM allocations</summary>
        public void Allocate(IArbitration[] Organs, BiomassArbitrationType DM)
        {
            var N = Arbitrator.N;

            double PreNStressDMAllocation = DM.Allocated;
            for (int i = 0; i < Organs.Length; i++)
                N.TotalAllocation[i] = N.StructuralAllocation[i] + N.MetabolicAllocation[i] + N.StorageAllocation[i];

            N.Allocated = N.TotalAllocation.Sum();

            //To introduce functionality for other nutrients we need to repeat this for loop for each new nutrient type
            // Calculate posible growth based on Minimum N requirement of organs
            for (int i = 0; i < Organs.Length; i++)
            {
                double TotalNDemand = N.StructuralDemand[i] + N.MetabolicDemand[i] + N.StorageDemand[i];
                if (N.TotalAllocation[i] > TotalNDemand || MathUtilities.FloatsAreEqual(N.TotalAllocation[i], TotalNDemand))
                    N.ConstrainedGrowth[i] = 100000000; //given high value so where there is no N deficit in organ and N limitation to growth
                else
                    if (N.TotalAllocation[i] == 0 | Organs[i].MinNConc == 0)
                    N.ConstrainedGrowth[i] = 0;
                else
                    //N.ConstrainedGrowth[i] = Math.Max((N.TotalAllocation[i] + Organs[i].Live.N) / Organs[i].MinNConc - Organs[i].Live.Wt, 0);
                    N.ConstrainedGrowth[i] = N.TotalAllocation[i] / Organs[i].MinNConc;
            }

            // Reduce DM allocation below potential if insufficient N to reach Min n Conc or if DM was allocated to fixation
            for (int i = 0; i < Organs.Length; i++)
                if ((DM.MetabolicAllocation[i] + DM.StructuralAllocation[i]) != 0)
                {
                    double MetabolicProportion = DM.MetabolicAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i] + DM.StorageAllocation[i]);
                    double StructuralProportion = DM.StructuralAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i] + DM.StorageAllocation[i]);
                    double StorageProportion = DM.StorageAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i] + DM.StorageAllocation[i]);
                    DM.MetabolicAllocation[i] = Math.Min(DM.MetabolicAllocation[i], N.ConstrainedGrowth[i] * MetabolicProportion);
                    DM.StructuralAllocation[i] = Math.Min(DM.StructuralAllocation[i], N.ConstrainedGrowth[i] * StructuralProportion);  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function
                    DM.StorageAllocation[i] = Math.Min(DM.StorageAllocation[i], N.ConstrainedGrowth[i] * StorageProportion);  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function

                    //Question.  Why do I not restrain non-structural DM allocations.  I think this may be wrong and require further thought HEB 15-1-2015
                }
            //Recalculated DM Allocation totals
            DM.Allocated = DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation + DM.TotalStorageAllocation;
            DM.NutrientLimitation = (PreNStressDMAllocation - DM.Allocated);
            /*//unused DM, calculate proportions of total supply
            double unusedDM = DM.NutrientLimitation;
            if (unusedDM > 0 && DM.TotalPlantSupply > 0)
            {
                // Calculate proportions of total supply
                double totalSupply = DM.TotalPlantSupply;
                double totalRealloc = DM.TotalReallocation;
                double totalRetrans = DM.TotalRetranslocation;

                double unusedRealloc = unusedDM * (totalRealloc / totalSupply);
                double unusedRetrans = unusedDM * (totalRetrans / totalSupply);
                // Redistribute unused reallocation and retranslocation proportionally back to each organ
                for (int i = 0; i < Organs.Length; i++)
                {
                    if (totalRealloc > 0)
                        DM.Reallocation[i] -= unusedRealloc * (DM.Reallocation[i] / totalRealloc);
                    if (totalRetrans > 0)
                        DM.Retranslocation[i] -= unusedRetrans * (DM.Retranslocation[i] / totalRetrans);
                }

                // Reset NutrientLimitation to only reflect truly "lost" biomass (e.g., fixation that can't be used)
                DM.NutrientLimitation = unusedDM * (DM.TotalFixationSupply/ totalSupply);
            }*/

        }
    }
}
