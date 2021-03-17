using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant feed activity</summary>
    /// <summary>This activity provides food to people from the whole available human food store based on defined nutritional targets</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("OUTDATED! BACKUP! This activity performs human feeding based upon the current labour filtering and a feeding style.")]
    [Version(1, 0, 1, "Backup of initial method for determining consumption. This activity will be depreciated.")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourActivitiyFeedToTargets1.htm")]
    public class LabourActivityFeedToTargets1 : CLEMActivityBase, IValidatableObject
    {
        private Labour people = null;
        private HumanFoodStore food = null;

        /// <summary>
        /// Feed hired labour as well as household
        /// </summary>
        [Description("Include hired labour")]
        public bool IncludeHiredLabour { get; set; }

        /// <summary>
        /// Daily intake limit
        /// </summary>
        [Description("Daily intake limit")]
        [Units("kg/AE/day")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Daily intake limit required"), GreaterThanValue(0)]
        public double DailyIntakeLimit { get; set; }

        /// <summary>
        /// Daily intake from sources other than modelled in Human Food Store
        /// </summary>
        [Description("Intake from sources not modelled")]
        [Units("kg/AE/day")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Intake from sources not modelled required"), GreaterThanEqualValue(0)]
        public double DailyIntakeOtherSources { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            people = Resources.GetResourceGroupByType(typeof(Labour)) as Labour;
            food = Resources.GetResourceGroupByType(typeof(HumanFoodStore)) as HumanFoodStore;
        }

        /// <summary>
        /// Validate component
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // check that at least one target has been provided. 
            if (this.FindAllChildren<LabourActivityFeedTarget>().Count() == 0)
            {
                string[] memberNames = new string[] { "LabourActivityFeedToTargets" };
                results.Add(new ValidationResult(String.Format("At least one [LabourActivityFeedTarget] component is required below the feed activity [{0}]", this.Name), memberNames));
            }
            return results;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            if (people is null | food is null)
            {
                return null;
            }

            List<LabourType> peopleList = people.Items.Where(a => IncludeHiredLabour || a.Hired == true).ToList();
            peopleList.Select(a => a.FeedToTargetIntake == 0);

            List<ResourceRequest> requests = new List<ResourceRequest>();

            // determine AEs to be fed
            double aE = peopleList.Sum(a => a.AdultEquivalent);

            // determine feed limits (max kg per AE per day * AEs * days)
            double intakeLimit = DailyIntakeLimit * aE * 30.4;

            // remove previous consumption
            intakeLimit -= this.DailyIntakeOtherSources * aE * 30.4;
            intakeLimit -= peopleList.Sum(a => a.GetAmountConsumed());

            List<LabourActivityFeedTarget> labourActivityFeedTargets = this.FindAllChildren<LabourActivityFeedTarget>().Cast<LabourActivityFeedTarget>().ToList();
            int feedTargetIndex = 0;

            // determine targets
            foreach (LabourActivityFeedTarget target in labourActivityFeedTargets)
            {
                // calculate target
                target.Target = target.TargetValue * aE * 30.4;

                // set initial level based on off store inputs
                target.CurrentAchieved = target.OtherSourcesValue * aE * 30.4;

                // calculate current level from previous intake this month (LabourActivityFeed)
                target.CurrentAchieved += people.GetDietaryValue(target.Metric, IncludeHiredLabour, true) * aE * 30.4;
            }

            // order food to achieve best returns for first criteria conversion factor decreasing
            List<HumanFoodStoreType> foodStoreTypes = food.FindAllChildren<HumanFoodStoreType>().Cast<HumanFoodStoreType>().OrderBy(a => a.ConversionFactor(labourActivityFeedTargets[feedTargetIndex].Metric)).ToList();

            // check availability to take food based on order in simulation tree
            while (foodStoreTypes.Count() > 0 & intakeLimit > 0)
            {
                // get next food store type
                HumanFoodStoreType foodtype = foodStoreTypes[0];

                // get amount people can still eat based on limits and previous consumption
                double amountNeededRaw = 0;
                foreach (LabourType labourType in peopleList)
                {
                    double indLimit = (labourType.AdultEquivalent * DailyIntakeLimit * 30.4);
                    double alreadyEatenThis = labourType.GetAmountConsumed(foodtype.Name);
                    double alreadyEaten = labourType.GetAmountConsumed() + labourType.FeedToTargetIntake;
                    double canStillEat = Math.Max(0, indLimit - alreadyEaten);

                    double amountOfThisFood = canStillEat;
                    amountNeededRaw += amountOfThisFood / foodtype.EdibleProportion;
                }

                // update targets based on amount available (will update excess if transmutated later)
                double amountNeededEdible = Math.Min(amountNeededRaw, foodtype.Amount) * foodtype.EdibleProportion;
                foreach (LabourActivityFeedTarget target in labourActivityFeedTargets)
                {
                    target.CurrentAchieved += amountNeededEdible * foodtype.ConversionFactor(target.Metric);
                }

                if (amountNeededRaw > 0)
                {
                    // create request
                    requests.Add(new ResourceRequest()
                    {
                        AllowTransmutation = false,
                        Required = amountNeededRaw,
                        Available = foodtype.Amount,
                        ResourceType = typeof(HumanFoodStore),
                        ResourceTypeName = foodtype.Name,
                        ActivityModel = this,
                        Category = "Consumption"
                    }
                    );
                }

                foodStoreTypes.RemoveAt(0);

                // check if target has been met (allows slight overrun)
                if (labourActivityFeedTargets[feedTargetIndex].CurrentAchieved >= labourActivityFeedTargets[feedTargetIndex].Target)
                {
                    feedTargetIndex++;
                    if (feedTargetIndex > labourActivityFeedTargets.Count())
                    {
                        // all feed targets have been met. Preserve remaining food for next time.
                        //TODO: eat food that will go off if not eaten and still below limits.
                        break;
                    }
                    // reorder remaining food types to next feed target if available
                    foodStoreTypes = foodStoreTypes.OrderBy(a => a.ConversionFactor(labourActivityFeedTargets[feedTargetIndex].Metric)).ToList();
                }
            }

            // We have now been through all food types or all targets have been achieved.
            // Any unused food will not be consumed even if it is about to spoil.
            // The food requests ready to send contain excesses that may need to be purchased but haven't been accounted for towards targets yet

            // Next we go through and check all requests that exceed available to see if we can and there is need to buy resources.
            foreach (ResourceRequest request in ResourceRequestList.Where(a => a.Required > a.Available))
            {
                // all targets have not been met
                if (feedTargetIndex <= labourActivityFeedTargets.Count())
                {
                    // allow if transmutation possible
                    if ((request.Resource as HumanFoodStoreType).TransmutationDefined)
                    {
                        // allow if still below threshold
                        if (labourActivityFeedTargets[feedTargetIndex].CurrentAchieved < labourActivityFeedTargets[feedTargetIndex].Target)
                        {
                            HumanFoodStoreType foodStore = request.Resource as HumanFoodStoreType;

                            // if this food type provides towards the target
                            if (foodStore.ConversionFactor(labourActivityFeedTargets[feedTargetIndex].Metric) > 0)
                            {
                                // work out what the extra is worth
                                double excess = request.Required - request.Available;
                                // get target needed
                                double remainingToTarget = labourActivityFeedTargets[feedTargetIndex].Target - labourActivityFeedTargets[feedTargetIndex].CurrentAchieved;
                                double excessConverted = excess * foodStore.EdibleProportion * foodStore.ConversionFactor(labourActivityFeedTargets[feedTargetIndex].Metric);

                                // reduce if less than needed
                                double prop = Math.Max(excessConverted / remainingToTarget, 1.0);
                                double newExcess = excess * prop;
                                request.Required = request.Available + newExcess;
                                request.AllowTransmutation = true;

                                // update targets based on new amount eaten
                                foreach (LabourActivityFeedTarget target in labourActivityFeedTargets)
                                {
                                    target.CurrentAchieved += newExcess * foodStore.EdibleProportion * foodStore.ConversionFactor(target.Metric);
                                }

                                // move to next target if achieved.
                                if (labourActivityFeedTargets[feedTargetIndex].CurrentAchieved >= labourActivityFeedTargets[feedTargetIndex].Target)
                                {
                                    feedTargetIndex++;
                                }
                            }
                        }
                    }
                }
                // transmutation not allowed so only get what was available.
                if (request.AllowTransmutation == false)
                {
                    request.Required = request.Available;
                }
            }
            return requests;
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            List<LabourType> group = Resources.Labour().Items.Where(a => a.Hired != true).ToList();
            int head = 0;
            double adultEquivalents = 0;
            foreach (Model child in this.FindAllChildren<LabourFeedGroup>())
            {
                var subgroup = group.Filter(child).ToList();
                head += subgroup.Count();
                adultEquivalents += subgroup.Sum(a => a.AdultEquivalent);
            }

            double daysNeeded = 0;
            double numberUnits = 0;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    numberUnits = head / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perAE:
                    numberUnits = adultEquivalents / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Feeding", null);
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            if (LabourLimitProportion < 1)
            {
                foreach (ResourceRequest item in ResourceRequestList)
                {
                    if (item.ResourceType != typeof(LabourType))
                    {
                        item.Provided *= LabourLimitProportion;
                    }
                }
            }
            return;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            // add all provided requests to the individuals intake pools.

            List<LabourType> group = Resources.Labour().Items.Where(a => IncludeHiredLabour | a.Hired != true).ToList();
            double aE = group.Sum(a => a.AdultEquivalent);
            Status = ActivityStatus.NotNeeded;
            if (group != null && group.Count > 0)
            {
                var requests = ResourceRequestList.Where(a => a.ResourceType == typeof(HumanFoodStore));
                if (requests.Count() > 0)
                {
                    this.Status = ActivityStatus.Success;
                    foreach (ResourceRequest request in requests)
                    {
                        if (request.Provided > 0)
                        {
                            if (request.Provided < request.Available)
                            {
                                this.Status = ActivityStatus.Partial;
                            }

                            // add to individual intake
                            foreach (LabourType labour in group)
                            {
                                labour.AddIntake(new LabourDietComponent()
                                {
                                    AmountConsumed = request.Provided * (labour.AdultEquivalent / aE),
                                    FoodStore = request.Resource as HumanFoodStoreType
                                });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "<div class=\"activityentry\">";
            html += "Each Adult Equivalent is able to consume ";
            if (DailyIntakeLimit > 0)
            {
                html += "<span class=\"setvalue\">";
                html += DailyIntakeLimit.ToString("#,##0.##");
            }
            else
            {
                html += "<span class=\"errorlink\">NOT SET";
            }
            html += "</span> kg per day</div>";
            if (DailyIntakeOtherSources > 0)
            {
                html += "with <span class=\"setvalue\">";
                html += DailyIntakeOtherSources.ToString("#,##0.##");
                html += "</span> provided from non-modelled sources";
            }
            html += "</div>";
            html += "<div class=\"activityentry\">";
            html += "Hired labour <span class=\"setvalue\">" + ((IncludeHiredLabour) ? "is" : "is not") + "</span> included";
            html += "</div>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            html += "\r\n</div>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            string html = "";
            html += "\r\n<div class=\"croprotationborder\">";
            html += "<div class=\"croprotationlabel\">The people will eat to the following targets:</div>";

            if (this.FindAllChildren<LabourActivityFeedTarget>().Count() == 0)
            {
                html += "\r\n<div class=\"errorbanner clearfix\">";
                html += "<div class=\"filtererror\">No Feed To Target component provided</div>";
                html += "</div>";
            }
            return html;
        }
    }
}
