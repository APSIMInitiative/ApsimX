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
    /// <summary>Activity to arrange payment of hired labour at start of CLEM timestep
    /// Labour can be limited by shortfall
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
        /// Constructor
        /// </summary>
        public LabourActivityPayHired()
        {
            // activity is performed in CLEMStartOfTimestep not default CLEMGetResources
            this.AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "ActivityFee":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>()
                        {
                            "labour available",
                            "labour used"
                        },
                        measures: new List<string>() {
                            "fixed",
                            "per day",
                            "per total charged"
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <summary>An event handler to allow us to initialise</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            labour = Resources.FindResourceGroup<Labour>();
        }

        /// <summary>An event handler to allow us to organise payment at start of timestep.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            task = "available";
            ResourceRequestList = new List<ResourceRequest>(); 
            ManageActivityResourcesAndTasks("labour available");
        }

        /// <summary>An event handler to allow us to organise payment at start of timestep.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMHerdSummary")]
        private void OnCLEMHerdSummary(object sender, EventArgs e)
        {
            task = "used";
            ResourceRequestList = new List<ResourceRequest>();
            ManageActivityResourcesAndTasks("labour used");
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            amountToSkip = 0;

            int currentmonth = clock.Today.Month;
            double totaldays = 0;
            double totalfees = 0;
            foreach (LabourType item in labour.Items.Where(a => a.Hired))
            {
                double days = 0;
                switch (task)
                {
                    case "available":
                        days = item.LabourAvailability.GetAvailability(currentmonth - 1);
                        break;
                    case "used":
                        days = item.LabourAvailability.GetAvailability(currentmonth - 1) - item.AvailableDays;
                        break;
                }
                totaldays += days;
                totalfees += days * item.PayRate();
            }
            amountToDo = totaldays;

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    case "per day":
                        valuesForCompanionModels[valueToSupply.Key] = totaldays;
                        break;
                    case "per total charged":
                        valuesForCompanionModels[valueToSupply.Key] = totalfees;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                }
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
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // make sure finance present
            // this is performed in the assignment of bankaccount in InitialiseActivity

            if (labour is null)
            {
                string[] memberNames = new string[] { "Labour" };
                results.Add(new ValidationResult("No [r=Labour] is provided in resources\r\nThis activity will not be performed without labour.", memberNames));
            }
            else
            {
                // make sure labour hired present
                if (!labour.Items.Where(a => a.Hired).Any())
                {
                    string[] memberNames = new string[] { "Hired labour" };
                    results.Add(new ValidationResult("No [r=LabourType] of hired labour has been defined in [r=Labour]\r\nThis activity will not be performed without hired labour.", memberNames));
                }
                // make sure pay rates present
                if (!labour.PricingAvailable)
                {
                    string[] memberNames = new string[] { "Labour pay rate" };
                    results.Add(new ValidationResult("No [r=LabourPricing] is available for [r=Labour]\r\nThis activity will not be performed without labour pay rates.", memberNames));
                }
            }
            return results;
        }
        #endregion

        #region descriptive summary
        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">Pay all hired labour based on associated Fee components</div>");
            return htmlWriter.ToString();
        }
        #endregion

    }
}
