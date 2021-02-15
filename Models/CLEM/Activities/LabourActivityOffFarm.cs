using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Off farm labour activities
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages labour supplied and income derived from an off-farm task.")]
    [HelpUri(@"Content/Features/Activities/Labour/OffFarmWork.htm")]
    [Version(1, 0, 2, "Labour required and pricing  now implement in LabourRequirement component and LabourPricing from Resources used.")]
    [Version(1, 0, 1, "")]
    public class LabourActivityOffFarm: CLEMActivityBase, IValidatableObject
    {
        private FinanceType bankType { get; set; }
        private LabourRequirement labourRequired { get; set; }

        /// <summary>
        /// Bank account name to pay to
        /// </summary>
        [Description("Bank account to pay to")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(Finance) })]
        public string BankAccountName { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // locate resources
            bankType = Resources.GetResourceItem(this, BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as FinanceType;
            labourRequired = this.FindAllChildren<LabourRequirement>().FirstOrDefault() as LabourRequirement;
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            double daysNeeded = 0;
            // get fixed days per LabourRequirement
            if(labourRequired != null)
            {
                daysNeeded = labourRequired.LabourPerUnit;
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Off-farm", null);

        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            // days provided from labour set in the requests in the resourceResquestList
            // receive payment for labour if bank type exists
            if (bankType != null)
            {
                bankType.Add(ResourceRequestList.Sum(a => a.Value), this, "", "Off farm labour");
            }
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
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

        #region validation
        /// <summary>
        /// Validate this component before simulation
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (bankType == null && Resources.GetResourceGroupByType(typeof(Finance)) != null)
            {
                Summary.WriteWarning(this, "No bank account has been specified for [a=" + this.Name + "]. No funds will be earned!");
            }

            // get check labour required
            if (labourRequired == null)
            {
                string[] memberNames = new string[] { "Labour requirement" };
                results.Add(new ValidationResult(String.Format("[a={0}] requires a [r=LabourRequirement] component to set the labour needed.\r\nThis activity will be ignored without this component.", this.Name), memberNames));
            }
            else
            {
                // check labour required is using fixed type
                if (labourRequired.UnitType != LabourUnitType.Fixed)
                {
                    string[] memberNames = new string[] { "Labour requirement" };
                    results.Add(new ValidationResult(String.Format("The UnitType of the [r=LabourRequirement] in [a={0}] must be [Fixed] for this activity.", this.Name), memberNames));
                }
            }

            // check pricing
            if (!Resources.Labour().PricingAvailable)
            {
                string[] memberNames = new string[] { "Labour pricing" };
                results.Add(new ValidationResult(String.Format("[a={0}] requires a [r=LabourPricing] component to set the labour rates.\r\nThis activity will be ignored without this component.", this.Name), memberNames));
            }
            return results;
        } 
        #endregion

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
                htmlWriter.Write("\r\n<div class=\"activityentry\">Earn ");
                htmlWriter.Write("Earnings will be paid to ");
                if (BankAccountName == null || BankAccountName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">[ACCOUNT NOT SET]</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + BankAccountName + "</span>");
                }
                htmlWriter.Write(" based on <span class=\"resourcelink\">Labour Pricing</span> set in the <span class=\"resourcelink\">Labour</span>");
                htmlWriter.Write("</div>");

                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
