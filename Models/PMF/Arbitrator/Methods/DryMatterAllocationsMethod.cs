using Models.Core;
using Models.PMF.Interfaces;
using System;

namespace Models.PMF.Arbitrator
{
    /// <summary>Allocates the DM to the different organs.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class DryMatterAllocationsMethod : Model, IAllocationMethod
    {
        /// <summary>Sends the dm allocations.</summary>
        public void Allocate(IArbitration[] Organs, BiomassArbitrationType DM)
        {
            // Send DM allocations to all Plant Organs
            for (int i = 0; i < Organs.Length; i++)
                Organs[i].SetDryMatterAllocation(new BiomassAllocationType
                {
                    Respired = DM.Respiration[i],
                    Reallocation = DM.Reallocation[i],
                    Retranslocation = DM.Retranslocation[i],
                    Structural = DM.StructuralAllocation[i],
                    Storage = DM.StorageAllocation[i],
                    Metabolic = DM.MetabolicAllocation[i],
                });

            DM.End = 0;
            for (int i = 0; i < Organs.Length; i++)
                DM.End += Organs[i].Total.Wt;
            DM.BalanceError = (DM.End - (DM.Start + DM.TotalPlantSupply));
            if (DM.BalanceError > 0.0001)
                throw new Exception("DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than DM supplied by photosynthesis and DM remobilisation");
            DM.BalanceError = (DM.End - (DM.Start + DM.TotalStructuralDemand + DM.TotalMetabolicDemand + DM.TotalStorageDemand));
            if (DM.BalanceError > 0.0001)
                throw new Exception("DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than the sum of structural DM demand, metabolic DM demand and Storage DM capacity");
        }
    }
}
