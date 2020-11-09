using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;
using System;

namespace Models.PMF.Arbitrator
{

    /// <summary>Sends the potential dm allocations.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class SendPotentialDMAllocationsMethod : Model, IPartitionMethod
    {
        /// <summary>Does the fixation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="DM">The bat.</param>
        /// <param name="arbitrationMethod">The option.</param> not used 
        public void Calculate(IArbitration[] Organs, BiomassArbitrationType DM, IArbitrationMethod arbitrationMethod)
        {
            //  Allocate to meet Organs demands
            DM.Allocated = DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation + DM.TotalStorageAllocation;

            // Then check it all adds up
            if (MathUtilities.IsGreaterThan(DM.Allocated, DM.TotalPlantSupply))
                throw new Exception("Potential DM allocation by " + this.Name + " exceeds DM supply.   Thats not really possible so something has gone a miss");
            if (MathUtilities.IsGreaterThan(DM.Allocated, DM.TotalPlantDemand))
                throw new Exception("Potential DM allocation by " + this.Name + " exceeds DM Demand.   Thats not really possible so something has gone a miss");

            // Send potential DM allocation to organs to set this variable for calculating N demand
            for (int i = 0; i < Organs.Length; i++)
                Organs[i].SetDryMatterPotentialAllocation(new BiomassPoolType
                {
                    Structural = DM.StructuralAllocation[i],  //Need to seperate metabolic and structural allocations
                    Metabolic = DM.MetabolicAllocation[i],  //This wont do anything currently
                    Storage = DM.StorageAllocation[i], //Nor will this do anything
                });
        }
    }
}
