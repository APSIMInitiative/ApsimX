using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// This activity arranges payment for all hired labour performed during the time step
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs payment of all hired labour in the time step")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/PayHiredLabour.htm")]
    public class LabourActivityPayHired : CLEMActivityBase, IValidatableObject, IHandlesActivityCompanionModels
    {
        [Link]
        private readonly IClock clock = null;
        private double amountToDo;
        private double amountToSkip;
        private string task = "";
        private Labour labour;

        /// <summary>
        /// Default constructor
        /// </summary>
        public LabourActivityPayHired()
        {
            // activity is performed in CLEMStartOfTimestep not default CLEMGetResources
            this.AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            return type switch
            {
                "ActivityFee" => new LabelsForCompanionModels(
                                        identifiers:
                                        [
                                            "labour available",
                                            "labour used"
                                        ],
                                        measures: [
                                            "fixed",
                                            "per day",
                                            "per total charged"
                                        ]
                                        ),
                _ => new LabelsForCompanionModels(),
            };
        }

        /// <summary>An event handler allowing for any activity initialisation</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// This is performed during the <c>CLEMEvents.CLEMInitialiseActivity</c> event
        /// </remarks>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            labour = Resources.FindResourceGroup<Labour>();
        }

        /// <summary>An event handler to initialise details at the start of the time step.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// This is performed during the CLEMStartOfTimeStep event
        /// </remarks>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            task = "available";
            ResourceRequestList = []; 
            ManageActivityResourcesAndTasks("labour available");
        }

        /// <summary>
        /// An event handler to initialise details after labour has been allocated during the time step.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// This is performed during the CLEMHerdSummary event
        /// </remarks>
        [EventSubscribe("CLEMHerdSummary")]
        private void OnCLEMHerdSummary(object sender, EventArgs e)
        {
            task = "used";
            ResourceRequestList = [];
            ManageActivityResourcesAndTasks("labour used");
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            amountToSkip = 0;

            int currentMonth = clock.Today.Month;
            double totalDays = 0;
            double totalFees = 0;
            foreach (LabourType item in labour.Items.Where(a => a.IsHired))
            {
                double days = 0;
                switch (task)
                {
                    case "available":
                        days = item.LabourAvailability.GetAvailability(currentMonth - 1);
                        break;
                    case "used":
                        days = item.LabourAvailability.GetAvailability(currentMonth - 1) - item.AvailableDays;
                        break;
                }
                totalDays += days;
                totalFees += days * item.PayRate();
            }
            amountToDo = totalDays;

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                valuesForCompanionModels[valueToSupply.Key] = valueToSupply.Key.unit switch
                {
                    "fixed" => 1,
                    "per day" => totalDays,
                    "per total charged" => totalFees,
                    _ => throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key)),
                };
            }
            return null;
        }

        ///<inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // find shortfall by identifiers as these may have different influence on outcome
                var tagsShort = shortfalls.FirstOrDefault();
                amountToSkip = Convert.ToInt32(amountToDo * (1 - tagsShort.Available / tagsShort.Required));
                if (amountToSkip < 0)
                {
                    Status = ActivityStatus.Warning;
                    AddStatusMessage("Resource shortfall prevented any action");
                }
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (amountToDo > 0)
                SetStatusSuccessOrPartial(amountToSkip > 0);
        }

        #region validation
        /// <summary>
        /// Custom validation for the component
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>A list of validation results.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // make sure finance present
            // this is performed in the assignment of bankaccount in InitialiseActivity

            if (labour is null)
            {
                string[] memberNames = ["Labour"];
                results.Add(new ValidationResult("No [r=Labour] is provided in resources\r\nThis activity will not be performed without labour.", memberNames));
            }
            else
            {
                // make sure labour hired present
                if (!labour.Items.Any(a => a.IsHired))
                {
                    string[] memberNames = ["Hired labour"];
                    results.Add(new ValidationResult("No [r=LabourType] of hired labour has been defined in [r=Labour]\r\nThis activity will not be performed without hired labour.", memberNames));
                }
                // make sure pay rates present
                if (!labour.PricingAvailable)
                {
                    string[] memberNames = ["Labour pay rate"];
                    results.Add(new ValidationResult("No [r=LabourPricing] is available for [r=Labour]\r\nThis activity will not be performed without labour pay rates.", memberNames));
                }
            }
            return results;
        }
        #endregion
    }
}
