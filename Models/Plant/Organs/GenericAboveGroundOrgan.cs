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
        /// <summary>Called when crop is being cut.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Cutting")]
        private void OnCutting(object sender, EventArgs e)
        {
            if (sender == Plant)
            {
                Summary.WriteMessage(this, "Cutting");

                Live.Clear();
                Dead.Clear();
            }
        }
        #endregion
    }
}
