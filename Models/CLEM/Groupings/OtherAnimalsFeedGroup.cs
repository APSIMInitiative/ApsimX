using Models.CLEM.Activities;
using Models.Core.Attributes;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.CLEM.Resources;
using System.Text.Json.Serialization;
using Models.CLEM.Interfaces;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual other animals to feed
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Selects specific cohorts from the other animals")]
    [ValidParent(ParentType = typeof(OtherAnimalsActivityFeed))]
    [HelpUri(@"Content/Features/Filters/Groups/OtherAnimalsFeedGroup.htm")]
    [Version(1, 0, 1, "")]
    public class OtherAnimalsFeedGroup: OtherAnimalsGroup
    {
        private OtherAnimalsActivityFeed feedActivityParent;
        private ResourceRequest currentFeedRequest;

        /// <summary>
        /// Value to supply for each time-step
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
        /// The current feed resource request calculated for this feed group
        /// </summary>
        [JsonIgnore]
        public ResourceRequest CurrentResourceRequest { get { return currentFeedRequest; } }

        /// <inheritdoc/>
        [Description("Category for transactions")]
        [Models.Core.Display(Order = 500)]
        public string TransactionCategory { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public OtherAnimalsFeedGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            feedActivityParent = FindAncestor<OtherAnimalsActivityFeed>();
            SelectedOtherAnimalsType = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, AnimalTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as OtherAnimalsType;

            currentFeedRequest = new ResourceRequest()
            {
                AllowTransmutation = true,
                Resource = feedActivityParent.FeedType,
                ResourceType = typeof(AnimalFoodStore),
                ResourceTypeName = feedActivityParent.FeedTypeName,
                ActivityModel = Parent as CLEMActivityBase,
                Category = (Parent as CLEMActivityBase).TransactionCategory,
                RelatesToResource = SelectedOtherAnimalsType.Name
            };
        }

        /// <inheritdoc/>
        public override void PrepareForTimestep()
        {
            currentFeedRequest.Required = 0;
            double feedNeeded = 0;
            IEnumerable<OtherAnimalsTypeCohort> selectedCohorts = Filter(feedActivityParent.CohortsToBeFed.Where(a => a.Parent == SelectedOtherAnimalsType));

            if (selectedCohorts != null)
            {
                switch (feedActivityParent.FeedStyle)
                {
                    case OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount:
                        feedNeeded = Value * 30.4;
                        break;
                    case OtherAnimalsFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                        feedNeeded = (Value * 30.4) * selectedCohorts.Sum(a => a.Number);
                        break;
                    case OtherAnimalsFeedActivityTypes.ProportionOfWeight:
                        feedNeeded = Value * selectedCohorts.Sum(a => a.Weight) * selectedCohorts.Sum(a => a.Number) * 30.4;
                        break;
                    default:
                        break;
                }
                if(feedNeeded > 0)
                {
                    currentFeedRequest.Required = feedNeeded;
                    // remove fed cohorts from temp list to avoid double handling of a cohort in the parent activity
                    feedActivityParent.CohortsToBeFed = feedActivityParent.CohortsToBeFed.Where(a => !selectedCohorts.Contains(a));
                }
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double activityMetric)
        {
            return new List<ResourceRequest> { currentFeedRequest };
        }

    }
}
