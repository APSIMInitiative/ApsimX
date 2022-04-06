using APSIM.Shared.Utilities;
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
    [ValidParent(ParentType = typeof(IHandlesActivityCompanionModels))]
    [Description("Define a herd expense for herd management activities")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantFee.htm")]
    public class RuminantActivityFee: CLEMActivityBase, IActivityCompanionModel
    {
        [Link]
        private ResourcesHolder resources = null;
        private ResourceRequest resourceRequest; 

        /// <summary>
        /// An identifier for this Fee based on parent requirements
        /// </summary>
        [Description("Fee identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers")]
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
        [Description("Payment style")]
        public string Units { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        [Description("Amount")]
        [Required, GreaterThanEqualValue(0)]
        public double Amount { get; set; }

        /// <summary>
        /// Store finance type to use
        /// </summary>
        public FinanceType BankAccount;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityFee()
        {
            this.SetDefaults();
            TransactionCategory = "Livestock.[Fee]";
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            BankAccount = resources.FindResourceType<Finance, FinanceType>(this, BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            resourceRequest = null;
            if (MathUtilities.IsPositive(argument))
            {
                Status = ActivityStatus.NotNeeded;
                double charge = argument * Amount;
                resourceRequest = new ResourceRequest()
                {
                    Resource = BankAccount,
                    ResourceType = typeof(Finance),
                    AllowTransmutation = true,
                    Required = charge,
                    Category = TransactionCategory,
                    AdditionalDetails = this,
                    RelatesToResource = (Parent as CLEMRuminantActivityBase).PredictedHerdName,
                    ActivityModel = this
                };
                return new List<ResourceRequest>() { resourceRequest };
            }
            return null;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if(resourceRequest != null)
            {
                bool shortfalls = MathUtilities.IsLessThan(resourceRequest.Provided, resourceRequest.Required);
                SetStatusSuccessOrPartial(shortfalls);
            }
        }

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
