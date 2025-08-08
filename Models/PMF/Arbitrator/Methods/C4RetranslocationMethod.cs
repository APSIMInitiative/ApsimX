using System;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;
using Models.PMF.Phen;

namespace Models.PMF.Arbitrator
{
    /// <summary>The method used to Retranslocate Biomass Type</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class C4RetranslocationMethod : Model, IPartitionMethod, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }


        /// <summary>Determines Nutrient limitations to DM allocations</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        protected OrganArbitrator Arbitrator = null;

        private int leafIndex = 2;
        private int stemIndex = 4;

        /// <summary>Does the fixation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrationMethod">The option.</param>
        public void Calculate(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrationMethod)
        {
            if (MathUtilities.IsPositive(BAT.TotalRetranslocationSupply))
            {
                if (arbitrationMethod is SorghumArbitratorN)
                {
                    (arbitrationMethod as SorghumArbitratorN).DoRetranslocation(Organs, BAT, Arbitrator.DM);
                }
                else
                {
                    double BiomassRetranslocated = 0;
                    if (MathUtilities.IsPositive(BAT.TotalRetranslocationSupply))
                    {
                        var phenology = Structure.Find<Phenology>();
                        if (phenology.Beyond("EndGrainFill"))
                            return;
                        arbitrationMethod.DoAllocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);

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

        /// <summary>Called when crop is sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        virtual protected void OnPlantSowing(object sender, SowingParameters data)
        {
            var organNames = Arbitrator.OrganNames;
            leafIndex = organNames.IndexOf("Leaf");
            stemIndex = organNames.IndexOf("Stem");
        }
    }
}
