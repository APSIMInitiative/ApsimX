using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Other animals feed activity</summary>
    /// <summary>This activity provides food to specified other animals based on a feeding style</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages the feeding of a specified type of other animal based on a feeding style.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/OtherAnimals/OtherAnimalsActivityFeed.htm")]
    public class OtherAnimalsActivityFeed : CLEMActivityBase
    {
        [Link]
        Clock Clock = null;

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
            FeedType = Resources.GetResourceItem(this, FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IFeedType;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<ResourceRequest> resourcesNeeded = new List<ResourceRequest>();

            // get feed required
            // zero based month index for array
            int month = Clock.Today.Month - 1;
            double allIndividuals = 0;
            double amount = 0;
            foreach (OtherAnimalsFilterGroup filtergroup in this.FindAllChildren<OtherAnimalsFilterGroup>())
            {
                double total = 0;
                foreach (OtherAnimalsTypeCohort item in (filtergroup as OtherAnimalsFilterGroup).SelectedOtherAnimalsType.Cohorts.Filter(filtergroup as OtherAnimalsFilterGroup))
                {
                    total += item.Number * ((item.Age < (filtergroup as OtherAnimalsFilterGroup).SelectedOtherAnimalsType.AgeWhenAdult)?0.1:1);
                }
                allIndividuals += total;
                switch (FeedStyle)
                {
                    case OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount:
                        amount += (filtergroup as OtherAnimalsFilterGroup).MonthlyValues[month] * 30.4 * total;
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
                        ResourceType = typeof(AnimalFoodStore),
                        ResourceTypeName = FeedTypeName,
                        ActivityModel = this,
                        Category = "Feed",
                        RelatesToResource = "Other animals",
                        FilterDetails = null
                    }
                    );
                }
            }
            return resourcesNeeded;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return;
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
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            double allIndividuals = 0;
            foreach (OtherAnimalsFilterGroup filtergroup in this.FindAllChildren<OtherAnimalsFilterGroup>())
            {
                double total = 0;
                foreach (OtherAnimalsTypeCohort item in (filtergroup as OtherAnimalsFilterGroup).SelectedOtherAnimalsType.Cohorts.Filter(filtergroup as OtherAnimalsFilterGroup))
                {
                    total += item.Number * ((item.Age < (filtergroup as OtherAnimalsFilterGroup).SelectedOtherAnimalsType.AgeWhenAdult) ? 0.1 : 1);
                }
                allIndividuals += total;
            }

            double daysNeeded;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    daysNeeded = Math.Ceiling(allIndividuals / requirement.UnitSize) * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Feed", "Other animals");
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
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
