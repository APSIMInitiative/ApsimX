using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;

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
    [Description("This activity performs the collection of manure from a specified paddock in the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Manure/CollectManurePaddock.htm")]
    public class ManureActivityCollectPaddock: CLEMActivityBase
    {
        private ProductStoreTypeManure manureStore;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            manureStore = Resources.GetResourceItem(this, typeof(ProductStore), "Manure", OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as ProductStoreTypeManure;
        }

        /// <summary>
        /// Name of paddock or pasture to collect from (blank is yards)
        /// </summary>
        [Description("Name of paddock (GrazeFoodStoreType) to collect from (blank is yards)")]
        [Required]
        public string GrazeFoodStoreTypeName { get; set; }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            double amountAvailable = 0;
            // determine wet weight to move
            if (manureStore != null)
            {
                ManureStoreUncollected msu = manureStore.UncollectedStores.Where(a => a.Name.ToLower() == GrazeFoodStoreTypeName.ToLower()).FirstOrDefault();
                if (msu != null)
                {
                    amountAvailable = msu.Pools.Sum(a => a.WetWeight(manureStore.MoistureDecayRate, manureStore.ProportionMoistureFresh));
                }
            }
            double daysNeeded = 0;
            double numberUnits = 0;
            switch (requirement.UnitType)
            {
                case LabourUnitType.perUnit:
                    numberUnits = amountAvailable / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Collect manure", manureStore.NameWithParent);
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            Status = ActivityStatus.Critical;
            // get all shortfalls
            double labourLimit = this.LabourLimitProportion;

            if (labourLimit == 1 || this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable)
            {
                manureStore.Collect(manureStore.Name, labourLimit, this);
                if (labourLimit == 1)
                {
                    SetStatusSuccess();
                }
                else
                {
                    this.Status = ActivityStatus.Partial;
                }
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMCollectManure")]
        private void OnCLEMCollectManure(object sender, EventArgs e)
        {
            if (manureStore != null)
            {
                // get resources
                GetResourcesRequiredForActivity();
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
            ResourceShortfallOccurred?.Invoke(this, e);
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
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "\r\n<div class=\"activityentry\">Collect manure from ";
            if (GrazeFoodStoreTypeName == null || GrazeFoodStoreTypeName == "")
            {
                html += "<span class=\"errorlink\">[PASTURE NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + GrazeFoodStoreTypeName + "</span>";
            }
            html += "</div>";

            return html;
        } 
        #endregion

    }
}
