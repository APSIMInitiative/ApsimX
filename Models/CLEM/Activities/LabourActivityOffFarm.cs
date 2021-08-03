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
    [ViewName("UserInterface.Views.PropertyView")]
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
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance) } })]
        public string BankAccountName { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityOffFarm()
        {
            TransactionCategory = "Income";
        }

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

        /// <inheritdoc/>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            double daysNeeded = 0;
            // get fixed days per LabourRequirement
            if(labourRequired != null)
            {
                daysNeeded = labourRequired.LabourPerUnit;
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, TransactionCategory, null);

        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            // days provided from labour set in the requests in the resourceResquestList
            // receive payment for labour if bank type exists
            if (bankType != null)
            {
                bankType.Add(ResourceRequestList.Sum(a => a.Value), this, "", TransactionCategory);
            }
        }

        /// <inheritdoc/>
        public override void AdjustResourcesNeededForActivity()
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

        /// <inheritdoc/>
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
