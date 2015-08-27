using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.PMF.Organs
{
    /// <summary>
    /// This is a generic above ground organ which has DM and Biomass pools.  
    /// </summary>
    [Serializable]
    public class GenericAboveGroundOrgan : GenericOrgan, AboveGround
    {
        #region Event handlers
        /// <summary>Called when [prune].</summary>
        /// <param name="Prune">The prune.</param>
        [EventSubscribe("Prune")]
        private void OnPrune(PruneType Prune)
        {
            Summary.WriteMessage(this, "Pruning");

            Live.Clear();
            Dead.Clear();
        }

        /// <summary>
        /// The default proportions biomass to removeed from each organ on harvest.
        /// </summary>
        public override OrganBiomassRemovalType HarvestDefault
        {
            get
            {
                return new OrganBiomassRemovalType
                {
                    FractionRemoved = 0,
                    FractionToResidue = 0
                };
            }
            set { }
        }

        /// <summary>
        /// The default proportions biomass to removeed from each organ on Cutting
        /// </summary>
        public override OrganBiomassRemovalType CutDefault
        {
            get
            {
                return new OrganBiomassRemovalType
                {
                    FractionRemoved = 0.8,
                    FractionToResidue = 0
                };
            }
            set { }
        }
        #endregion
    }
}
