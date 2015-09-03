using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using System.Xml.Serialization;
using Models.Interfaces;

namespace Models.PMF.Phen
{
    ///<summary>
    /// The switches phenology to a nominated stage on a nominated event
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PhaseSetFunction : Model
    {
        ///<summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;
        /// <summary>The plant</summary>
        [Link]
        protected Plant Plant = null;
        /// <summary>Start stage for period when phase set is allowed</summary>
        [Description("Stage when we Start Allowing Phase Set")]
        public string StartAllowingPhaseSet { get; set; }
        /// <summary>End stage for period when phase set is allowed</summary>
        [Description("Stage when we Stop Allowing Phase Set")]
        public string StopAllowingPhaseSet { get; set; }
        /// <summary>The phenology</summary>
        [Description("Phase Name To Set To when Event occurs")]
        public string PhaseNameToSetTo { get; set; }
        /// <summary>The Event that triggers a phase set</summary>
        [Description("Event which triggers phase set")]
        public string Event { get; set; }

        /// <summary>Called when crop is being cut.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Cutting")]
        private void OnCutting(object sender, EventArgs e)
        {
            if (sender == Plant)
                if (Event == "Cut")
                    Phenology.CurrentPhaseName = PhaseNameToSetTo;
        }
        /// <summary>Called when crop is being cut.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Grazing")]
        private void OnGrazing(object sender, EventArgs e)
        {
            if (sender == Plant)
                if (Event == "Graze")
                    Phenology.CurrentPhaseName = PhaseNameToSetTo;
        }
    }
}
