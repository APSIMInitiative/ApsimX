using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core.Attributes;
using System.IO;
using Models.CLEM.Interfaces;
using Newtonsoft.Json;
using APSIM.Numerics;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// Defines the labour required for an activity
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(IHandlesActivityCompanionModels))]
    [Description("Defines the amount and type of labour required for an activity. This component must have at least one LabourFilterGroup as a child")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourRequirement.htm")]
    public class LabourRequirement: CLEMActivityBase, IValidatableObject, IActivityCompanionModel, IReportPartialResourceAction
    {
        private double maximumDaysPerPerson = 0;
        private double maximumDaysPerGroup = 0;
        private double minimumDaysPerPerson = 0;
        private Labour labourResource;
        private readonly List<ResourceRequest> resourceList = new();

        /// <summary>
        /// An identifier for this Labour requirement based on parent requirements
        /// </summary>
        [Description("Labour identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers", VisibleCallback = "ParentSuppliedIdentifiersPresent")]
        public string Identifier { get; set; }

        /// <summary>
        /// Days labour required per unit or fixed (days)
        /// </summary>
        [Description("Labour required (days)")]
        [Tooltip("Number of days required for the number of unit blocks specified (or fixed if set as unit)")]
        [Required, GreaterThanEqualValue(0)]
        [Category("Farm", "Rate")]
        public double LabourPerUnit { get; set; } = 1.0;

        /// <summary>
        /// Size of unit
        /// </summary>
        [Description("Number of units per allocation block")]
        [Tooltip("The number of units per days labour required")]
        [Required, GreaterThanEqualValue(0)]
        [Category("Farm", "Units")]
        public double UnitSize { get; set; } = 1.0;

        /// <summary>
        /// Labour unit type
        /// </summary>
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedMeasures", VisibleCallback = "ParentSuppliedMeasuresPresent")]
        [Tooltip("The style of units to consider")]
        [Description("Measure of units")]
        [Category("Farm", "Units")]
        public string Measure { get; set; }

        /// <summary>
        /// Whole unit blocks only
        /// </summary>
        [Description("Use whole units")]
        [Tooltip("Labour supplied in whole blocks of the number of units only")]
        [Category("Farm", "Units")]
        public bool WholeUnitBlocks { get; set; }

        /// <summary>
        /// Labour limit style
        /// </summary>
        [Description("Limit style")]
        [Tooltip("The style of providing limits to the amount of labour provided")]
        [Category("Farm", "Limits")]
        [Required]
        public LabourLimitType LimitStyle { get; set; } = LabourLimitType.ProportionOfDaysRequired;

        /// <summary>
        /// Maximum labour allocated per labour group
        /// </summary>
        [Description("Maximum per group for task")]
        [Required, GreaterThanValue(0)]
        [Category("Farm", "Limits")]
        public double MaximumPerGroup { get; set; } = 1.0;

        /// <summary>
        /// Minimum labour allocated per person for task
        /// </summary>
        [Description("Minimum per person for task")]
        [Required, GreaterThanEqualValue(0)]
        [Category("Farm", "Limits")]
        public double MinimumPerPerson { get; set; }

        /// <summary>
        /// Maximum labour allocated per person for task
        /// </summary>
        [Description("Maximum per person for task")]
        [Required, GreaterThanValue(0), GreaterThan("MinimumPerPerson", ErrorMessage ="Maximum per individual must be greater than minimum per individual in Labour Required")]
        [Category("Farm", "Limits")]
        public double MaximumPerPerson { get; set; }

        /// <summary>
        /// Apply to all matching labour (everyone performs activity)
        /// </summary>
        [Description("Apply to all matching (everyone performs activity)")]
        [Required]
        [Category("Farm", "Rate")]
        public bool ApplyToAll { get; set; }

        /// <summary>
        /// Get the calculated maximum days per person for activity from CalculateLimits
        /// </summary>
        [JsonIgnore]
        public double MaximumDaysPerPerson { get { return maximumDaysPerPerson; } }

        /// <summary>
        /// Get the calculated maximum days per person for activity from CalculateLimits
        /// </summary>
        [JsonIgnore]
        public double MaximumDaysPerGroup { get { return maximumDaysPerGroup; } }

        /// <summary>
        /// Get the calculated maximum days per person for activity from CalculateLimits
        /// </summary>
        [JsonIgnore]
        public double MinimumDaysPerPerson { get { return minimumDaysPerPerson; } }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            labourResource = Resources.FindResourceGroup<Labour>();
        }

        /// <summary>
        /// Calculate the limits for people and groups using the style
        /// </summary>
        public void CalculateLimits(double amountRequested)
        {
            switch (LimitStyle)
            {
                case LabourLimitType.AsRatePerUnitsAllowed:
                    double units = amountRequested / LabourPerUnit;
                    maximumDaysPerPerson = units * MaximumPerPerson;
                    maximumDaysPerGroup = units * MaximumPerGroup;
                    minimumDaysPerPerson = units * MinimumPerPerson;
                    break;
                case LabourLimitType.AsTotalDaysAllowed:
                    maximumDaysPerPerson = MaximumPerPerson;
                    maximumDaysPerGroup = MaximumPerGroup;
                    minimumDaysPerPerson = MinimumPerPerson;
                    break;
                case LabourLimitType.ProportionOfDaysRequired:
                    maximumDaysPerPerson = amountRequested * MaximumPerPerson;
                    maximumDaysPerGroup = amountRequested * MaximumPerGroup;
                    minimumDaysPerPerson = amountRequested * MinimumPerPerson;
                    break;
                default:
                    break;
            }
            return;
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double activityMetric)
        {
            resourceList.Clear();
            double daysNeeded;
            switch (Measure)
            {
                case "Fixed":
                    daysNeeded = LabourPerUnit * activityMetric;
                    break;
                default:
                    double numberUnits = activityMetric / UnitSize;
                    if (WholeUnitBlocks)
                        numberUnits = Math.Ceiling(numberUnits);
                    daysNeeded = numberUnits * LabourPerUnit;
                    break;
            }

            if (MathUtilities.IsPositive(daysNeeded))
            {
                foreach (LabourGroup fg in Structure.FindChildren<LabourGroup>())
                {
                    int numberOfPpl = 1;
                    if (ApplyToAll)
                    {
                        numberOfPpl = fg.Filter(labourResource.Items).Count();
                    }

                    for (int i = 0; i < numberOfPpl; i++)
                    {
                        resourceList.Add(new ResourceRequest()
                        {
                            AllowTransmutation = true,
                            Required = daysNeeded,
                            ResourceType = typeof(Labour),
                            ResourceTypeName = "",
                            ActivityModel = this,
                            FilterDetails = new List<object>() { fg },
                            Category = TransactionCategory,
                        }
                        );
                    }
                }
            }
            return resourceList;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (resourceList.Count != 0)
            {
                SetStatusSuccessOrPartial(resourceList.Where(a => a.ResourceType == typeof(Labour) && a.Provided > a.Required).Any());
            }
        }

        #region validation
        /// <summary>
        /// Validate this object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // ensure labour resource added
            Labour lab = Resources.FindResource<Labour>();
            if (lab == null)
            {
                Summary.WriteMessage(this, "No [r=Labour] resources in simulation. All [LabourRequirement] will be ignored.", MessageType.Warning);
            }
            else if (lab.Children.Count <= 0)
            {
                Summary.WriteMessage(this, "No [r=LabourResourceTypes] are provided in the [r=Labour] resource. All [LabourRequirement] will be ignored.", MessageType.Warning);
            }

            // check filter groups present
            if (!Structure.FindChildren<LabourGroup>().Any())
            {
                yield return new ValidationResult($"No [f=LabourFilterGroup] is provided with the [LabourRequirement] for [a={NameWithParent}].{Environment.NewLine}Add a [LabourFilterGroup] to specify individuals for this activity.", new string[] { "Labour filter group" });
            }

            // check for individual nesting.
            foreach (LabourGroup labourGroup in Structure.FindChildren<LabourGroup>())
            {
                LabourGroup currentFilterGroup = labourGroup;
                while (currentFilterGroup != null && Structure.FindChildren<LabourGroup>(relativeTo: currentFilterGroup).Any())
                {
                    if (Structure.FindChildren<LabourGroup>(relativeTo: currentFilterGroup).Count() > 1)
                    {
                        yield return new ValidationResult($"Invalid nested labour filter groups in [f={currentFilterGroup.Name}] for [a={Name}]. Only one nested filter group is permitted each branch. Additional filtering will be ignored.", new string[] { "Labour filter group" });
                    }
                    currentFilterGroup = Structure.FindChildren<LabourGroup>(relativeTo: currentFilterGroup).FirstOrDefault();
                }
            }
        }
        #endregion
    }
}
