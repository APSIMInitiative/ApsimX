using Models.Core;
using Models.WholeFarm.Groupings;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>Other animals feed activity</summary>
	/// <summary>This activity provides food to specified other animals based on a feeding style</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class OtherAnimalsActivityFeed : WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;

		/// <summary>
		/// Get the Clock.
		/// </summary>
		[Link]
		Clock Clock = null;

		/// <summary>
		/// Name of Feed to use
		/// </summary>
		[Description("Feed type name in Animal Food Store")]
		public string FeedTypeName { get; set; }

		private IResourceType FoodSource { get; set; }

		/// <summary>
		/// Feed type
		/// </summary>
		[XmlIgnore]
		public IFeedType FeedType { get; set; }

		/// <summary>
		/// Feeding style to use
		/// </summary>
		[Description("Feeding style to use")]
		public OtherAnimalsFeedActivityTypes FeedStyle { get; set; }

		/// <summary>
		/// Labour settings
		/// </summary>
		private List<LabourFilterGroupSpecified> labour { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			// locate FeedType resource
//			bool resourceAvailable = false;
			FeedType = Resources.GetResourceItem(this, typeof(AnimalFoodStore), FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IFeedType;
			FoodSource = FeedType;

			// get labour specifications
			labour = this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
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
			foreach (OtherAnimalsFilterGroup child in this.Children.Where(a => a.GetType() == typeof(OtherAnimalsFilterGroup)))
			{
				double total = 0;
				foreach (OtherAnimalsTypeCohort item in (child as OtherAnimalsFilterGroup).SelectedOtherAnimalsType.Cohorts.Filter(child as OtherAnimalsFilterGroup))
				{
					total += item.Number * ((item.Age < (child as OtherAnimalsFilterGroup).SelectedOtherAnimalsType.AgeWhenAdult)?0.1:1);
				}
				allIndividuals += total;
				switch (FeedStyle)
				{
					case OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount:
						amount += (child as OtherAnimalsFilterGroup).MonthlyValues[month] * 30.4 * total;
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
//						ActivityName = "Feed " + (child as OtherAnimalsFilterGroup).AnimalType,
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
		/// Method used to perform initialisation of this activity.
		/// This will honour ReportErrorAndStop action but will otherwise be preformed regardless of resources available
		/// It is the responsibility of this activity to determine resources provided.
		/// </summary>
		public override void DoInitialisation()
		{
			return;
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
