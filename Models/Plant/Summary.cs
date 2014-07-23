using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Organs;
using Models.PMF.Phen;

namespace Models.PMF
{
    [Serializable]
    public class Summariser : Model
    {
        [Link] Biomass AboveGround = null;
        [Link] Biomass BelowGround = null;
        [Link] Biomass Total       = null;
        [Link] Biomass TotalLive   = null;
        [Link] Biomass TotalDead   = null;

        [Link]
        ISummary Summary = null;
        [Link]
        Phenology Phenology = null;

        [Link(IsOptional = true)]
        Leaf Leaf = null;

        [Link]
        Clock Clock = null;

        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType PhaseChange)
        {
            string message = Phenology.CurrentPhase.Start + "\r\n";
            if (Leaf != null)
            {
                message += "  LAI = " + Leaf.LAI.ToString("f2") + " (m^2/m^2)" + "\r\n";
                message += "  Above Ground Biomass = " + AboveGround.Wt.ToString("f2") + " (g/m^2)" + "\r\n";
            }
            Summary.WriteMessage(FullPath, message);
        }
    }

}