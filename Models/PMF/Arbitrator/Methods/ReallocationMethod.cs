using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;
using System;

namespace Models.PMF.Arbitrator
{
    /// <summary>Function called at OnDoPotentialPlantPartioning.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class ReallocationMethod : Model, IPartitionMethod
    {
        /// <summary>Reallocate the Biomass Type.</summary>
        public void Calculate(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrationMethod)
        {
            double BiomassReallocated = 0;
            if (BAT.TotalReallocationSupply > 0.00000000001)
            {
                arbitrationMethod.DoAllocation(Organs, BAT.TotalReallocationSupply, ref BiomassReallocated, BAT);

                //Then calculate how much biomass is realloced from each supplying organ based on relative reallocation supply
                for (int i = 0; i < Organs.Length; i++)
                    if (BAT.ReallocationSupply[i] > 0)
                    {
                        double RelativeSupply = BAT.ReallocationSupply[i] / BAT.TotalReallocationSupply;
                        BAT.Reallocation[i] += BiomassReallocated * RelativeSupply;
                    }
                BAT.TotalReallocation = MathUtilities.Sum(BAT.Reallocation);
            }
        }
    }
}
