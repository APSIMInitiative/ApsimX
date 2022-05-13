using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Models.CLEM;
using Models.CLEM.Groupings;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant herd cost </summary>
    /// <summary>This activity will arrange payment of a herd expense such as vet fees</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Arrange payment of a ruminant herd expense with specified style")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantHerdCost.htm")]
    public class RuminantActivityHerdCost : CLEMRuminantActivityBase, IValidatableObject
    {
        private FinanceType bankAccount;

        /// <summary>
        /// Amount payable
        /// </summary>
        [Description("Amount payable")]
        [Required, GreaterThanValue(0)]
        public double Amount { get; set; }

        /// <summary>
        /// Payment style
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(AnimalPaymentStyleType.perHead)]
        [Description("Payment style")]
        [Required]
        public AnimalPaymentStyleType PaymentStyle { get; set; }

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Bank account required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Category label to use in ledger
        /// </summary>
        [Description("Shortname of fee for reporting")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Shortname required")]
        public string Category { get; set; }

        /// <summary>
        /// Validate object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            switch (PaymentStyle)
            {
                case AnimalPaymentStyleType.Fixed:
                case AnimalPaymentStyleType.perHead:
                case AnimalPaymentStyleType.perAE:
                    break;
                default:
                    string[] memberNames = new string[] { "PaymentStyle" };
                    results.Add(new ValidationResult("Payment style " + PaymentStyle.ToString() + " is not supported", memberNames));
                    break;
            }
            return results;
        } 

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityHerdCost()
        {
            this.SetDefaults();
            TransactionCategory = "Livestock.Manage";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(true, true);
            bankAccount = Resources.FindResourceType<Finance, FinanceType>(this, AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportWarning);
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<ResourceRequest> resourcesNeeded = null;

            double amountNeeded = 0;
            IEnumerable<Ruminant> herd = this.CurrentHerd(false);
            if (herd.Any())
            {
                switch (PaymentStyle)
                {
                    case AnimalPaymentStyleType.Fixed:
                        amountNeeded = Amount;
                        break;
                    case AnimalPaymentStyleType.perHead:
                        amountNeeded = Amount * herd.Count();
                        break;
                    case AnimalPaymentStyleType.perAE:
                        amountNeeded = Amount * herd.Sum(a => a.AdultEquivalent);
                        break;
                    default:
                        break;
                }
            }

            if (amountNeeded > 0)
            {
                // determine breed
                // this is too much overhead for a simple reason field, especially given large herds.
                string breedName = "Herd cost";

                resourcesNeeded = new List<ResourceRequest>()
                {
                    new ResourceRequest()
                    {
                        AllowTransmutation = false,
                        Required = amountNeeded,
                        Resource = bankAccount,
                        ResourceType = typeof(Finance),
                        ResourceTypeName = this.AccountName.Split('.').Last(),
                        ActivityModel = this,
                        RelatesToResource = breedName,
                        Category = TransactionCategory
                    }
                };
            }
            return resourcesNeeded;
        }

        /// <inheritdoc/>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            // get all potential dry breeders
            IEnumerable<Ruminant> herd = this.CurrentHerd(false);
            double daysNeeded = 0;
            double numberUnits = 0;
            if (herd.Any())
            {
                switch (requirement.UnitType)
                {
                    case LabourUnitType.Fixed:
                        daysNeeded = requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perHead:
                        int head = herd.Count();
                        numberUnits = head / requirement.UnitSize;
                        if (requirement.WholeUnitBlocks)
                            numberUnits = Math.Ceiling(numberUnits);

                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    case LabourUnitType.perAE:
                        double animalEquivalents = herd.Sum(a => a.AdultEquivalent);
                        numberUnits = animalEquivalents / requirement.UnitSize;
                        if (requirement.WholeUnitBlocks)
                            numberUnits = Math.Ceiling(numberUnits);

                        daysNeeded = numberUnits * requirement.LabourPerUnit;
                        break;
                    default:
                        throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
                } 
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, TransactionCategory, this.PredictedHerdName);
        }


        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Pay ");
                htmlWriter.Write("<span class=\"setvalue\">" + Amount.ToString("#,##0.00") + "</span> ");
                htmlWriter.Write("<span class=\"setvalue\">" + PaymentStyle.ToString() + "</span> from ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(AccountName, "Account not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">This activity uses a category label ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(TransactionCategory, "Not set"));
                htmlWriter.Write(" for all transactions</div>");
                return htmlWriter.ToString(); 
            }
        } 

    }
}
