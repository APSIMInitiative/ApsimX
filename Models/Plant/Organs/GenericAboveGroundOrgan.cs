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
        /// <summary>Called when [prune].</summary>
        /// <param name="Prune">The prune.</param>
        [EventSubscribe("Prune")]
        private void OnPrune(PruneType Prune)
        {
            Summary.WriteMessage(this, "Pruning");

            Live.Clear();
            Dead.Clear();
        }

        #region Biomass Removal
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
        }
        /// <summary>
        /// The default proportions biomass to removeed from each organ on Pruning
        /// </summary>
        public override OrganBiomassRemovalType PruneDefault
        {
            get
            {
                return new OrganBiomassRemovalType
                {
                    FractionRemoved = 0,
                    FractionToResidue = 0.6
                };
            }
        }
        /// <summary>
        /// The default proportions biomass to removeed from each organ on Grazing
        /// </summary>
        public override OrganBiomassRemovalType GrazeDefault
        {
            get
            {
                return new OrganBiomassRemovalType
                {
                    FractionRemoved = 0,
                    FractionToResidue = 0.8
                };
            }
        }
        #endregion
    }
}
