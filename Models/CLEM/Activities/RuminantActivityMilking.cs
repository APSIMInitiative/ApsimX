using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Activity to undertake milking of particular herd</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs milking based upon the current herd filtering.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantMilking.htm")]
    public class RuminantActivityMilking: CLEMRuminantActivityBase
    {
        private object milkStore;

        /// <summary>
        /// Resource type to store milk in
        /// </summary>
        [Description("Store to place milk")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(ProductStore) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of milk store required")]
        public string ResourceTypeName { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(true, true);

            // find milk store
            milkStore = Resources.GetResourceItem(this, ResourceTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <summary>An event handler to call for all herd management activities</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalMilkProduction")]
        private void OnCLEMMilkProduction(object sender, EventArgs e)
        {
            // this method will ensure the milking status is defined for females after births when lactation is set and before milk production is determined
            foreach (RuminantFemale item in this.CurrentHerd(true).Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().Where(a => a.IsLactating == true).ToList())
            {
                // set these females to state milking performed so they switch to the non-suckling milk production curves.
                item.MilkingPerformed = true;
            }
        }

        /// <summary>An event handler to call for all herd management activities</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalMilking")]
        private void OnCLEMMilking(object sender, EventArgs e)
        {
            // take all milk
            List<RuminantFemale> herd = this.CurrentHerd(true).Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().Where(a => a.IsLactating == true).ToList();
            double milkTotal = herd.Sum(a => a.MilkCurrentlyAvailable);
            if (milkTotal > 0)
            {
                double labourLimit = this.LabourLimitProportion;
                // only provide what labour would allow
                (milkStore as IResourceType).Add(milkTotal * labourLimit, this, this.PredictedHerdName, "Milking");

                // record milk taken with female for accounting
                foreach (RuminantFemale female in herd)
                {
                    female.TakeMilk(female.MilkCurrentlyAvailable * labourLimit, MilkUseReason.Milked);
                    this.Status = ActivityStatus.Success;
                }
            }
            else
            {
                this.Status = ActivityStatus.NotNeeded;
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
        /// Determine the labour required for this activity based on LabourRequired items in tree
        /// </summary>
        /// <param name="requirement">Labour requirement model</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            List<RuminantFemale> herd = this.CurrentHerd(true).Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().Where(a => a.IsLactating == true & a.SucklingOffspringList.Count() == 0).ToList();
            int head = herd.Count();
            double daysNeeded = 0;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    double numberUnits = head / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Milking", this.PredictedHerdName);
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
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Milk is placed in ");

                if (ResourceTypeName == null || ResourceTypeName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">[NOT SET]</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + ResourceTypeName + "</span>");
                }
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
