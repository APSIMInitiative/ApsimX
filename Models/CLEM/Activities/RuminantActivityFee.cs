using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant herd cost </summary>
    /// <summary>This activity will arrange payment of a herd expense such as vet fees</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ICanHandleIdentifiableChildModels))]
//    [ValidParent(ParentType = typeof(RuminantActivityBuySell))]
//    [ValidParent(ParentType = typeof(RuminantActivityControlledMating))]
    [Description("Define a herd expense for herd management activities")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantFee.htm")]
    public class RuminantActivityFee: CLEMModel, IIdentifiableChildModel, IValidatableObject
    {
        [Link]
        private ResourcesHolder resources = null;

        /// <summary>
        /// An identifier for this Fee based on parent requirements
        /// </summary>
        [Description("Fee identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Identifier required")]
        public string Identifier { get; set; }

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Account to use required")]
        public string BankAccountName { get; set; }

        /// <summary>
        /// Payment style
        /// </summary>
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedUnits")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Units of measure required")]
        [Description("Payment style")]
        public string Units { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        [Description("Amount")]
        [Required, GreaterThanEqualValue(0)]
        public double Amount { get; set; }

        /// <summary>
        /// Label to assign each transaction created by this activity in ledgers
        /// </summary>
        [Description("Category for transactions")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Category for transactions required")]
        [Models.Core.Display(Order = 500)]
        public string TransactionCategory { get; set; }

        /// <inheritdoc/>
        [Description("Allow finance shortfall to affect activity")]
        [Required]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool ShortfallAffectsActivity { get; set; }

        /// <inheritdoc/>
        public List<ResourceRequest> GetResourceRequests(double activityMetric)
        {
            double charge;
            charge = (double)activityMetric * Amount;
            return new List<ResourceRequest>() { new ResourceRequest()
            {
                Resource = BankAccount,
                ResourceType = typeof(Finance),
                AllowTransmutation = false,
                Required = charge,
                Category = TransactionCategory,
                AdditionalDetails = this,
                RelatesToResource = (Parent as CLEMRuminantActivityBase).PredictedHerdName
            }
            };
        }

        /// <summary>
        /// Store finance type to use
        /// </summary>
        public FinanceType BankAccount;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            BankAccount = resources.FindResourceType<Finance, FinanceType>(this, BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityFee()
        {
            this.SetDefaults();
            TransactionCategory = "Livestock.[Activity]";
        }

        #region validation

        /// <summary>
        /// A method to return the list of identifiers relavent to this parent activity
        /// </summary>
        /// <returns>A list of identifiers</returns>
        public List<string> ParentSuppliedIdentifiers()
        {
            if (Parent != null && Parent is ICanHandleIdentifiableChildModels)
                return (Parent as ICanHandleIdentifiableChildModels).DefineIdentifiableChildModelLabels<RuminantActivityFee>().Identifiers;
            else
                return new List<string>();
        }

        /// <summary>
        /// A method to return the list of units relavent to this parent activity
        /// </summary>
        /// <returns>A list of units</returns>
        public List<string> ParentSuppliedUnits()
        {
            if (Parent != null && Parent is ICanHandleIdentifiableChildModels)
                return (Parent as ICanHandleIdentifiableChildModels).DefineIdentifiableChildModelLabels<RuminantActivityFee>().Units;
            else
                return new List<string>();
        }


        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Parent != null && Parent is ICanHandleIdentifiableChildModels)
            {
                var identifiers = ParentSuppliedIdentifiers();
                if (identifiers.Any() & !identifiers.Contains(Identifier))
                {
                    string[] memberNames = new string[] { "Ruminant activity fee" };
                    results.Add(new ValidationResult($"The fee identifier [{((Identifier == "") ? "BLANK" : Identifier)}] in [f={this.Name}] is not valid for the parent activity [a={Parent.Name}].{Environment.NewLine}Select an option from the list or provide an empty value for the property if no entries are provided", memberNames));
                }
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
                htmlWriter.Write("\r\n<div class=\"activityentry\">Pay ");
                htmlWriter.Write($"<span class=\"setvalue\">{Amount:#,##0.##}</span> ");
                htmlWriter.Write($"<span class=\"setvalue\">{Units}</span> ");
                htmlWriter.Write(" from ");

                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(BankAccountName, "Account not set", HTMLSummaryStyle.Resource));
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
