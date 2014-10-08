using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Organs;
using Models.PMF.Phen;

namespace Models.PMF
{
    /// <summary>
    /// A summeriser model
    /// </summary>
    [Serializable]
    public class Summariser : Model
    {
        /// <summary>The above ground</summary>
        [Link] Biomass AboveGround = null;

        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The leaf</summary>
        [Link(IsOptional = true)]
        Leaf Leaf = null;

        /// <summary>Called when [phase changed].</summary>
        /// <param name="PhaseChange">The phase change.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType PhaseChange)
        {
            string message = Phenology.CurrentPhase.Start + "\r\n";
            if (Leaf != null)
            {
                message += "  LAI = " + Leaf.LAI.ToString("f2") + " (m^2/m^2)" + "\r\n";
                message += "  Above Ground Biomass = " + AboveGround.Wt.ToString("f2") + " (g/m^2)" + "\r\n";
            }
            Summary.WriteMessage(this, message);
        }
    }

}