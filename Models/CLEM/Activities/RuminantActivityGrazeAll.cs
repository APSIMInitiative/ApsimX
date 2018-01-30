using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant graze activity</summary>
    /// <summary>This activity determines how a ruminant group will graze</summary>
    /// <summary>It is designed to request food via a food store arbitrator</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs grazing of all herds and pastures (paddocks) in the simulation.")]
    public class RuminantActivityGrazeAll : CLEMActivityBase
    {
        [Link]
        private Clock Clock = null;

        /// <summary>
        /// Number of hours grazed
        /// Based on 8 hour grazing days
        /// Could be modified to account for rain/heat walking to water etc.
        /// </summary>
        [Description("Number of hours grazed")]
        [Required, Range(0, 8, ErrorMessage = "Value based on maximum 8 hour grazing day")]
        public double HoursGrazed { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // create activity for each pasture type and breed at startup
            foreach (GrazeFoodStoreType pastureType in Resources.GrazeFoodStore().Children)
            {
                RuminantActivityGrazePasture ragp = new RuminantActivityGrazePasture();
                ragp.GrazeFoodStoreModel = pastureType;
                ragp.Clock = Clock;
                ragp.Parent = this;
                ragp.Name = "Graze_" + pastureType.Name;
                ragp.OnPartialResourcesAvailableAction = this.OnPartialResourcesAvailableAction;
                ragp.ActivityPerformed += BubblePaddock_ActivityPerformed;

                foreach (RuminantType herdType in Resources.RuminantHerd().Children)
                {
                    RuminantActivityGrazePastureHerd ragpb = new RuminantActivityGrazePastureHerd
                    {
                        GrazeFoodStoreModel = pastureType,
                        RuminantTypeModel = herdType,
                        HoursGrazed = HoursGrazed,
                        Parent = ragp,
                        Name = ragp.Name+"_"+herdType.Name,
                        OnPartialResourcesAvailableAction = this.OnPartialResourcesAvailableAction
                    };
                    if (ragpb.Resources == null)
                    {
                        ragpb.Resources = this.Resources;
                    }
                    if (ragpb.Clock == null)
                    {
                        ragpb.Clock = this.Clock;
                    }
                    ragpb.InitialiseHerd(true, true);
                    if (ragp.ActivityList == null)
                    {
                        ragp.ActivityList = new List<CLEMActivityBase>();
                    }
                    ragp.ActivityList.Add(ragpb);
                    ragpb.ResourceShortfallOccurred += GrazeAll_ResourceShortfallOccurred;
                    ragpb.ActivityPerformed += BubblePaddock_ActivityPerformed;
                }
                if (ActivityList == null)
                {
                    ActivityList = new List<CLEMActivityBase>();
                }
                ActivityList.Add(ragp);

            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (ActivityList != null)
            {
                foreach (RuminantActivityGrazePasture pastureGraze in ActivityList)
                {
                    foreach (RuminantActivityGrazePastureHerd pastureHerd in pastureGraze.ActivityList)
                    {
                        pastureHerd.ResourceShortfallOccurred -= GrazeAll_ResourceShortfallOccurred;
                        pastureHerd.ActivityPerformed -= BubblePaddock_ActivityPerformed;
                    }
                }
            }
        }

        private void GrazeAll_ResourceShortfallOccurred(object sender, EventArgs e)
        {
            // bubble shortfall to Activity base for reporting
            OnShortfallOccurred(e);
        }

        private void BubblePaddock_ActivityPerformed(object sender, EventArgs e)
        {
            if (ActivityPerformed != null)
                ActivityPerformed(sender, e);
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return;
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }


    }
}
