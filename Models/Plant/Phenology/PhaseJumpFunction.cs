using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Models.Core;
using Models.PMF.Functions;

namespace Models.PMF.Phen
{
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    class PhaseJumpFunction
    {
        [Link]
        Phenology Phenology = null;

        [Description("Start")]
        public string Start { get; set; }
        [Description("End")]
        public string End { get; set; }
        [Description("PhaseNameToJumpTo")]
        public string PhaseNameToJumpTo { get; set; }
        [Description("Event")]
        public string Event { get; set; }

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
