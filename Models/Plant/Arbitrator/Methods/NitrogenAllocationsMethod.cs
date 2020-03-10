using Models.Core;
using Models.PMF;
using Models.PMF.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF.Arbitrator
{
    /// <summary>Sends the dm allocations.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class NitrogenAllocationsMethod : Model, IAllocationMethod
    {
        /// <summary>The method used to arbitrate N allocations</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        protected IArbitrator Arbitrator = null;


        /// <summary>Sends the nutrient allocations.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="N">The organs.</param>
        public void Allocate(IArbitration[] Organs, BiomassArbitrationType N)
        {
            // Send N allocations to all Plant Organs
            for (int i = 0; i < Organs.Length; i++)
            {
                if ((N.StructuralAllocation[i] < -0.00000001) || (N.MetabolicAllocation[i] < -0.00000001) || (N.StorageAllocation[i] < -0.00000001))
                    throw new Exception("-ve N Allocation");
                if (N.StructuralAllocation[i] < 0.0)
                    N.StructuralAllocation[i] = 0.0;
                if (N.MetabolicAllocation[i] < 0.0)
                    N.MetabolicAllocation[i] = 0.0;
                if (N.StorageAllocation[i] < 0.0)
                    N.StorageAllocation[i] = 0.0;
                Organs[i].SetNitrogenAllocation(new BiomassAllocationType
                {
                    Structural = N.StructuralAllocation[i], //This needs to be seperated into components
                    Metabolic = N.MetabolicAllocation[i],
                    Storage = N.StorageAllocation[i],
                    Fixation = N.Fixation[i],
                    Reallocation = N.Reallocation[i],
                    Retranslocation = N.Retranslocation[i],
                    Uptake = N.Uptake[i]
                });
            }

            //Finally Check Mass balance adds up
            N.End = 0;
            for (int i = 0; i < Organs.Length; i++)
                N.End += Organs[i].Total.N;
            N.BalanceError = (N.End - (N.Start + N.TotalPlantSupply));
            if (N.BalanceError > 0.05)
                throw new Exception("N Mass balance violated!!!!.  Daily Plant N increment is greater than N supply");
            N.BalanceError = (N.End - (N.Start + N.TotalPlantDemand));
            if (N.BalanceError > 0.001)
                throw new Exception("N Mass balance violated!!!!  Daily Plant N increment is greater than N demand");
            
            //var DM = Arbitrator.DM;
            //DM.End = 0;
            //for (int i = 0; i < Organs.Length; i++)
            //    DM.End += Organs[i].Total.Wt;
            //DM.BalanceError = (DM.End - (DM.Start + DM.TotalPlantSupply));
            //if (DM.BalanceError > 0.0001)
            //    throw new Exception("DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than DM supplied by photosynthesis and DM remobilisation");
            //DM.BalanceError = (DM.End - (DM.Start + DM.TotalStructuralDemand + DM.TotalMetabolicDemand + DM.TotalStorageDemand));
            //if (DM.BalanceError > 0.0001)
            //    throw new Exception("DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than the sum of structural DM demand, metabolic DM demand and Storage DM capacity");

        }
    }
}
