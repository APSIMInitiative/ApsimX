using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

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
    public class OtherAnimalsActivityFeed : CLEMActivityBase
    {
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Name of Feed to use
        /// </summary>
        [Description("Feed type name in Animal Food Store")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Feed type name to use required")]
        public string FeedTypeName { get; set; }

        /// <summary>
        /// Feeding style to use
        /// </summary>
        [Description("Feeding style to use")]
        [System.ComponentModel.DefaultValueAttribute(OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount)]
        [Required]
        public OtherAnimalsFeedActivityTypes FeedStyle { get; set; }

        /// <summary>
        /// Labour settings
        /// </summary>
        private List<LabourFilterGroupSpecified> labour { get; set; }

        private IResourceType FoodSource { get; set; }
        /// <summary>
        /// Feed type
        /// </summary>
        [XmlIgnore]
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
            FeedType = Resources.GetResourceItem(this, typeof(AnimalFoodStore), FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IFeedType;
            FoodSource = FeedType;

            // get labour specifications
            labour = Apsim.Children(this, typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
            if (labour == null) labour = new List<LabourFilterGroupSpecified>();
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            ResourceRequestList = null;

            // get feed required
            // zero based month index for array
            int month = Clock.Today.Month - 1;
            double allIndividuals = 0;
            double amount = 0;
            foreach (OtherAnimalsFilterGroup filtergroup in Apsim.Children(this, typeof(OtherAnimalsFilterGroup)))
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
                    if(ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
                    ResourceRequestList.Add(new ResourceRequest()
                    {
                        AllowTransmutation = true,
                        Required = amount,
                        ResourceType = typeof(AnimalFoodStore),
                        ResourceTypeName = FeedTypeName,
                        ActivityModel = this,
//                        ActivityName = "Feed " + (child as OtherAnimalsFilterGroup).AnimalType,
                        Reason = "oops",
                        FilterDetails = null
                    }
                    );
                }
            }

            if (amount == 0) return ResourceRequestList;

            // for each labour item specified
            foreach (var item in labour)
            {
                double daysNeeded = 0;
                switch (item.UnitType)
                {
                    case LabourUnitType.Fixed:
                        daysNeeded = item.LabourPerUnit;
                        break;
                    case LabourUnitType.perHead:
                        daysNeeded = Math.Ceiling(allIndividuals / item.UnitSize) * item.LabourPerUnit;
                        break;
                    default:
                        throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", item.UnitType, item.Name, this.Name));
                }
                if (daysNeeded > 0)
                {
                    if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
                    ResourceRequestList.Add(new ResourceRequest()
                    {
                        AllowTransmutation = false,
                        Required = daysNeeded,
                        ResourceType = typeof(Labour),
                        ResourceTypeName = "",
                        ActivityModel = this,
                        FilterDetails = new List<object>() { item }
                    }
                    );
                }
            }
            return ResourceRequestList;
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
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
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
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
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
