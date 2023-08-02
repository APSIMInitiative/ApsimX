using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Models.Core.Attributes;
using System.IO;
using Models.CLEM.Interfaces;

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
    [Description("Manages labour supplied and income derived from an off-farm task")]
    [HelpUri(@"Content/Features/Activities/Labour/OffFarmWork.htm")]
    [Version(1, 0, 2, "Labour required and pricing now implement in LabourRequirement component and LabourPricing from Resources used.")]
    [Version(1, 0, 1, "")]
    public class LabourActivityOffFarm: CLEMActivityBase, IValidatableObject, IHandlesActivityCompanionModels
    {
        private FinanceType bankType { get; set; }

        /// <summary>
        /// Bank account name to pay to
        /// </summary>
        [Description("Bank account to pay to")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance) } })]
        public string BankAccountName { get; set; }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() {
                            "fixed",
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            bankType = Resources.FindResourceType<Finance, FinanceType>(this, BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels.ToList())
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            // days provided from labour set in the requests in the resourceResquestList
            // receive payment for labour if bank type exists
            if (bankType != null)
            {
                bankType.Add(ResourceRequestList.Sum(a => a.Value), this, null, TransactionCategory);
                SetStatusSuccessOrPartial(ResourceRequestList.Where(a => a.ActivityModel is LabourRequirement).Where(a => a.Available < a.Provided).Any());
            }
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
            if (bankType == null && Resources.FindResource<Finance>() != null)
                Summary.WriteMessage(this, "No bank account has been specified for [a=" + this.Name + "]. No funds will be earned!", MessageType.Warning);

            var labour = FindAllChildren<LabourRequirement>();
            // get check labour required
            if (labour == null)
            {
                string[] memberNames = new string[] { "Labour requirement" };
                results.Add(new ValidationResult(String.Format("[a={0}] requires a [r=LabourRequirement] component to set the labour needed.\r\nThis activity will be ignored without this component.", this.Name), memberNames));
            }

            // check pricing
            if (!Resources.FindResourceGroup<Labour>().PricingAvailable)
            {
                string[] memberNames = new string[] { "Labour pricing" };
                results.Add(new ValidationResult(String.Format("[a={0}] requires a [r=LabourPricing] component to set the labour rates.\r\nThis activity will be ignored without this component.", this.Name), memberNames));
            }
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Earn ");
                htmlWriter.Write("Earnings will be paid to ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(BankAccountName, "Account not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write(" based on <span class=\"resourcelink\">Labour Pricing</span> set in the <span class=\"resourcelink\">Labour</span>");
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
