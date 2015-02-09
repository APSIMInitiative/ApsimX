using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.PMF.Organs
{
    /// <summary>
    /// A generic above ground organ
    /// </summary>
    [Serializable]
    public class GenericAboveGroundOrgan : GenericOrgan, AboveGround
    {
        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;

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
        /// <summary>Called when [cut].</summary>
        public override void OnCut()
        {
            Summary.WriteMessage(this, "Cutting");

            Live.Clear();
            Dead.Clear();
        }
        #endregion
    }
}
