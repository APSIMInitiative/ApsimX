using Models.Core;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using System;

namespace Models.PMF.Arbitrator
{
    /// <summary>Updates the Leaf Area after Potential DM ha sbeen allocated.</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassTypeArbitrator))]
    public class UpdateLeafAreaMethod : Model, IAllocationMethod
    {
        /// <summary>The method used to arbitrate N allocations</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        protected SorghumLeaf Leaf = null;


        /// <summary>Allocate the nutrient allocations.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="N">The organs.</param>
        public void Allocate(IArbitration[] Organs, BiomassArbitrationType N)
        {
            Leaf.UpdateArea();
        }
    }
}
