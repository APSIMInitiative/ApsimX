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
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantFeedGroup : RuminantGroup
    {
        [Link]
        private Summary summary = null;
        [Link(IsOptional = true)]
        private readonly CLEMEvents events = null;

        private RuminantActivityFeed feedActivityParent;
        private ResourceRequest currentFeedRequest;
        private List<Ruminant> individualsToBeFed;
        private bool countNeeded = false;
        private bool weightNeeded = false;

        /// <summary>
        /// Value to supply for each month
        /// </summary>
        [Description("Value to supply")]
        [GreaterThanEqualValue(0)]
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
        /// Amount of feed required to satisfy the animals
        /// </summary>
        public double FeedToSatisfy { get; set; }

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
        [Category("Simulation", "Reporting")]
        [Models.Core.Display(Order = 500)]
        public string TransactionCategory { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            feedActivityParent = Structure.FindParent<RuminantActivityFeed>(recurse: true);
            currentFeedRequest = new ResourceRequest()
            {
                AllowTransmutation = true,
                Resource = feedActivityParent.FeedResource,
                ResourceType = typeof(AnimalFoodStore),
                ResourceTypeName = feedActivityParent.FeedTypeName,
                ActivityModel = Parent as CLEMActivityBase,
                Category = TransactionCategory,
                RelatesToResource = feedActivityParent.PredictedHerdNameToDisplay,
                TransactionPending = true,
            };

            switch (feedActivityParent.FeedStyle)
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
                    string error = $"FeedStyle [{feedActivityParent.FeedStyle}] is not supported by [f=RuminantFeedGroup] in [a={NameWithParent}]";
                    summary.WriteMessage(this, error, MessageType.Error);
                    break;
            }

            if(Value == 0)
            {
                string error = $"Amount to feed set to [0] so no feeding will occur for [f={NameWithParent}]";
                summary.WriteMessage(this, error, MessageType.Warning);
            }

            // warning that any take filters will be ignored.
            if (Structure.FindChildren<TakeFromFiltered>(recurse: true).Any())
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

            // get copy of food store details in case they are dynamically changing.
            currentFeedRequest.AdditionalDetails = new FoodResourcePacket(feedActivityParent.FeedDetails);

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
            currentFeedRequest.Required = 0;
            FeedToSatisfy = 0;
            //FeedToOverSatisfy = 0;
            double feedNeeded = 0;

            var selectedIndividuals = Filter(individualsToBeFed).GroupBy(i => 1).Select(a => new
            {
                Count = countNeeded ? a.Count() : 0,
                Weight = weightNeeded ? a.Sum(b => b.Weight.Live) : 0,
                Intake = a.Sum(b => b.Intake.SolidsDaily.ActualForTimeStep(events.Interval)),
                PotentialIntake = a.Sum(b => b.Intake.SolidsDaily.ExpectedForTimeStep(events.Interval))
            }).FirstOrDefault();

            if (selectedIndividuals != null)
            {
                switch (feedActivityParent.FeedStyle)
                {
                    case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                        feedNeeded = value * events.Interval;
                        break;
                    case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                        feedNeeded = (value * events.Interval) * selectedIndividuals.Count;
                        break;
                    case RuminantFeedActivityTypes.ProportionOfWeight:
                        feedNeeded = value * selectedIndividuals.Weight * events.Interval;
                        break;
                    case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                        feedNeeded = value * selectedIndividuals.PotentialIntake;
                        break;
                    case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                        feedNeeded = value * (selectedIndividuals.PotentialIntake - selectedIndividuals.Intake);
                        break;
                    case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                        feedNeeded = value * feedActivityParent.FeedResource.AmountAvailable;
                        break;
                    default:
                        break;
                }

                // get the amount that can be eaten by individuals available meeting this group filter
                // individuals in multiple filters will be considered once
                // accounts for some feeding style allowing overeating to the user declared value in ruminant

                FeedToSatisfy = selectedIndividuals.PotentialIntake - selectedIndividuals.Intake;
                if (feedActivityParent.StopFeedingWhenSatisfied & feedActivityParent.ForceFeed == false)
                {
                    // restrict to max intake permitted by individuals and avoid overfeed wastage
                    feedNeeded = Math.Min(feedNeeded, FeedToSatisfy);
                }
                (currentFeedRequest.AdditionalDetails as FoodResourcePacket).SetAmount(feedNeeded);
                currentFeedRequest.Required = feedNeeded;
            }
        }
    }
}
