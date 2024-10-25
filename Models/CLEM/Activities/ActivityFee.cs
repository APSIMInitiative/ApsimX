using APSIM.Shared.Utilities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Activity fee</summary>
    /// <summary>This activity will arrange payment of a fee based on a metric value provided by the parent activity</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IHandlesActivityCompanionModels))]
    [Description("Define an expense based on tasks of parent activity")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/ActivityFee.htm")]

    public class ActivityFee : CLEMActivityBase, IActivityCompanionModel
    {
        [Link]
        private ResourcesHolder resources = null;
        private ResourceRequest resourceRequest;

        /// <summary>
        /// An identifier for this Fee based on parent requirements
        /// </summary>
        [Description("Fee identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers", VisibleCallback = "ParentSuppliedIdentifiersPresent")]
        public string Identifier { get; set; }

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Resource to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance), typeof(AnimalFoodStore), typeof(Equipment), typeof(HumanFoodStore), typeof(ProductStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Resource type required")]
        public string BankAccountName { get; set; }

        /// <summary>
        /// Payment style
        /// </summary>
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedMeasures", VisibleCallback = "ParentSuppliedMeasuresPresent")]
        [Description("Measure to use")]
        public string Measure { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        [Description("Rate per measure")]
        [Required, GreaterThanEqualValue(0)]
        public double Amount { get; set; }

        /// <summary>
        /// Store finance type to use
        /// </summary>
        public IResourceType BankAccount;

        /// <summary>
        /// Store type to use
        /// </summary>
        public ResourceBaseWithTransactions ResourceStore;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityFee()
        {
            this.SetDefaults();
            AllocationStyle = ResourceAllocationStyle.Manual;
            ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get resource type to buy
            BankAccount = resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, BankAccountName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
            if (BankAccount is not null)
                ResourceStore = (BankAccount as IModel).Parent as ResourceBaseWithTransactions;
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            resourceRequest = null;
            if (MathUtilities.IsPositive(argument))
            {
                string relatesTo = null;
                if (Parent as CLEMRuminantActivityBase != null)
                    relatesTo = (Parent as CLEMRuminantActivityBase).PredictedHerdNameToDisplay;
                if (Parent as ResourceActivityBuy != null)
                    relatesTo = (Parent as ResourceActivityBuy).ResourceName;
                if (Parent is OtherAnimalsActivityBuy otherParentBuy)
                    relatesTo = otherParentBuy.PredictedAnimalType;

                double charge = argument * Amount;
                resourceRequest = new ResourceRequest()
                {
                    Resource = BankAccount,
                    ResourceType = ResourceStore.GetType(),
                    ResourceTypeName = BankAccount.Name,
                    AllowTransmutation = true,
                    Required = charge,
                    Category = TransactionCategory,
                    AdditionalDetails = this,
                    RelatesToResource = relatesTo,
                    ActivityModel = this
                };
                return new List<ResourceRequest>() { resourceRequest };
            }
            return null;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (resourceRequest != null && argument != 0)
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
                htmlWriter.Write($"\r\n<div class=\"activityentry\">Pay {CLEMModel.DisplaySummaryValueSnippet(Amount, "Rate not set")} ");
                if(Measure?.ToLower() != "")
                    htmlWriter.Write($"per {CLEMModel.DisplaySummaryValueSnippet(Measure, "Measure not set")} ");
                htmlWriter.Write($"from {DisplaySummaryResourceTypeSnippet(BankAccountName)}</div>");
                return htmlWriter.ToString();
            }
        }
        #endregion

    }
}
