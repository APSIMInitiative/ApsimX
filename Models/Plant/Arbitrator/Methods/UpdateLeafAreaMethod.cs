using Models.Core;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using System;

namespace Models.PMF.Arbitrator
{
    /// <summary>Updates the Leaf Area after Potential DM ha sbeen allocated.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class UpdateLeafAreaMethod : Model, IPartitionMethod
    {
        /// <summary>The method used to arbitrate N allocations</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        protected SorghumLeaf Leaf = null;


        /// <summary>Allocate the nutrient allocations.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="N">The biomass arbitration type.</param>
        /// <param name="method">The arbitration method.</param>
        public void Calculate(IArbitration[] Organs, BiomassArbitrationType N, IArbitrationMethod method)
        {
            Leaf.UpdateArea();
        }
    }
}
