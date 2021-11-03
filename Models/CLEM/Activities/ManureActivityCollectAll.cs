using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant manure collection activity</summary>
    /// <summary>This activity performs the collection of all manure</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Undertake the collection of manure from all paddocks and yards in the simulation")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Manure/CollectManureAll.htm")]
    public class ManureActivityCollectAll : CLEMActivityBase
    {
        private ProductStoreTypeManure manureStore;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // activity is performed in CLEMCollectManure not CLEMGetResources
            this.AllocationStyle = ResourceAllocationStyle.Manual;

            manureStore = Resources.FindResourceType<ProductStore, ProductStoreTypeManure>(this, "Manure", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ManureActivityCollectAll()
        {
            TransactionCategory = "Manure";
        }

        /// <inheritdoc/>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            double amountAvailable = 0;
            // determine wet weight to move
            foreach (ManureStoreUncollected msu in manureStore.UncollectedStores)
                amountAvailable = msu.Pools.Sum(a => a.WetWeight(manureStore.MoistureDecayRate, manureStore.ProportionMoistureFresh));

            double daysNeeded = 0;
            switch (requirement.UnitType)
            {
                case LabourUnitType.perUnit:
                    daysNeeded = requirement.LabourPerUnit * (amountAvailable / requirement.UnitSize);
                    break;
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, TransactionCategory, manureStore.NameWithParent);
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            Status = ActivityStatus.Critical;
            // get all shortfalls
            double labourLimit = this.LabourLimitProportion;

            if (labourLimit == 1 || this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable)
            {
                foreach (ManureStoreUncollected msu in manureStore.UncollectedStores)
                {
                    manureStore.Collect(msu.Name, labourLimit, this);
                    if (labourLimit == 1)
                    {
                        this.Status = ActivityStatus.Success;
                    }
                    else
                    {
                        this.Status = ActivityStatus.Partial;
                    }
                }
            }
        }

        /// <summary>An event handler to allow us to perform manure collection</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMCollectManure")]
        private void OnCLEMCollectManure(object sender, EventArgs e)
        {
            if (manureStore != null)
                // get resources
                GetResourcesRequiredForActivity();
        }

        ///<inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            return "\r\n<div class=\"activityentry\">Collect manure from all pasture</div>";
        }

    }
}
