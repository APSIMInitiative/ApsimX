using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;
using System;

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

            N.Allocated = MathUtilities.Sum(N.TotalAllocation);

            //To introduce functionality for other nutrients we need to repeat this for loop for each new nutrient type
            // Calculate posible growth based on Minimum N requirement of organs
            for (int i = 0; i < Organs.Length; i++)
            {
                double TotalNDemand = N.StructuralDemand[i] + N.MetabolicDemand[i] + N.StorageDemand[i];
                if (N.TotalAllocation[i] >= TotalNDemand)
                    N.ConstrainedGrowth[i] = 100000000; //given high value so where there is no N deficit in organ and N limitation to growth  
                else
                    if (N.TotalAllocation[i] == 0 | Organs[i].MinNconc == 0)
                    N.ConstrainedGrowth[i] = 0;
                else
                    N.ConstrainedGrowth[i] = N.TotalAllocation[i] / Organs[i].MinNconc;
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
        }
    }
}
