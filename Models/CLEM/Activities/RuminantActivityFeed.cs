using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant feed activity</summary>
    /// <summary>This activity provides food to specified ruminants based on a feeding style</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Feed ruminants by a feeding style.")]
    [Version(1, 0, 4, "Added smart feeding switch to stop feeding when animals are satisfied and avoid overfeed wastage")]
    [Version(1, 0, 3, "User defined PotentialIntake modifer and reporting of trampling and overfed wastage in ledger")]
    [Version(1, 0, 2, "Manages feeding whole herd a specified daily amount or proportion of available feed")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantFeed.htm")]
    public class RuminantActivityFeed : CLEMRuminantActivityBase, IValidatableObject
    {
        [Link]
        private Clock clock = null;

        // amount requested
        private double feedEstimated = 0;
        // amount actually needed to satisfy animals
        private double feedToSatisfy = 0;
        // amount actually needed to satisfy animals allowing for overfeeding
        private double feedToOverSatisfy = 0;
        // does this feeding style need a potential intake modifier
        private bool usingPotentialIntakeMultiplier = false;
        private double overfeedProportion = 1;

        /// <summary>
        /// Name of Feed to use (with Resource Group name appended to the front [separated with a '.'])
        /// eg. AnimalFoodStore.RiceStraw
        /// </summary>
        [Description("Feed to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(AnimalFoodStore), typeof(HumanFoodStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Feed type required")]
        public string FeedTypeName { get; set; }

        /// <summary>
        /// Proportion wasted (e.g. trampling, 0 = feed trough present)
        /// </summary>
        [Description("Proportion wasted (e.g. trampling, 0 = feed trough present)")]
        [Required, Proportion]
        public double ProportionTramplingWastage { get; set; }

        /// <summary>
        /// Feeding style to use
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(RuminantFeedActivityTypes.SpecifiedDailyAmount)]
        [Description("Feeding style to use")]
        [Required]
        public RuminantFeedActivityTypes FeedStyle { get; set; }

        /// <summary>
        /// Stop feeding when animals are satisfied
        /// </summary>
        [Description("Stop feeding when satisfied")]
        [Required]
        public bool StopFeedingWhenSatisfied { get; set; }

        /// <summary>
        /// Feed type
        /// </summary>
        [JsonIgnore]
        public IFeedType FeedType { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityFeed()
        {
            this.SetDefaults();
            TransactionCategory = "Livestock.Feed";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get all ui tree herd filters that relate to this activity
            this.InitialiseHerd(true, true);

            // locate FeedType resource
            FeedType = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IFeedType;
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            var herd = CurrentHerd(false);
            feedEstimated = 0;
            feedToSatisfy = 0;
            feedToOverSatisfy = 0;
            bool singleFeedAmountsCalculated = false;

            // get list from filters
            foreach (var child in FindAllChildren<FilterGroup<Ruminant>>())
            {
                double value = 0;
                if (child is RuminantFeedGroup rfg)
                    value = rfg.Value;
                else if (child is RuminantFeedGroupMonthly rfgm)
                    value = rfgm.MonthlyValues[clock.Today.Month - 1];
                else
                    continue;

                bool countNeeded = false;
                bool weightNeeded = false;
                switch (FeedStyle)
                {
                    case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                        countNeeded = true;
                        break;
                    case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                        countNeeded = true;
                        break;
                    case RuminantFeedActivityTypes.ProportionOfWeight:
                        weightNeeded = true;
                        break;
                    case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                        break;
                    case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                        break;
                    case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                        countNeeded = true;
                        break;
                    default:
                        break;
                }

                var selectedIndividuals = child.Filter(herd).GroupBy(i => 1).Select(a => new {
                    Count = countNeeded ? a.Count() : 0,
                    Weight = weightNeeded ? a.Sum(b => b.Weight) : 0,
                    Intake = a.Sum(b => b.Intake),
                    PotentialIntake = a.Sum(b => b.PotentialIntake),
                    IntakeMultiplier = usingPotentialIntakeMultiplier ? a.FirstOrDefault().BreedParams.OverfeedPotentialIntakeModifier : 1
                }).ToList();

                if (selectedIndividuals.Count > 0)
                {
                    var selectedIndividualsDetails = selectedIndividuals.FirstOrDefault();

                    switch (FeedStyle)
                    {
                        case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                            usingPotentialIntakeMultiplier = true;
                            if (!singleFeedAmountsCalculated && selectedIndividualsDetails.Count > 0)
                            {
                                feedEstimated += value * 30.4;
                                singleFeedAmountsCalculated = true;
                            }
                            break;
                        case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                            feedEstimated += (value * 30.4) * selectedIndividualsDetails.Count;
                            usingPotentialIntakeMultiplier = true;
                            break;
                        case RuminantFeedActivityTypes.ProportionOfWeight:
                            usingPotentialIntakeMultiplier = true;
                            feedEstimated += value * selectedIndividualsDetails.Weight * 30.4;
                            break;
                        case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                            feedEstimated += value * selectedIndividualsDetails.PotentialIntake;
                            break;
                        case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                            feedEstimated += value * (selectedIndividualsDetails.PotentialIntake - selectedIndividualsDetails.Intake);
                            break;
                        case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                            usingPotentialIntakeMultiplier = true;
                            if (!singleFeedAmountsCalculated && selectedIndividualsDetails.Count > 0)
                            {
                                feedEstimated += value * FeedType.Amount;
                                singleFeedAmountsCalculated = true;
                            }
                            break;
                        default:
                            throw new Exception($"FeedStyle [{FeedStyle}] is not supported in [a={Name}]");
                    }

                    // get the amount that can be eaten. Does not account for individuals in multiple filters
                    // accounts for some feeding style allowing overeating to the user declared value in ruminant 

                    feedToSatisfy += selectedIndividualsDetails.PotentialIntake - selectedIndividualsDetails.Intake;
                    feedToOverSatisfy += selectedIndividualsDetails.PotentialIntake * selectedIndividualsDetails.IntakeMultiplier - selectedIndividualsDetails.Intake;
                }
            }

            if(StopFeedingWhenSatisfied)
                // restrict to max intake permitted by individuals and avoid overfeed wastage
                feedEstimated = Math.Min(feedEstimated, Math.Max(feedToOverSatisfy, feedToSatisfy));

            if (feedEstimated > 0)
            {
                // create food resrouce packet with details
                FoodResourcePacket foodPacket = new FoodResourcePacket()
                {
                    Amount = feedEstimated,
                    DMD = FeedType.DMD,
                    PercentN = FeedType.Nitrogen
                };

                return new List<ResourceRequest>()
                {
                    new ResourceRequest()
                    {
                        AllowTransmutation = true,
                        Required = feedEstimated,
                        Resource = FeedType,
                        ResourceType = typeof(AnimalFoodStore),
                        ResourceTypeName = FeedTypeName,
                        ActivityModel = this,
                        Category = TransactionCategory,
                        RelatesToResource = this.PredictedHerdName,
                        AdditionalDetails = foodPacket
                    }
                };
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override LabourRequiredArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            IEnumerable<Ruminant> herd = CurrentHerd(false);
            int head = 0;
            double adultEquivalents = 0;
            double daysNeeded = 0;
            double numberUnits = 0;

            if (requirement.UnitType == LabourUnitType.perHead || requirement.UnitType == LabourUnitType.perAE)
                foreach (IFilterGroup child in Children.OfType<RuminantFeedGroup>())
                {
                    var selectedIndividuals = child.Filter(herd).GroupBy(i => 1).Select(a => new {
                        Count = (requirement.UnitType == LabourUnitType.perHead) ? a.Count() : 0,
                        TotalAE = (requirement.UnitType == LabourUnitType.perAE) ? a.Sum(b => b.Weight) : 0
                    }).ToList();

                    if (selectedIndividuals.Count > 0)
                    {
                        head += selectedIndividuals.FirstOrDefault().Count;
                        adultEquivalents += selectedIndividuals.FirstOrDefault().TotalAE;
                    }
                }

            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    numberUnits = head / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                        numberUnits = Math.Ceiling(numberUnits);

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perAE:
                    numberUnits = adultEquivalents / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                        numberUnits = Math.Ceiling(numberUnits);

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perKg:
                    daysNeeded = feedEstimated * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perUnit:
                    numberUnits = feedEstimated / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                        numberUnits = Math.Ceiling(numberUnits);

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new LabourRequiredArgs(daysNeeded, TransactionCategory, this.PredictedHerdName);
        }

        /// <inheritdoc/>
        public override void AdjustResourcesNeededForActivity()
        {
            // labour shortfall if any
            double labourLimit = this.LabourLimitProportion;
            overfeedProportion = 0;

            // TODO: adjust if multiple animal food stores included in future.
            // FirstOrDefault() is still known to be food store request. After this call it will be last in list with wasted and excess at start of list
            ResourceRequest item = ResourceRequestList.Where(a => a.ResourceType == typeof(AnimalFoodStore)).FirstOrDefault();

            if(item != null)
            {
                //add limits to amout collected based on labour shortfall
                item.Required *= labourLimit;

                // account for any wastage
                // removed from food resource provided and then will be handled if required if less than provided in next section (DoActivity).
                if (ProportionTramplingWastage > 0)
                {
                    double wasted = Math.Min(item.Available, item.Required) * ProportionTramplingWastage;
                    if (wasted > 0)
                    {
                        ResourceRequest wastedRequest = new ResourceRequest()
                        {
                            AllowTransmutation = false,
                            Required = wasted,
                            Available = wasted,
                            Resource = item.Resource,
                            ResourceType = typeof(AnimalFoodStore),
                            ResourceTypeName = item.ResourceTypeName,
                            ActivityModel = this,
                            Category = "Wastage",
                            RelatesToResource = this.PredictedHerdName,
                        };
                        ResourceRequestList.Insert(0, wastedRequest);
                        item.Required -= wasted;
                        // adjust the food known available for the actual feed
                        item.Available -= wasted;
                    }
                }

                // report any excess fed above feed needed to fill animals intake (including potential multiplier if required for overfeeding)
                double excess = 0;
                if (Math.Min(item.Available, item.Required) >= feedToOverSatisfy)
                {
                    excess = Math.Min(item.Available, item.Required) - feedToOverSatisfy;
                    if(feedToOverSatisfy > feedToSatisfy)
                        overfeedProportion = 1;
                }
                else if(feedToOverSatisfy > feedToSatisfy && Math.Min(item.Available, item.Required) > feedToSatisfy)
                    overfeedProportion = (Math.Min(item.Available, item.Required) - feedToSatisfy) / (feedToOverSatisfy - feedToSatisfy);
                if (excess > 0)
                {
                    ResourceRequest excessRequest = new ResourceRequest()
                    {
                        AllowTransmutation = false,
                        Required = excess,
                        Available = excess,
                        Resource = item.Resource,
                        ResourceType = typeof(AnimalFoodStore),
                        ResourceTypeName = item.ResourceTypeName,
                        ActivityModel = this,
                        Category = "Overfed wastage",
                        RelatesToResource = this.PredictedHerdName
                    };
                    ResourceRequestList.Insert(0, excessRequest);
                    item.Required -= excess;
                    item.Available -= excess;
                }
            }
            return;
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            IEnumerable<Ruminant> herd = CurrentHerd(false);
            if (herd != null && herd.Any())
            {
                double feedLimit = 0.0;

                ResourceRequest feedRequest = ResourceRequestList.Where(a => a.ResourceType == typeof(AnimalFoodStore)).LastOrDefault();
                FoodResourcePacket details = new FoodResourcePacket();
                if (feedRequest != null)
                {
                    details = feedRequest.AdditionalDetails as FoodResourcePacket;
                    feedLimit = Math.Min(1.0, feedRequest.Provided / feedRequest.Required);
                }

                // feed animals
                if(feedRequest == null | (feedRequest?.Required == 0 | feedRequest?.Available == 0) | APSIM.Shared.Utilities.MathUtilities.FloatsAreEqual(feedLimit, 0.0))
                {
                    Status = ActivityStatus.NotNeeded;
                    return;
                }

                // get list from filters
                foreach (var child in Children.OfType<FilterGroup<Ruminant>>())
                {
                    double value = 0;
                    if (child is RuminantFeedGroup rfg)
                        value = rfg.Value;
                    else if (child is RuminantFeedGroupMonthly rfgm)
                        value = rfgm.MonthlyValues[clock.Today.Month - 1];
                    else
                        continue;

                    foreach (Ruminant ind in child.Filter(herd))
                    {
                        switch (FeedStyle)
                        {
                            case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                            case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                                details.Amount = ((ind.PotentialIntake * (usingPotentialIntakeMultiplier ? ind.BreedParams.OverfeedPotentialIntakeModifier : 1)) - ind.Intake);
                                details.Amount *= feedLimit;
                                break;
                            case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                                details.Amount = value * 30.4;
                                details.Amount *= feedLimit;
                                break;
                            case RuminantFeedActivityTypes.ProportionOfWeight:
                                details.Amount = value * ind.Weight * 30.4;
                                details.Amount *= feedLimit;
                                break;
                            case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                                details.Amount = value * ind.PotentialIntake;
                                details.Amount *= feedLimit;
                                break;
                            case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                                details.Amount = value * (ind.PotentialIntake - ind.Intake); 
                                details.Amount *= feedLimit;
                                break;
                            default:
                                throw new Exception($"FeedStyle [{FeedStyle}] is not supported in [a={Name}]");
                        }
                        // check amount meets intake limits
                        if (usingPotentialIntakeMultiplier)
                            if (details.Amount > (ind.PotentialIntake + (Math.Max(0,ind.BreedParams.OverfeedPotentialIntakeModifier-1)*overfeedProportion*ind.PotentialIntake)) - ind.Intake)
                                details.Amount = (ind.PotentialIntake + (Math.Max(0, ind.BreedParams.OverfeedPotentialIntakeModifier - 1) * overfeedProportion * ind.PotentialIntake)) - ind.Intake;

                        ind.AddIntake(details);
                    }
                }
                SetStatusSuccess();
            }
            else
                Status = ActivityStatus.NotNeeded;
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

            if (FindAllChildren<RuminantFeedGroup>().Count() + this.FindAllChildren<RuminantFeedGroupMonthly>().Count() == 0)
            {
                string[] memberNames = new string[] { "Ruminant feed group" };
                results.Add(new ValidationResult("At least one [f=RuminantFeedGroup] or [f=RuminantFeedGroupMonthly] is required to define the animals and amount fed", memberNames));
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
                htmlWriter.Write("\r\n<div class=\"activityentry\">Feed ruminants ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(FeedTypeName, "Feed not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write("</div>");
                if (ProportionTramplingWastage > 0)
                    htmlWriter.Write("\r\n<div class=\"activityentry\"> <span class=\"setvalue\">" + (ProportionTramplingWastage).ToString("0.##%") + "</span> is lost through trampling</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
