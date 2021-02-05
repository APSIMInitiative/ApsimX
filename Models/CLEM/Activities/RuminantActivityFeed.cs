using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Models.Core.Attributes;
using MathNet.Numerics;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant feed activity</summary>
    /// <summary>This activity provides food to specified ruminants based on a feeding style</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs ruminant feeding based upon the current herd filtering and a feeding style.")]
    [Version(1, 0, 4, "Added smart feeding switch to stop feeding when animals are satisfied and avoid overfeed wastage")]
    [Version(1, 0, 3, "User defined PotentialIntake modifer and reporting of trampling and overfed wastage in ledger")]
    [Version(1, 0, 2, "Manages feeding whole herd a specified daily amount or proportion of available feed")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantFeed.htm")]
    public class RuminantActivityFeed : CLEMRuminantActivityBase, IValidatableObject
    {
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Name of Feed to use (with Resource Group name appended to the front [separated with a '.'])
        /// eg. AnimalFoodStore.RiceStraw
        /// </summary>
        [Description("Feed to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] {typeof(AnimalFoodStore), typeof(HumanFoodStore)} )]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Feed type required")]
        public string FeedTypeName { get; set; }

        /// <summary>
        /// Proportion wasted (e.g. trampling, 0 = feed trough present)
        /// </summary>
        [Description("Proportion wasted (e.g. trampling, 0 = feed trough present)")]
        [Required, Proportion]
        public double ProportionTramplingWastage { get; set; }

        /// <summary>
        /// Feed type
        /// </summary>
        [JsonIgnore]
        public IFeedType FeedType { get; set; }

        // amount requested
        private double feedEstimated = 0;
        // amount actually needed to satisfy animals
        private double feedToSatisfy = 0;
        // amount actually needed to satisfy animals allowing for overfeeding
        private double feedToOverSatisfy = 0;
        // does this feeding style need a potential intake modifier
        private bool usingPotentialintakeMultiplier = false;

        private double overfeedProportion = 1;

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
        /// Constructor
        /// </summary>
        public RuminantActivityFeed()
        {
            this.SetDefaults();
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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get all ui tree herd filters that relate to this activity
            this.InitialiseHerd(true, true);

            // locate FeedType resource
            FeedType = Resources.GetResourceItem(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IFeedType;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<Ruminant> herd = CurrentHerd(false);
            feedEstimated = 0;
            feedToSatisfy = 0;
            feedToOverSatisfy = 0;

            // get list from filters
            foreach (Model child in this.Children.Where(a => a.GetType().ToString().Contains("RuminantFeedGroup")))
            {
                var selectedIndividuals = herd.Filter(child);

                switch (FeedStyle)
                {
                    case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                    case RuminantFeedActivityTypes.ProportionOfWeight:
                    case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                    case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                        usingPotentialintakeMultiplier = true;
                        break;
                }

                // get the amount that can be eaten. Does not account for individuals in multiple filters
                // accounts for some feeding style allowing overeating to the user declared value in ruminant 
                feedToSatisfy += selectedIndividuals.Sum(a => a.PotentialIntake - a.Intake);
                feedToOverSatisfy += selectedIndividuals.Sum(a => a.PotentialIntake * (usingPotentialintakeMultiplier ? a.BreedParams.OverfeedPotentialIntakeModifier : 1) - a.Intake);

                double value = 0;
                if (child is RuminantFeedGroup)
                {
                    value = (child as RuminantFeedGroup).Value;
                }
                else
                {
                    value = (child as RuminantFeedGroupMonthly).MonthlyValues[Clock.Today.Month - 1];
                }

                if (FeedStyle == RuminantFeedActivityTypes.SpecifiedDailyAmount)
                {
                    feedEstimated += value * 30.4;
                }
                else if(FeedStyle == RuminantFeedActivityTypes.ProportionOfFeedAvailable)
                {
                    feedEstimated += value * FeedType.Amount;
                }
                else
                {
                    foreach (Ruminant ind in selectedIndividuals)
                    {
                        switch (FeedStyle)
                        {
                            case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                                feedEstimated += value * 30.4;
                                break;
                            case RuminantFeedActivityTypes.ProportionOfWeight:
                                feedEstimated += value * ind.Weight * 30.4;
                                break;
                            case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                                feedEstimated += value * ind.PotentialIntake;
                                break;
                            case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                                feedEstimated += value * (ind.PotentialIntake - ind.Intake);
                                break;
                            default:
                                throw new Exception(String.Format("FeedStyle {0} is not supported in {1}", FeedStyle, this.Name));
                        }
                    }
                }
            }

            if(StopFeedingWhenSatisfied)
            {
                // restrict to max intake permitted by individuals and avoid overfeed wastage
                feedEstimated = Math.Min(feedEstimated, Math.Max(feedToOverSatisfy, feedToSatisfy));
            }

            if (feedEstimated > 0)
            {
                // FeedTypeName includes the ResourceGroup name eg. AnimalFoodStore.FeedItemName
                string feedItemName = FeedTypeName.Split('.').Last();
                return new List<ResourceRequest>()
                {
                    new ResourceRequest()
                    {
                        AllowTransmutation = true,
                        Required = feedEstimated,
                        ResourceType = typeof(AnimalFoodStore),
                        ResourceTypeName = feedItemName,
                        ActivityModel = this,
                        Category = "Feed",
                        RelatesToResource = this.PredictedHerdName
                    }
                };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            List<Ruminant> herd = CurrentHerd(false);
            int head = 0;
            double adultEquivalents = 0;
            foreach (Model child in this.Children.Where(a => a.GetType().ToString().Contains("RuminantFeedGroup")))
            {
                var subherd = herd.Filter(child).ToList();
                head += subherd.Count();
                adultEquivalents += subherd.Sum(a => a.AdultEquivalent);
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
                case LabourUnitType.perKg:
                    daysNeeded = feedEstimated * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perUnit:
                    numberUnits = feedEstimated / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }
                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Feed", this.PredictedHerdName);
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
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
                            ResourceType = typeof(AnimalFoodStore),
                            ResourceTypeName = item.ResourceTypeName,
                            ActivityModel = this,
                            Category = "Wastage",
                            RelatesToResource = this.PredictedHerdName
                        };
                        ResourceRequestList.Insert(0, wastedRequest);
                        item.Required -= wasted;
                        // adjust the food known available for the actual feed
                        item.Available -= wasted;
                    }
                }

                // report any excess fed above feed needed to fill animals itake (including potential multiplier if required for overfeeding)
                double excess = 0;
                if (Math.Min(item.Available, item.Required) >= feedToOverSatisfy)
                {
                    excess = Math.Min(item.Available, item.Required) - feedToOverSatisfy;
                    if(feedToOverSatisfy > feedToSatisfy)
                    {
                        overfeedProportion = 1;
                    }
                }
                else if(feedToOverSatisfy > feedToSatisfy && Math.Min(item.Available, item.Required) > feedToSatisfy)
                {
                    overfeedProportion = (Math.Min(item.Available, item.Required) - feedToSatisfy) / (feedToOverSatisfy - feedToSatisfy);
                }
                if (excess > 0)
                {
                    ResourceRequest excessRequest = new ResourceRequest()
                    {
                        AllowTransmutation = false,
                        Required = excess,
                        Available = excess,
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

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            List<Ruminant> herd = CurrentHerd(false);
            if (herd != null && herd.Count > 0)
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
                if(feedRequest == null || (feedRequest.Required == 0 | feedRequest.Available == 0))
                {
                    Status = ActivityStatus.NotNeeded;
                    return;
                }

                // get list from filters
                foreach (Model child in this.Children.Where(a => a.GetType().ToString().Contains("RuminantFeedGroup")))
                {
                    double value = 0;
                    if (child is RuminantFeedGroup)
                    {
                        value = (child as RuminantFeedGroup).Value;
                    }
                    else
                    {
                        value = (child as RuminantFeedGroupMonthly).MonthlyValues[Clock.Today.Month - 1];
                    }

                    foreach (Ruminant ind in herd.Filter(child))
                    {
                        switch (FeedStyle)
                        {
                            case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                            case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                                details.Amount = ((ind.PotentialIntake * (usingPotentialintakeMultiplier ? ind.BreedParams.OverfeedPotentialIntakeModifier : 1)) - ind.Intake);
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
                                throw new Exception("Feed style used [" + FeedStyle + "] not implemented in [" + this.Name + "]");
                        }
                        // check amount meets intake limits
                        if (usingPotentialintakeMultiplier)
                        {
                            if (details.Amount > (ind.PotentialIntake + (Math.Max(0,ind.BreedParams.OverfeedPotentialIntakeModifier-1)*overfeedProportion*ind.PotentialIntake)) - ind.Intake)
                            {
                                details.Amount = (ind.PotentialIntake + (Math.Max(0, ind.BreedParams.OverfeedPotentialIntakeModifier - 1) * overfeedProportion * ind.PotentialIntake)) - ind.Intake;
                            }
                        }
                        ind.AddIntake(details);

                    }
                }
                SetStatusSuccess();
            }
            else
            {
                Status = ActivityStatus.NotNeeded;
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

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Feed ruminants ");

                if (FeedTypeName == null || FeedTypeName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">[Feed TYPE NOT SET]</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + FeedTypeName + "</span>");
                }
                htmlWriter.Write("</div>");

                if (ProportionTramplingWastage > 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\"> <span class=\"setvalue\">" + (ProportionTramplingWastage).ToString("0.##%") + "</span> is lost through trampling</div>");
                }
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
