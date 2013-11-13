using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Plant.Organs;
using Models.Plant.Phen;

namespace Models.Plant
{
    public class Summariser : Model
    {
        public Biomass AboveGround { get; set; }
        public Biomass BelowGround { get; set; }
        public Biomass Total { get; set; }


        [Link]
        Phenology Phenology = null;

        [Link(IsOptional = true)]
        Leaf Leaf = null;

        [Link]
        Clock Clock = null;

        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType PhaseChange)
        {
            Console.WriteLine(Clock.Today.ToString("d MMMM yyyy") + " - " + Phenology.CurrentPhase.Start);
            if (Leaf != null)
            {
                Console.WriteLine("                            LAI = " + Leaf.LAI.ToString("f2") + " (m^2/m^2)");
                Console.WriteLine("           Above Ground Biomass = " + AboveGround.Wt.ToString("f2") + " (g/m^2)");
            }
        }
    }

}