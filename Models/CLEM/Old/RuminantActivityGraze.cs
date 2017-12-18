using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
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
    public class RuminantActivityGraze : CLEMActivityBase
    {
        [Link]
        private Activities.ActivitiesHolder Activities = null;

        /// <summary>
        /// Number of hours grazed
        /// Based on 8 hour grazing days
        /// Could be modified to account for rain/heat walking to water etc.
        /// </summary>
        [Description("Number of hours grazed")]
        public double HoursGrazed { get; set; }

        /// <summary>
        /// Name of paddock or pasture to graze
        /// </summary>
        [Description("Name of manage paddock activity")]
        public string PaddockName { get; set; }

        private PastureActivityManage paddockActivity { get; set; }
        private GrazeFoodStoreType grazeType { get; set; }

        /// <summary>
        /// Feed type (not used here)
        /// </summary>
        [XmlIgnore]
        public IFeedType FeedType { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // get paddock
            paddockActivity = Activities.SearchForNameInActivities(PaddockName) as PastureActivityManage;

            grazeType = paddockActivity.LinkedNativeFoodType;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            ResourceRequestList = null;
            double kgPerHa = grazeType.Amount / paddockActivity.Area;

            RuminantHerd ruminantHerd = Resources.RuminantHerd();
            List<Ruminant> herd = ruminantHerd.Herd.Where(a => a.Location == PaddockName).ToList();

            if (herd.Count() > 0)
            {
                double amount = 0;
                // get list of all Ruminants in this paddock
                foreach (Ruminant ind in herd)
                {
                    // Reduce potential intake based on pasture quality for the proportion consumed.

                    // TODO: build in pasture quality intake correction

                    // calculate intake from potential modified by pasture availability and hours grazed
                    amount += ind.PotentialIntake * (1 - Math.Exp(-ind.BreedParams.IntakeCoefficientBiomass * kgPerHa)) * (HoursGrazed / 8);
                }
                if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
                ResourceRequestList.Add(new ResourceRequest()
                {
                    AllowTransmutation = true,
                    Required = amount,
                    ResourceType = typeof(GrazeFoodStore),
                    ResourceTypeName = this.grazeType.Name,
                    ActivityModel = this
                }
                );
            }
            return ResourceRequestList;
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
    }

}
