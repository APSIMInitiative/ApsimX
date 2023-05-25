using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Other animals feed activity</summary>
    /// <summary>This activity provides food to specified other animals based on a feeding style</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manages the feeding of a specified type of other animal based on a feeding style")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/OtherAnimals/OtherAnimalsActivityFeed.htm")]
    public class OtherAnimalsActivityFeed : CLEMActivityBase
    {
        [Link]
        private IClock clock = null;

        /// <summary>
        /// Name of Feed to use
        /// </summary>
        [Description("Feed store to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(AnimalFoodStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Feed type to use required")]
        public string FeedTypeName { get; set; }

        /// <summary>
        /// Feeding style to use
        /// </summary>
        [Description("Feeding style to use")]
        [System.ComponentModel.DefaultValueAttribute(OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount)]
        [Required]
        public OtherAnimalsFeedActivityTypes FeedStyle { get; set; }

        /// <summary>
        /// Feed type
        /// </summary>
        [JsonIgnore]
        public IFeedType FeedType { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public OtherAnimalsActivityFeed()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // locate FeedType resource
            FeedType = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IFeedType;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            List<ResourceRequest> resourcesNeeded = new List<ResourceRequest>();

            // get feed required
            // zero based month index for array
            int month = clock.Today.Month - 1;
            double allIndividuals = 0;
            double amount = 0;
            foreach (var group in FindAllChildren<OtherAnimalsFilterGroup>())
            {
                double total = 0;
                foreach (var item in group.Filter(group.SelectedOtherAnimalsType.Cohorts))
                {
                    total += item.Number * ((item.Age < group.SelectedOtherAnimalsType.AgeWhenAdult) ? 0.1 : 1);
                }
                allIndividuals += total;
                switch (FeedStyle)
                {
                    case OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount:
                        amount += group.MonthlyValues[month] * 30.4 * total;
                        break;
                    case OtherAnimalsFeedActivityTypes.ProportionOfWeight:
                        throw new NotImplementedException("Proportion of weight is not implemented as a feed style for other animals");
                    default:
                        amount += 0;
                        break;
                }

                if (amount > 0)
                {
                    resourcesNeeded.Add(new ResourceRequest()
                    {
                        AllowTransmutation = true,
                        Required = amount,
                        Resource = FeedType,
                        ResourceType = typeof(AnimalFoodStore),
                        ResourceTypeName = FeedTypeName,
                        ActivityModel = this,
                        Category = TransactionCategory,
                        RelatesToResource = "Other animals",
                        FilterDetails = null
                    }
                    );
                }
            }
            return resourcesNeeded;
        }

    }

    /// <summary>
    /// Ruminant feeding styles
    /// </summary>
    public enum OtherAnimalsFeedActivityTypes
    {
        /// <summary>
        /// Feed specified amount daily in selected months
        /// </summary>
        SpecifiedDailyAmount,
        /// <summary>
        /// Feed proportion of animal weight in selected months
        /// </summary>
        ProportionOfWeight,
    }
}
