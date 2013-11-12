using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.Plant.Phen
{
    class PhaseJumpFunction
    {
        [Link]
        Phenology Phenology = null;

        public string Start = "";
        public string End = "";
        public string PhaseNameToJumpTo = "";
        public string Event = "";

        [EventSubscribe("Initialised")]
        private void OnInitialised(object sender, EventArgs e)
        {
            //Fixme, make this work again
            //MyPaddock.Subscribe(Event, OnEvent);
        }

        public void OnEvent()
        {
            if (Phenology.Between(Start, End))
            {
                Phenology.CurrentPhaseName = PhaseNameToJumpTo;
            }
        }
    }
}
