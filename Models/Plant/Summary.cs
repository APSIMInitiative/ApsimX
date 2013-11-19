using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Organs;
using Models.PMF.Phen;

namespace Models.PMF
{
    public class Summariser : Model
    {
        public Biomass AboveGround { get; set; }
        public Biomass BelowGround { get; set; }
        public Biomass Total { get; set; }

        [Link]
        Summary Summary = null;
        [Link]
        Phenology Phenology = null;

        [Link(IsOptional = true)]
        Leaf Leaf = null;

        [Link]
        Clock Clock = null;

        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType PhaseChange)
        {
            Summary.WriteMessage(FullPath, Clock.Today.ToString("d MMMM yyyy") + " - " + Phenology.CurrentPhase.Start);
            if (Leaf != null)
            {
                Summary.WriteMessage(FullPath, "                            LAI = " + Leaf.LAI.ToString("f2") + " (m^2/m^2)");
                Summary.WriteMessage(FullPath, "           Above Ground Biomass = " + AboveGround.Wt.ToString("f2") + " (g/m^2)");
            }
        }
    }

}