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
    [ViewName("UserInterface.Views.PropertyView")]
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
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(ProductStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of milk store required")]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public RuminantActivityMilking()
        {
            TransactionCategory = "Livestock.Milking";
        }

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
            foreach (RuminantFemale item in this.CurrentHerd(true).OfType<RuminantFemale>().Where(a => a.IsLactating))
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
            IEnumerable<RuminantFemale> herd = this.CurrentHerd(true).OfType<RuminantFemale>().Where(a => a.IsLactating);
            double milkTotal = herd.Sum(a => a.MilkCurrentlyAvailable);
            if (milkTotal > 0)
            {
                double labourLimit = this.LabourLimitProportion;
                // only provide what labour would allow
                (milkStore as IResourceType).Add(milkTotal * labourLimit, this, this.PredictedHerdName, TransactionCategory);

                // record milk taken with female for accounting
                foreach (RuminantFemale female in herd)
                {
                    female.TakeMilk(female.MilkCurrentlyAvailable * labourLimit, MilkUseReason.Milked);
                    this.Status = (labourLimit < 1)?ActivityStatus.Partial:ActivityStatus.Success;
                }
            }
            else
            {
                this.Status = ActivityStatus.NotNeeded;
            }

        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <inheritdoc/>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            IEnumerable<RuminantFemale> herd = this.CurrentHerd(true).OfType<RuminantFemale>().Where(a => a.IsLactating & a.SucklingOffspringList.Count() == 0);
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
                        numberUnits = Math.Ceiling(numberUnits);

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, TransactionCategory, this.PredictedHerdName);
        }

        /// <inheritdoc/>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            return;
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <inheritdoc/>
        public override event EventHandler ResourceShortfallOccurred;

        /// <inheritdoc/>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <inheritdoc/>
        public override event EventHandler ActivityPerformed;

        /// <inheritdoc/>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary

        /// <inheritdoc/>
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
