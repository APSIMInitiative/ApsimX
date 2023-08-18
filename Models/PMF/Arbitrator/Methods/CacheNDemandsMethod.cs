using Models.Core;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using System;

namespace Models.PMF.Arbitrator
{
    /// <summary>
    /// CacheNDemandsMethod - used for reporting NDemand
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class CacheNDemandsMethod : Model, IPartitionMethod
    {
        /// <summary>The method used to arbitrate N allocations</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        protected OrganArbitrator Arbitrator = null;

        /// <summary>
        /// Used for Reporting
        /// </summary>
        [Description("Total N Demand")]
        public double TotalNDemand { get; set; } = 0.0;

        //cache indexes on start of simulation
        private int grainIndex = 0;
        private int rootIndex = 1;
        private int leafIndex = 2;
        private int stemIndex = 4;

        /// <summary>
        /// Calculate Method
        /// </summary>
        /// <param name="Organs"></param>
        /// <param name="BAT"></param>
        /// <param name="ArbitrationMethod"></param>
        public void Calculate(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod ArbitrationMethod)
        {
            var N = Arbitrator.N;

            //C4NitrogenUptakeMethod is called 4 times which makes it difficult to report what is happening inside it

            var rootDemand = N.StructuralDemand[rootIndex] + N.MetabolicDemand[rootIndex];
            var stemDemand = /*N.StructuralDemand[stemIndex] + */N.MetabolicDemand[stemIndex];
            var leafDemand = N.MetabolicDemand[leafIndex];
            var grainDemand = N.StructuralDemand[grainIndex] + N.MetabolicDemand[grainIndex];
            //have to correct the leaf demand calculation
            var leaf = Organs[leafIndex] as SorghumLeaf;
            var leafAdjustment = leaf.CalculateClassicDemandDelta();

            //double NDemand = (N.TotalPlantDemand - N.TotalReallocation) / kgha2gsm * Plant.Zone.Area; //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
            //old sorghum uses g/m^2 - need to convert after it is used to calculate actual diffusion
            // leaf adjustment is not needed here because it is an adjustment for structural demand - we only look at metabolic here.

            // dh - In old sorghum, root only has one type of NDemand - it doesn't have a structural/metabolic division.
            // In new apsim, root only uses structural, metabolic is always 0. Therefore, we have to include root's structural
            // NDemand in this calculation.

            // dh - In old sorghum, totalDemand is metabolic demand for all organs. However in new apsim, grain has no metabolic
            // demand, so we must include its structural demand in this calculation.
            TotalNDemand = N.TotalMetabolicDemand + N.StructuralDemand[rootIndex] + N.StructuralDemand[grainIndex];
            TotalNDemand -= grainDemand;
            TotalNDemand = Math.Max(0, TotalNDemand); // to replicate calcNDemand in old sorghum 
        }

        /// <summary>Called when crop is sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        virtual protected void OnPlantSowing(object sender, SowingParameters data)
        {
            var organNames = Arbitrator.OrganNames;
            grainIndex = organNames.IndexOf("Grain");
            rootIndex = organNames.IndexOf("Root");
            leafIndex = organNames.IndexOf("Leaf");
            stemIndex = organNames.IndexOf("Stem");
        }

    }
}
