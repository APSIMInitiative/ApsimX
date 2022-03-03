using Models.Core;
using Models.CLEM.Resources;
using Models.CLEM.Interfaces;
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
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantHerdCost.htm")]
    public class RuminantActivityHerdCost : CLEMRuminantActivityBase, ICanHandleIdentifiableChildModels
    {
        //private FinanceType bankAccount;
        private int numberToDo;
        private int numberToSkip;
        private double amountToDo;
        private double amountToSkip;

        private IEnumerable<Ruminant> uniqueIndividuals;
        private IEnumerable<RuminantGroup> filterGroups;

        ///// <summary>
        ///// Amount payable
        ///// </summary>
        //[Description("Amount payable")]
        //[Required, GreaterThanValue(0)]
        //public double Amount { get; set; }

        ///// <summary>
        ///// Payment style
        ///// </summary>
        //[System.ComponentModel.DefaultValueAttribute(AnimalPaymentStyleType.perHead)]
        //[Description("Payment style")]
        //[Required]
        //public AnimalPaymentStyleType PaymentStyle { get; set; }

        ///// <summary>
        ///// Bank account to use
        ///// </summary>
        //[Description("Bank account to use")]
        //[Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance) } })]
        //[Required(AllowEmptyStrings = false, ErrorMessage = "Bank account required")]
        //public string AccountName { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityHerdCost()
        {
            this.SetDefaults();
            TransactionCategory = "Livestock.Cost";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(true, true);
            filterGroups = GetIdentifiableChildrenByIdentifier<RuminantGroup>("Individuals to cost", true, false);

            //bankAccount = Resources.FindResourceType<Finance, FinanceType>(this, AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportWarning);
        }

        /// <inheritdoc/>
        public override LabelsForIdentifiableChildren DefineIdentifiableChildModelLabels<T>()
        {
            switch (typeof(T).Name)
            {
                case "RuminantGroup":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>() {
                            "Individuals to cost" },
                        units: new List<string>()
                        );
                case "RuminantActivityFee":
                case "LabourRequirement":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>() {
                            "Number paid for",
                        },
                        units: new List<string>() {
                            "fixed",
                            "per head",
                            "per AE"
                        }
                        );
                default:
                    return new LabelsForIdentifiableChildren();
            }
        }

        /// <inheritdoc/>
        protected override List<ResourceRequest> DetermineResourcesForActivity()
        {
            numberToDo = 0;
            numberToSkip = 0;
            IEnumerable<Ruminant> herd = GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.NotMarkedForSale);
            uniqueIndividuals = GetUniqueIndividuals<Ruminant>(filterGroups, herd);
            numberToDo = uniqueIndividuals?.Count() ?? 0;

            // provide updated units of measure for identifiable children
            foreach (var valueToSupply in valuesForIdentifiableModels.ToList())
            {
                int number = numberToDo;
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForIdentifiableModels[valueToSupply.Key] = 1;
                        break;
                    case "per head":
                        valuesForIdentifiableModels[valueToSupply.Key] = number;
                        break;
                    case "per AE":
                        amountToDo = uniqueIndividuals.Sum(a => a.AdultEquivalent);
                        valuesForIdentifiableModels[valueToSupply.Key] = amountToDo;
                        break;
                    default:
                        throw new NotImplementedException($"Unknown units [{((valueToSupply.Key.unit == "") ? "Blank" : valueToSupply.Key.unit)}] for [{((valueToSupply.Key.identifier == "") ? "Blank" : valueToSupply.Key.identifier)}] identifier in [a={NameWithParent}]");
                }
            }
            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForActivity()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // find shortfall by identifiers as these may have different influence on outcome
                var shorts = shortfalls.Where(a => a.IdentifiableChildDetails.unit == "per head").FirstOrDefault();
                if (shorts != null)
                    numberToSkip = Convert.ToInt32(numberToDo * shorts.Required / shorts.Provided);

                var amountShort = shortfalls.Where(a => a.IdentifiableChildDetails.unit == "per AE").FirstOrDefault();
                if (amountShort != null)
                    amountToSkip = Convert.ToInt32(amountToDo * amountShort.Required / amountShort.Provided);

                this.Status = ActivityStatus.Partial;
            }
        }

        /// <inheritdoc/>
        protected override void PerformTasksForActivity()
        {
            if (numberToDo - numberToSkip > 0)
            {
                amountToDo -= amountToSkip;
                double amountDone = 0;
                int number = 0;
                foreach (Ruminant ruminant in uniqueIndividuals.SkipLast(numberToSkip).ToList())
                {
                    amountDone += ruminant.AdultEquivalent;
                    amountToDo -= ruminant.AdultEquivalent;
                    number++;
                    if (amountToDo <= 0)
                        break;
                }

                if (number == numberToDo && amountToDo <= 0)
                    SetStatusSuccess();
                else
                    this.Status = ActivityStatus.Partial;
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Pay ");
                //htmlWriter.Write("<span class=\"setvalue\">" + Amount.ToString("#,##0.00") + "</span> ");
                //htmlWriter.Write("<span class=\"setvalue\">" + PaymentStyle.ToString() + "</span> from ");
                //htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(AccountName, "Account not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">This activity uses a category label ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(TransactionCategory, "Not set"));
                htmlWriter.Write(" for all transactions</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
