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
    /// <summary>The method used to Retranslocate</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class RetranslocationMethod : Model, IPartitionMethod
    {
        /// <summary>Does the fixation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrationMethod">The option.</param>
        public void Calculate(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrationMethod)
        {
            double BiomassRetranslocated = 0;
            if (BAT.TotalRetranslocationSupply > 0.00000000001)
            {
                arbitrationMethod.DoAllocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);
                // Then calculate how much N (and associated biomass) is retranslocated from each supplying organ based on relative retranslocation supply
                for (int i = 0; i < Organs.Length; i++)
                    if (BAT.RetranslocationSupply[i] > 0.00000000001)
                    {
                        double RelativeSupply = BAT.RetranslocationSupply[i] / BAT.TotalRetranslocationSupply;
                        BAT.Retranslocation[i] += BiomassRetranslocated * RelativeSupply;
                    }
            }
        }
    }
}
