using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.CLEM.Extras;
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
    /// <summary>
    /// Activity fee component will arrange payment of a fee based on a metric value provided by the parent activity
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IHandlesActivityCompanionModels))]
    [Description("Define an expense based on tasks of parent activity")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/ActivityFee.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class ActivityFee : CLEMActivityBase, IActivityCompanionModel
    {
        [Link]
        private readonly ResourcesHolder resources = null;
        private ResourceRequest resourceRequest;

        /// <summary>
        /// An identifier for this Fee based on metrics reported by the parent activity
        /// </summary>
        /// <remarks>
        /// The values provided in the drop down list in the UI are supplied by the parent activity. If the parent
        /// does not supply any identifiers then this property will not be visible in the UI.
        /// </remarks>
        [Description("Fee identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers", VisibleCallback = "ParentSuppliedIdentifiersPresent")]
        public string Identifier { get; set; }

        /// <summary>
        /// The bank account (FinanceResourceType) to use for payment of the fee. This is a required field and must be
        /// specified.
        /// </summary>
        /// <remarks>
        /// The values provided in the drop down list in the UI are supplied by the <c>FinanceType</c> resource
        /// component
        /// </remarks>
        [Description("Resource for payment")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance), typeof(AnimalFoodStore), typeof(Equipment), typeof(HumanFoodStore), typeof(ProductStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Resource type required")]
        public string BankAccountName { get; set; }

        /// <summary>
        /// An identifier for the measure to use to calculate the fee.
        /// </summary>
        /// <remarks>
        /// The values provided in the drop down list in the UI are supplied by the parent activity.
        /// </remarks>
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedMeasures", VisibleCallback = "ParentSuppliedMeasuresPresent")]
        [Description("Measure to use")]
        public string Measure { get; set; }

        /// <summary>
        /// The amount to charge per unit of measure.
        /// </summary>
        /// <remarks>
        /// This is a required field and must be specified
        /// </remarks>
        [Description("Rate per measure")]
        [Required, GreaterThanEqualValue(0)]
        public double Amount { get; set; }

        /// <summary>
        /// The resource type being used by the Fee
        /// </summary>
        public IResourceType BankAccount;

        /// <summary>
        /// The resource component to be used to pay for this expense
        /// </summary>
        public ResourceBaseWithTransactions ResourceStore;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ActivityFee()
        {
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <summary>
        /// An event handler to perform initialisation of the Fee details
        /// </summary>
        /// <remarks>
        /// Performed during the CLEMInitialiseActivity event
        /// </remarks>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            BankAccount = resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, BankAccountName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
            if (BankAccount is not null)
            {
                ResourceStore = (BankAccount as IModel).Parent as ResourceBaseWithTransactions;
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            resourceRequest = null;
            if (MathUtilities.IsPositive(argument))
            {
                string relatesTo = null;
                if (Parent as CLEMRuminantActivityBase != null)
                {
                    relatesTo = (Parent as CLEMRuminantActivityBase).PredictedHerdNameToDisplay;
                }

                if (Parent as ResourceActivityBuy != null)
                {
                    relatesTo = (Parent as ResourceActivityBuy).ResourceName;
                }

                if (Parent is OtherAnimalsActivityBuy otherParentBuy)
                {
                    relatesTo = otherParentBuy.PredictedAnimalType;
                }

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
                return [resourceRequest];
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
    }
}
