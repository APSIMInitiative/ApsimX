using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual ruminants for feeding
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityFeed))]
    [Description("Set feeding value for specified individual ruminants")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantFeedGroup.htm")]
    public class RuminantFeedGroup : RuminantGroup
    {
        [Link]
        private Summary summary = null;

        private RuminantActivityFeed feedActivityParent;
        private ResourceRequest currentFeedRequest;
        private bool usingPotentialIntakeMultiplier = false;
        private List<Ruminant> individualsToBeFed;
        private bool countNeeded = false;
        private bool weightNeeded = false;

        /// <summary>
        /// Value to supply for each month
        /// </summary>
        [Description("Value to supply")]
        [GreaterThanValue(0)]
        public double Value { get; set; }

        /// <summary>
        /// Payment style
        /// </summary>
        public override string Measure
        {
            get { return "Feed provided"; }
            set {; }
        }

        /// <summary>
        /// Get the value for the current month
        /// </summary>
        [JsonIgnore]
        public virtual double CurrentValue { get { return Value; } }

        /// <summary>
        /// The current feed resource request calculated for this feed group
        /// </summary>
        [JsonIgnore]
        public ResourceRequest CurrentResourceRequest { get { return currentFeedRequest; } }

        /// <summary>
        /// The current individuals being fed for this feed group
        /// </summary>
        [JsonIgnore]
        public List<Ruminant> CurrentIndividualsToFeed
        {
            get
            {
                return individualsToBeFed;
            }
            set
            {
                individualsToBeFed = value;
            }
        }

        /// <inheritdoc/>
        [Description("Category for transactions")]
        [Models.Core.Display(Order = 500)]
        public string TransactionCategory { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantFeedGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            feedActivityParent = FindAncestor<RuminantActivityFeed>();

            RuminantActivityFeed feedParent = Parent as RuminantActivityFeed;
            switch (feedParent.FeedStyle)
            {
                case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                    usingPotentialIntakeMultiplier = true;
                    countNeeded = true;
                    break;
                case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                    usingPotentialIntakeMultiplier = true;
                    countNeeded = true;
                    break;
                case RuminantFeedActivityTypes.ProportionOfWeight:
                    usingPotentialIntakeMultiplier = true;
                    weightNeeded = true;
                    break;
                case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                    break;
                case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                    break;
                case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                    usingPotentialIntakeMultiplier = true;
                    countNeeded = true;
                    break;
                default:
                    string error = $"FeedStyle [{feedParent.FeedStyle}] is not supported by [f=RuminantFeedGroup] in [a={NameWithParent}]";
                    summary.WriteMessage(this, error, MessageType.Error);
                    break;
            }

            // warning that any take filters will be ignored.
            if (FindAllDescendants<TakeFromFiltered>().Any())
            {
                string warnMessage = $"The [TakeFiltered] component of [f={this.NameWithParent}] is not valid for [OtherAnimalFeedGroup].Take or Skip will be ignored.";
                Warnings.CheckAndWrite(warnMessage, Summary, this, MessageType.Warning);
            }

        }

        /// <inheritdoc/>
        public override void PrepareForTimestep()
        {
            // remember individuals and request details for later adjustment based on shortfalls
            individualsToBeFed = Filter(feedActivityParent.IndividualsToBeFed).ToList();
            currentFeedRequest = null;

            // create food resource packet with details
            FoodResourcePacket foodPacket = new FoodResourcePacket()
            {
                DMD = feedActivityParent.FeedType.DMD,
                PercentN = feedActivityParent.FeedType.Nitrogen
            };

            currentFeedRequest = new ResourceRequest()
            {
                AllowTransmutation = true,
                Resource = feedActivityParent.FeedType,
                ResourceType = typeof(AnimalFoodStore),
                ResourceTypeName = feedActivityParent.FeedTypeName,
                ActivityModel = Parent as CLEMActivityBase,
                Category = (Parent as CLEMActivityBase).TransactionCategory,
                RelatesToResource = feedActivityParent.PredictedHerdNameToDisplay,
                AdditionalDetails = foodPacket
            };

            UpdateCurrentFeedDemand(feedActivityParent);

            // remove fed individuals from temp list to avoid double handling of an individual in the parent activity
            feedActivityParent.IndividualsToBeFed = feedActivityParent.IndividualsToBeFed.Skip(individualsToBeFed.Count);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double activityMetric)
        {
            return new List<ResourceRequest> { currentFeedRequest };
        }

        /// <summary>
        /// Method to update the feed request details based on the current animals to feed
        /// </summary>
        public void UpdateCurrentFeedDemand(RuminantActivityFeed feedActivityParent)
        {
            double value = CurrentValue;
            double feedToSatisfy = 0;
            double feedToOverSatisfy = 0;
            double feedNeeded = 0;

            var selectedIndividuals = Filter(individualsToBeFed).GroupBy(i => 1).Select(a => new
            {
                Count = countNeeded ? a.Count() : 0,
                Weight = weightNeeded ? a.Sum(b => b.Weight) : 0,
                Intake = a.Sum(b => b.Intake),
                PotentialIntake = a.Sum(b => b.PotentialIntake),
                IntakeMultiplier = usingPotentialIntakeMultiplier ? a.FirstOrDefault().BreedParams.OverfeedPotentialIntakeModifier : 1
            }).FirstOrDefault();

            if (selectedIndividuals != null)
            {
                switch (feedActivityParent.FeedStyle)
                {
                    case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                        feedNeeded = value * 30.4;
                        break;
                    case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                        feedNeeded = (value * 30.4) * selectedIndividuals.Count;
                        break;
                    case RuminantFeedActivityTypes.ProportionOfWeight:
                        feedNeeded = value * selectedIndividuals.Weight * 30.4;
                        break;
                    case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                        feedNeeded = value * selectedIndividuals.PotentialIntake;
                        break;
                    case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                        feedNeeded = value * (selectedIndividuals.PotentialIntake - selectedIndividuals.Intake);
                        break;
                    case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                        feedNeeded = value * feedActivityParent.FeedType.Amount;
                        break;
                    default:
                        break;
                }

                // get the amount that can be eaten by individuals available meeting this group filter
                // individuals in multiple filters will be considered once
                // accounts for some feeding style allowing overeating to the user declared value in ruminant 

                feedToSatisfy = selectedIndividuals.PotentialIntake - selectedIndividuals.Intake;
                feedToOverSatisfy = selectedIndividuals.PotentialIntake * selectedIndividuals.IntakeMultiplier - selectedIndividuals.Intake;

                if (feedActivityParent.StopFeedingWhenSatisfied)
                    // restrict to max intake permitted by individuals and avoid overfeed wastage
                    feedNeeded = Math.Min(feedNeeded, Math.Max(feedToOverSatisfy, feedToSatisfy));

                (currentFeedRequest.AdditionalDetails as FoodResourcePacket).Amount = feedNeeded;
                currentFeedRequest.Required = feedNeeded;
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (this.Parent.GetType() != typeof(RuminantActivityFeed))
                {
                    return "<div class=\"warningbanner\">This Ruminant Feed Group must be placed beneath a Ruminant Activity Feed component</div>";
                }

                RuminantFeedActivityTypes ft = (this.Parent as RuminantActivityFeed).FeedStyle;
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                switch (ft)
                {
                    case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                    case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                        htmlWriter.Write($"<span class=\"{((Value <= 0) ? "errorlink" : "setvalue")}\">{Value} kg</span>");
                        break;
                    case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                    case RuminantFeedActivityTypes.ProportionOfWeight:
                    case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                    case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                        if (Value != 1)
                        {
                            htmlWriter.Write($"<span class=\"{((Value <= 0) ? "errorlink" : "setvalue")}\">{Value.ToString("0.##%")}</span>");
                        }
                        break;
                    default:
                        break;
                }

                string starter = " of ";
                if (Value == 1)
                {
                    starter = "The ";
                }

                bool overfeed = false;
                htmlWriter.Write("<span class=\"setvalue\">");
                switch (ft)
                {
                    case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                        htmlWriter.Write(" of the available food supply");
                        overfeed = true;
                        break;
                    case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                        htmlWriter.Write(" per individual per day");
                        overfeed = true;
                        break;
                    case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                        overfeed = true;
                        htmlWriter.Write(" per day");
                        break;
                    case RuminantFeedActivityTypes.ProportionOfWeight:
                        overfeed = true;
                        htmlWriter.Write(starter + "live weight");
                        break;
                    case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                        htmlWriter.Write(starter + "potential intake");
                        break;
                    case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                        htmlWriter.Write(starter + "remaining intake");
                        break;
                    default:
                        break;
                }
                htmlWriter.Write("</span> ");

                switch (ft)
                {
                    case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                        htmlWriter.Write("will be fed to all individuals that match the following conditions:");
                        break;
                    case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                        htmlWriter.Write("combined is fed to all individuals that match the following conditions:");
                        break;
                    case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                        htmlWriter.Write("is fed to each individual that matches the following conditions:");
                        break;
                    default:
                        htmlWriter.Write("is fed to the individuals that match the following conditions:");
                        break;
                }
                htmlWriter.Write("</div>");

                if (overfeed)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("Individual's intake will be limited to Potential intake x the modifer for max overfeeding");
                    if (!(this.Parent as RuminantActivityFeed).StopFeedingWhenSatisfied)
                    {
                        htmlWriter.Write(", with excess food still utilised but wasted");
                    }
                    htmlWriter.Write("</div>");

                }
                if (ft == RuminantFeedActivityTypes.SpecifiedDailyAmount)
                {
                    htmlWriter.Write("<div class=\"warningbanner\">Note: This is a specified daily amount fed to the entire herd. If insufficient provided, each individual's potential intake will not be met</div>");
                }

                return htmlWriter.ToString();
            }
        }

        #endregion

    }
}
