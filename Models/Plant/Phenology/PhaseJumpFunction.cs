using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;

namespace Models.PMF.Phen
{
    [Serializable]
    class PhaseJumpFunction
    {
        [Link]
        Phenology Phenology = null;

        public string Start = "";
        public string End = "";
        public string PhaseNameToJumpTo = "";
        public string Event = "";

        public void OnCommencing()
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
