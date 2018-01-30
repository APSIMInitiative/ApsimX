using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant manure collection activity</summary>
    /// <summary>This activity performs the collection of all manure</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs the collection of manure from all paddocks and yards in the simulation.")]
    public class RuminantActivityCollectManureAll : CLEMActivityBase
    {
        /// <summary>
        /// Labour settings
        /// </summary>
        private List<LabourFilterGroupSpecified> labour { get; set; }

        private ProductStoreTypeManure manureStore;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            manureStore = Resources.GetResourceItem(this, typeof(ProductStore), "Manure", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as ProductStoreTypeManure;

            // get labour specifications
            labour = Apsim.Children(this, typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
            if (labour == null) labour = new List<LabourFilterGroupSpecified>();
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        private List<ResourceRequest> GetResourcesNeededForActivityLocal()
        {
            ResourceRequestList = null;
            double amountAvailable = 0;
            // determine wet weight to move
            foreach (ManureStoreUncollected msu in manureStore.UncollectedStores)
            {
                amountAvailable = msu.Pools.Sum(a => a.WetWeight(manureStore.MoistureDecayRate, manureStore.ProportionMoistureFresh));
            }
            // determine labour required
            if (amountAvailable > 0)
            {
                // for each labour item specified
                foreach (var item in labour)
                {
                    double daysNeeded = 0;
                    switch (item.UnitType)
                    {
                        case LabourUnitType.perKg:
                            daysNeeded = item.LabourPerUnit * (amountAvailable/ item.UnitSize);
                            break;
                        default:
                            throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", item.UnitType, item.Name, this.Name));
                    }
                    if (daysNeeded > 0)
                    {
                        if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
                        ResourceRequestList.Add(new ResourceRequest()
                        {
                            AllowTransmutation = false,
                            Required = daysNeeded,
                            ResourceType = typeof(Labour),
                            ResourceTypeName = "",
                            ActivityModel = this,
                            Reason = "Manure collection",
                            FilterDetails = new List<object>() { item }
                        }
                        );
                    }
                }

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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMCollectManure")]
        private void OnCLEMCollectManure(object sender, EventArgs e)
        {
            Status = ActivityStatus.Critical;
            // is manure in resources
            if (manureStore != null)
            {
                if (this.TimingOK)
                {
                    List<ResourceRequest> resourcesneeded = GetResourcesNeededForActivityLocal();
                    bool tookRequestedResources = TakeResources(resourcesneeded, true);
                    // get all shortfalls
                    double labourNeeded = 0;
                    double labourLimit = 1;
                    if (tookRequestedResources & (ResourceRequestList != null))
                    {
                        labourNeeded = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Required);
                        double labourProvided = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Provided);
                        labourLimit = labourProvided / labourNeeded;
                    }

                    if (labourLimit == 1 || this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable)
                    {
                        foreach (ManureStoreUncollected msu in manureStore.UncollectedStores)
                        {
                            manureStore.Collect(msu.Name, labourLimit, this.Name);
                            SetStatusSuccess();
                        }
                    }
                }
            }
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
