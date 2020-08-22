using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;
using System;

namespace Models.PMF.Arbitrator
{
    /// <summary>Allocates the N Uptakes.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class AllocateUptakesMethod : Model, IPartitionMethod
    {
        /// <summary>Allocates the N Supply to the different organs.</summary>
        public void Calculate(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrationMethod)
        {
            double BiomassTakenUp = 0;
            if (BAT.TotalUptakeSupply > 0.00000000001)
            {
                arbitrationMethod.DoAllocation(Organs, BAT.TotalUptakeSupply, ref BiomassTakenUp, BAT);
                // Then calculate how much N is taken up by each supplying organ based on relative uptake supply
                for (int i = 0; i < Organs.Length; i++)
                    BAT.Uptake[i] += BiomassTakenUp * MathUtilities.Divide(BAT.UptakeSupply[i], BAT.TotalUptakeSupply, 0);
            }
        }
    }
}
