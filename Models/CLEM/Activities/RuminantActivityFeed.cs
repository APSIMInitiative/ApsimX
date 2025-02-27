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
using APSIM.Shared.Utilities;
using StdUnits;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant feed activity</summary>
    /// <summary>This activity provides food to specified ruminants based on a feeding style</summary>
    /// <version>1.1</version>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Feed ruminants by a feeding style.")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 4, "Added smart feeding switch to stop feeding when animals are satisfied and avoid overfeed wastage")]
    [Version(1, 0, 3, "User defined PotentialIntake modifer and reporting of trampling and overfed wastage in ledger")]
    [Version(1, 0, 2, "Manages feeding whole herd a specified daily amount or proportion of available feed")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantFeed.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantActivityFeed : CLEMRuminantActivityBase, IValidatableObject, IHandlesActivityCompanionModels
    {
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;
        private int numberToDo;
        private int numberToSkip;
        private double amountToDo;
        private double amountToSkip;
        private double wasted;
        private double excessFed;
        private IEnumerable<Ruminant> uniqueIndividuals;
        private IEnumerable<RuminantGroup> filterGroups;
        private double feedEstimated = 0;
        private double excessreduction = 1;

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
        [Description("Feeding style to use")]
        [Required]
        public RuminantFeedActivityTypes FeedStyle { get; set; } = RuminantFeedActivityTypes.SpecifiedDailyAmount;

        /// <summary>
        /// Stop feeding when animals are satisfied
        /// </summary>
        [Description("Stop feeding when all satisfied")]
        [Core.Display(VisibleCallback = "RestrictIntakeAllowed")]
        [Required]
        public bool StopFeedingWhenSatisfied { get; set; }

        /// <summary>
        /// Force feed the amount specified (do not obey potential intake limits)
        /// </summary>
        [Description("Force feed all provided")]
        [Core.Display(VisibleCallback = "ForceIntakeAllowed")]
        [Required]
        public bool ForceFeed { get; set; }

        /// <summary>
        /// Feed resource
        /// </summary>
        [JsonIgnore]
        public IResourceType FeedResource { get; set; }

        /// <summary>
        /// Feed quality
        /// </summary>
        [JsonIgnore]
        public IFeed FeedDetails { get; set; }

        /// <summary>
        /// The list of individuals remaining to be fed in the current timestep
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Ruminant> IndividualsToBeFed { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityFeed()
        {
            SetDefaults();
        }

        /// <summary>
        /// Determines if style needs the restrict intake property displayed
        /// </summary>
        /// <returns>True or false</returns>
        public bool RestrictIntakeAllowed() => FeedStyle == RuminantFeedActivityTypes.ProportionOfFeedAvailable || FeedStyle == RuminantFeedActivityTypes.SpecifiedDailyAmount || FeedStyle == RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual || FeedStyle == RuminantFeedActivityTypes.ProportionOfWeight;

        /// <summary>
        /// Determines if style needs the restrict intake property displayed
        /// </summary>
        /// <returns>True or false</returns>
        public bool ForceIntakeAllowed() => FeedStyle == RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual || FeedStyle == RuminantFeedActivityTypes.ProportionOfWeight;


        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "RuminantFeedGroup":
                case "RuminantFeedGroupMonthly":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() { "Feed provided" }
                        );
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                            "Number fed",
                            "Feed provided"
                        },
                        measures: new List<string>() {
                            "fixed",
                            "per head",
                            "per kg feed"
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get all ui tree herd filters that relate to this activity
            InitialiseHerd(true, true);
            filterGroups = GetCompanionModelsByIdentifier<RuminantFeedGroup>(true, false);

            if (ForceIntakeAllowed() == false)
                ForceFeed = false;
            if (RestrictIntakeAllowed() == false)
                StopFeedingWhenSatisfied = false;

            // locate FeedType resource
            FeedDetails = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IFeed;
            FeedResource = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <inheritdoc/>
        public override void PrepareForTimestep()
        {
            numberToDo = 0;
            numberToSkip = 0;
            amountToDo = 0;
            amountToSkip = 0;
            wasted = 0;
            excessFed = 0;
            IEnumerable<Ruminant> herd = GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.AllOnFarm);
            uniqueIndividuals = GetUniqueIndividuals<Ruminant>(filterGroups.OfType<RuminantFeedGroup>(), herd);
            numberToDo = uniqueIndividuals?.Count() ?? 0;
            IndividualsToBeFed = uniqueIndividuals;
            feedEstimated = 0;
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            Status = ActivityStatus.NotNeeded;
            feedEstimated = filterGroups.OfType<RuminantFeedGroup>().Sum(a => a.CurrentResourceRequest.Required);

            foreach (var valueToSupply in valuesForCompanionModels)
            {
                int number = numberToDo;

                switch (valueToSupply.Key.type)
                {
                    case "RuminantFeedGroup":
                        valuesForCompanionModels[valueToSupply.Key] = feedEstimated;
                        break;
                    case "LabourRequirement":
                    case "ActivityFee":
                        switch (valueToSupply.Key.identifier)
                        {
                            case "Number fed":
                                switch (valueToSupply.Key.unit)
                                {
                                    case "fixed":
                                        valuesForCompanionModels[valueToSupply.Key] = 1;
                                        break;
                                    case "per head":
                                        valuesForCompanionModels[valueToSupply.Key] = number;
                                        break;
                                    default:
                                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                                }
                                break;
                            case "Feed provided":
                                switch (valueToSupply.Key.unit)
                                {
                                    case "fixed":
                                        valuesForCompanionModels[valueToSupply.Key] = 1;
                                        break;
                                    case "per kg fed":
                                        amountToDo = feedEstimated;
                                        valuesForCompanionModels[valueToSupply.Key] = feedEstimated;
                                        break;
                                    default:
                                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                                }
                                break;
                            default:
                                throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key));
                        }
                        break;
                    default:
                        throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key));
                }
            }
            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // find shortfall by identifiers as these may have different influence on outcome
                var numberShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == "Number fed").FirstOrDefault();
                if (numberShort != null)
                {
                    string warn = $"Resource shortfalls reduced the number of animals fed in [a={NameWithParent}] based on specified [ShortfallAffectsActivity] set to true for identifier [Number fed].{Environment.NewLine}The individuals fed will be restricted, but the model does not currenty adjust the amount of feed handled to match the reduced number of individuals";
                    Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);

                    numberToSkip = Convert.ToInt32(numberToDo * (1 - numberShort.Available / numberShort.Required));
                }

                var amountShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == "Feed provided").FirstOrDefault();
                if (amountShort != null)
                    amountToSkip = Convert.ToInt32(amountToDo * (1 - amountShort.Available / amountShort.Required));

                if (numberToDo == numberToSkip)
                {
                    amountToDo = 0;
                }
                Status = ActivityStatus.Partial;
            }
            
            // number and kg based shortfalls of labour and finance etc will affect lower feeding groups
            int numberNotAllowed = numberToSkip;
            double totalfed = 0;
            excessreduction = 1;

            foreach (var iChild in filterGroups.OfType<RuminantFeedGroup>().Reverse())
            {
                int numberPresent = iChild.CurrentIndividualsToFeed.Count;
                if (numberNotAllowed > 0)
                {
                    // reduce individuals in group
                    int numberToRemove = Math.Min(numberPresent, numberNotAllowed);
                    numberPresent -= numberToRemove;    
                    numberNotAllowed -= numberToRemove;

                    // calculate feed not needed for removed individuals
                    double previouslyRequired = iChild.CurrentResourceRequest.Required;

                    iChild.CurrentIndividualsToFeed = iChild.CurrentIndividualsToFeed.SkipLast(numberToRemove).ToList();
                    iChild.UpdateCurrentFeedDemand(this);

                    // remove from amountToSkip 
                    amountToSkip -= previouslyRequired;
                    Status = ActivityStatus.Partial;
                }
                if(MathUtilities.IsPositive(amountToSkip))
                {
                    // still need to reduce amount shortfalls from $ or labour
                    double amountToRemove = Math.Min(amountToSkip, iChild.CurrentResourceRequest.Available);
                    iChild.CurrentResourceRequest.Available -= amountToRemove;
                    amountToSkip -= amountToRemove;
                    Status = ActivityStatus.Partial;
                }

                if (MathUtilities.IsPositive(ProportionTramplingWastage))
                {
                    double wastedByGroup = Math.Min(iChild.CurrentResourceRequest.Available, iChild.CurrentResourceRequest.Required) * ProportionTramplingWastage;
                    wasted += wastedByGroup;
                    iChild.CurrentResourceRequest.Available -= wastedByGroup;
                    iChild.CurrentResourceRequest.Required -= wastedByGroup;
                }
                // calculate excess fed
                if (MathUtilities.IsGreaterThan(Math.Min(iChild.CurrentResourceRequest.Available, iChild.CurrentResourceRequest.Required), iChild.FeedToSatisfy))
                {
                    double excess = Math.Min(iChild.CurrentResourceRequest.Available, iChild.CurrentResourceRequest.Required) - iChild.FeedToSatisfy;
                    totalfed += Math.Min(iChild.CurrentResourceRequest.Available, iChild.CurrentResourceRequest.Required);
                    excessFed += excess;
                    if (!ForceFeed)
                    {
                        iChild.CurrentResourceRequest.Available -= excess;
                        iChild.CurrentResourceRequest.Required -= excess;
                    }
                }

                // adjust for, and report, wastage
                if (MathUtilities.IsPositive(wasted))
                {
                    ResourceRequest wastedRequest = new()
                    {
                        AllowTransmutation = false,
                        Required = wasted,
                        Available = wasted,
                        Resource = FeedResource,
                        ResourceType = typeof(AnimalFoodStore),
                        ResourceTypeName = FeedTypeName,
                        ActivityModel = this,
                        Category = $"{TransactionCategory}.Wastage",
                        RelatesToResource = PredictedHerdNameToDisplay,
                    };
                    ResourceRequestList.Insert(0, wastedRequest);
                }

                // report any excess fed above feed needed to fill animals intake (including potential multiplier if required for overfeeding)
                if (MathUtilities.IsPositive(excessFed))
                {
                    if(ForceFeed)
                    {
                        string warn = $"Individuals were forced to eat more than required by [a={NameWithParent}].";
                        Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                    }
                    else
                    {
                        excessreduction = 1 - excessFed / totalfed;
                        ResourceRequest excessRequest = new()
                        {
                            AllowTransmutation = false,
                            Required = excessFed,
                            Available = excessFed,
                            Resource = FeedResource,
                            ResourceType = typeof(AnimalFoodStore),
                            ResourceTypeName = FeedTypeName,
                            ActivityModel = this,
                            Category = $"{TransactionCategory}.Overfed wastage",
                            RelatesToResource = PredictedHerdNameToDisplay
                        };
                        ResourceRequestList.Insert(0, excessRequest);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            double overfed = 0;
            int numberFed = 0;
            foreach (var iChild in filterGroups.OfType<RuminantFeedGroup>())
            {
                if (iChild.CurrentResourceRequest != null)
                {
                    numberFed += iChild.CurrentIndividualsToFeed.Count;
                    double feedLimit = Math.Min(1.0, iChild.CurrentResourceRequest.Provided / iChild.CurrentResourceRequest.Required);

                    double totalWeight = 0;
                    if(FeedStyle == RuminantFeedActivityTypes.SpecifiedDailyAmount || FeedStyle == RuminantFeedActivityTypes.ProportionOfFeedAvailable)
                    {  
                        totalWeight = iChild.CurrentIndividualsToFeed.Sum(a => a.Weight.Live);
                    }

                    FoodResourcePacket details = iChild.CurrentResourceRequest.AdditionalDetails as FoodResourcePacket;

                    foreach (Ruminant ind in iChild.CurrentIndividualsToFeed)
                    {
                        switch (FeedStyle)
                        {
                            case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                            case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                                details.Amount = ind.Intake.SolidsDaily.RequiredForTimeStep(events.Interval);
                                details.Amount *= feedLimit; // shortfall in feed available.
                                details.Amount *= ind.Weight.Live /totalWeight;  // individual's proportion of the feed available.
                                break;
                            case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                                details.Amount = iChild.CurrentValue * events.Interval;
                                details.Amount *= feedLimit;
                                break;
                            case RuminantFeedActivityTypes.ProportionOfWeight:
                                details.Amount = iChild.CurrentValue * ind.Weight.Live * events.Interval;
                                details.Amount *= feedLimit;
                                break;
                            case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                                details.Amount = iChild.CurrentValue * ind.Intake.SolidsDaily.ExpectedForTimeStep(events.Interval);
                                details.Amount *= feedLimit;
                                break;
                            case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                                details.Amount = iChild.CurrentValue * ind.Intake.SolidsDaily.RequiredForTimeStep(events.Interval);
                                details.Amount *= feedLimit;
                                break;
                            default:
                                throw new Exception($"FeedStyle [{FeedStyle}] is not supported in [a={Name}]");
                        }
                        details.Amount *= excessreduction;
                        // convert to daily intake for the ruminant intake store. 
                        details.Amount /= (double)events.Interval;
                        // try to feed. excess will be returned.
                        overfed += ind.Intake.AddFeed(details, ForceFeed);
                    }
                }
            }
            if (numberToDo > 0)
                SetStatusSuccessOrPartial(numberFed != numberToDo);
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (filterGroups != null && filterGroups.Where(a => a.GetType() != typeof(RuminantFeedGroup) && a.GetType() != typeof(RuminantFeedGroupMonthly)).Any())
            {
                string warn = $"[a=RuminantActivityFeed] [{NameWithParent}] only accepts Resource groups of the type [f=RuminantFeedGroup] or [f=RuminantFeedGroupMonthly].{Environment.NewLine}All other groups will be ignored.";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);
                filterGroups = filterGroups.Where(a => a.GetType() == typeof(RuminantFeedGroup) || a.GetType() == typeof(RuminantFeedGroupMonthly));
            }

            // check that all children with proportion of feed available do not exceed 1
            if(FeedStyle == RuminantFeedActivityTypes.ProportionOfFeedAvailable)
            {
                double propOfFeed = FindAllChildren<RuminantFeedGroup>().Sum(a => a.Value);
                if(MathUtilities.IsGreaterThan(propOfFeed, 1.0))
                {
                    yield return new ValidationResult($"The sum of Proportions of total feed available excceds 1 across all [RuminantFeedGroups] in [a={Name}].{Environment.NewLine}Choose a different feeding style or ensure the sum of proportions specified do not exceed 1 when using ProportionOfFeedAvailable feeding style", new string[] { "Total proportion exceeds 1" });
                }
            }
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">Feed ruminants ");
            htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(FeedTypeName, "Feed not set", HTMLSummaryStyle.Resource));
            htmlWriter.Write("</div>");
            if (ProportionTramplingWastage > 0)
                htmlWriter.Write($"\r\n<div class=\"activityentry\"> <span class=\"setvalue\">{ProportionTramplingWastage:0.##%}</span> is lost through trampling</div>");
            return htmlWriter.ToString();
        } 
        #endregion
    }
}
