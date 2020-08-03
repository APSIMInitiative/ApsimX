using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF;
using Models.PMF.Arbitrator;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using System;

namespace Models.PMF.Arbitrator
{
    /// <summary>The method used to Retranslocate Biomass Type</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class C4RetranslocationMethod : Model, IPartitionMethod
    {
        /// <summary>Determines Nutrient limitations to DM allocations</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        protected IArbitrator Arbitrator = null;

        /// <summary>Does the fixation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrationMethod">The option.</param>
        public void Calculate(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrationMethod)
        {
            if (MathUtilities.IsPositive(BAT.TotalRetranslocationSupply))
            {
                var nArbitrator = arbitrationMethod as SorghumArbitratorN;
                if (nArbitrator != null)
                {
                    nArbitrator.DoRetranslocation(Organs, BAT, Arbitrator.DM);
                }
                else
                {
                    double BiomassRetranslocated = 0;
                    if (MathUtilities.IsPositive(BAT.TotalRetranslocationSupply))
                    {
                        var phenology = Apsim.Find(this, typeof(Phenology)) as Phenology;
                        if (phenology.Beyond("EndGrainFill"))
                            return;
                        arbitrationMethod.DoAllocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);

                        int leafIndex = 2;
                        int stemIndex = 4;

                        double grainDifferential = BiomassRetranslocated;

                        if (grainDifferential > 0)
                        {
                            // Retranslocate from stem.
                            double stemWtAvail = BAT.RetranslocationSupply[stemIndex];
                            double stemRetrans = Math.Min(grainDifferential, stemWtAvail);
                            BAT.Retranslocation[stemIndex] += stemRetrans;
                            grainDifferential -= stemRetrans;

                            double leafWtAvail = BAT.RetranslocationSupply[leafIndex];
                            double leafRetrans = Math.Min(grainDifferential, leafWtAvail);
                            BAT.Retranslocation[leafIndex] += Math.Min(grainDifferential, leafWtAvail);
                            grainDifferential -= leafRetrans;
                        }
                    }
                }
            }
        }
    }
}
