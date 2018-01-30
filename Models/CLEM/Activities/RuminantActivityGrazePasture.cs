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
    [Description("This activity performs grazing of all herds within a specified pasture (paddock) in the simulation.")]
    public class RuminantActivityGrazePasture : CLEMRuminantActivityBase
    {
        /// <summary>
        /// Link to clock
        /// Public so children can be dynamically created after links defined
        /// </summary>
        [Link]
        public Clock Clock = null;

        /// <summary>
        /// Number of hours grazed
        /// Based on 8 hour grazing days
        /// Could be modified to account for rain/heat walking to water etc.
        /// </summary>
        [Description("Number of hours grazed (based on 8 hr grazing day)")]
        [Required, Range(0, 8, ErrorMessage = "Value based on maximum 8 hour grazing day")]
        public double HoursGrazed { get; set; }

        /// <summary>
        /// Name of paddock or pasture to graze
        /// </summary>
        [Description("Name of GrazeFoodStoreType to graze")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of Graze Food Store required")]
        public string GrazeFoodStoreTypeName { get; set; }

        /// <summary>
        /// paddock or pasture to graze
        /// </summary>
        [XmlIgnore]
        public GrazeFoodStoreType GrazeFoodStoreModel { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // This method will only fire if the user has added this activity to the UI
            // Otherwise all details will be provided from GrazeAll code [CLEMInitialiseActivity]

            GrazeFoodStoreModel = Resources.GetResourceItem(this, typeof(GrazeFoodStore), GrazeFoodStoreTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;

            //Create list of children by breed
            foreach (RuminantType herdType in Resources.RuminantHerd().Children)
            {
                RuminantActivityGrazePastureHerd ragpb = new RuminantActivityGrazePastureHerd
                {
                    GrazeFoodStoreModel = GrazeFoodStoreModel,
                    RuminantTypeModel = herdType,
                    Parent = this,
                    Clock = this.Clock,
                    Name = "Graze_" + GrazeFoodStoreModel.Name + "_" + herdType.Name
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
                if (ActivityList == null)
                {
                    ActivityList = new List<CLEMActivityBase>();
                }
                ActivityList.Add(ragpb);
                ragpb.ResourceShortfallOccurred += Paddock_ResourceShortfallOccurred;
                ragpb.ActivityPerformed += BubbleHerd_ActivityPerformed;
            }
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            // This method does not take any resources but is used to arbitrate resources for all breed grazing activities it contains

            // determine pasture quality from all pools (DMD) at start of grazing
            double pastureDMD = GrazeFoodStoreModel.DMD;

            // Reduce potential intake based on pasture quality for the proportion consumed (zero legume).
            // TODO: check that this doesn't need to be performed for each breed based on how pasture taken
            // NABSA uses Diet_DMD, but we cant adjust Potential using diet before anything consumed.
            double potentialIntakeLimiter = 1.0;
            if ((0.8 - GrazeFoodStoreModel.IntakeTropicalQualityCoefficient - pastureDMD / 100) >= 0)
            {
                potentialIntakeLimiter = 1 - GrazeFoodStoreModel.IntakeQualityCoefficient * (0.8 - GrazeFoodStoreModel.IntakeTropicalQualityCoefficient - pastureDMD / 100);
            }

            // check nested graze breed requirements for this pasture
            double totalNeeded = 0;
            foreach (RuminantActivityGrazePastureHerd item in ActivityList)
            {
                item.ResourceRequestList = null;
                item.PotentialIntakePastureQualityLimiter = potentialIntakeLimiter;
                item.GetResourcesNeededForActivity();
                if (item.ResourceRequestList != null && item.ResourceRequestList.Count > 0)
                {
                    totalNeeded += item.ResourceRequestList[0].Required;
                }
            }

            // Check available resources
            // This determines the proportional amount available for competing breeds with different green diet proportions
            // It does not truly account for how the pasture is provided from pools but will suffice unless more detailed model developed
            double available = GrazeFoodStoreModel.Amount;
            double limit = 0;
            if(totalNeeded>0)
            {
                limit = Math.Min(1.0, available / totalNeeded);
            }

            // apply limits to children
            foreach (RuminantActivityGrazePastureHerd item in ActivityList)
            {
                item.SetupPoolsAndLimits(limit);
            }
            return null;
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (ActivityList != null)
            {
                foreach (RuminantActivityGrazePastureHerd pastureHerd in ActivityList)
                {
                    pastureHerd.ResourceShortfallOccurred -= Paddock_ResourceShortfallOccurred;
                    pastureHerd.ActivityPerformed -= BubbleHerd_ActivityPerformed;
                }
            }
        }

        private void Paddock_ResourceShortfallOccurred(object sender, EventArgs e)
        {
            // bubble shortfall to Activity base for reporting
            OnShortfallOccurred(e);
        }

        private void BubbleHerd_ActivityPerformed(object sender, EventArgs e)
        {
            if (ActivityPerformed != null)
                ActivityPerformed(sender, e);
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
