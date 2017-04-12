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

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			// locate FeedType resource
			bool resourceAvailable = false;
			FeedType = Resources.GetResourceItem("AnimalFoodStore", FeedTypeName, out resourceAvailable) as IFeedType;
			FoodSource = FeedType;
		}

		///// <summary>An event handler to call for all feed requests prior to arbitration and growth</summary>
		///// <param name="sender">The sender.</param>
		///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		//[EventSubscribe("WFRequestFeed")]
		//private void OnWFRequestFeed(object sender, EventArgs e)
		//{
		//	if (labourLimiter > 0)
		//	{
		//		// get month from clock
		//		int month = Clock.Today.Month;

		//		// get list from filters
		//		foreach (var child in this.Children)
		//		{
		//			if (child.GetType() == typeof(OtherAnimalsFilterGroup))
		//			{
		//				double total = 0;
		//				foreach (OtherAnimalsTypeCohort item in (child as OtherAnimalsFilterGroup).SelectedOtherAnimalsType.Cohorts.Filter(child as OtherAnimalsFilterGroup))
		//				{
		//					total += item.Number;
		//				}
		//				double amount = 0;
		//				switch (FeedStyle)
		//				{
		//					case OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount:
		//						amount = (child as OtherAnimalsFilterGroup).MonthlyValues[month - 1] * 30.4 * total;
		//						break;
		//					case OtherAnimalsFeedActivityTypes.ProportionOfWeight:
		//						throw new NotImplementedException("Proportion of weight is not implemented as a feed style for other animals");
		//					default:
		//						amount = 0;
		//						break;
		//				}
		//				amount *= labourLimiter;
		//				if (amount > 0)
		//				{
		//					FoodSource.Remove(amount, this.Name, (child as OtherAnimalsFilterGroup).AnimalType);
		//				}
		//			}
		//		}
		//	}

		//}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns></returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			ResourceRequestList = new List<ResourceRequest>();

			// get feed required
			// zero based month index for array
			int month = Clock.Today.Month - 1;
			double amount = 0;
			foreach (OtherAnimalsFilterGroup child in this.Children.Where(a => a.GetType() == typeof(OtherAnimalsFilterGroup)))
			{
				double total = 0;
				foreach (OtherAnimalsTypeCohort item in (child as OtherAnimalsFilterGroup).SelectedOtherAnimalsType.Cohorts.Filter(child as OtherAnimalsFilterGroup))
				{
					total += item.Number * ((item.Age < (child as OtherAnimalsFilterGroup).SelectedOtherAnimalsType.AgeWhenAdult)?0.1:1);
				}
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
					ResourceRequestList.Add(new ResourceRequest()
					{
						AllowTransmutation = true,
						Required = amount,
						ResourceName = "AnimalFoodStore",
						ResourceTypeName = FeedTypeName,
						Requestor = (child as OtherAnimalsFilterGroup).AnimalType,
						FilterSortDetails = null
					}
					);
//					FoodSource.Remove(amount, this.Name, (child as OtherAnimalsFilterGroup).AnimalType);
				}
			}

			if (amount > 0)
			{
				// get labour required
				double labourneeded = this.Children.Where(a => a.GetType() == typeof(ActivityLabourRequirementGroup)).Cast<ActivityLabourRequirementGroup>().FirstOrDefault().DaysRequired;
				if (labourneeded > 0)
				{
					ResourceRequestList.Add(new ResourceRequest()
					{
						AllowTransmutation = false,
						Required = labourneeded,
						ResourceName = "Labour",
						ResourceTypeName = "",
						Requestor = this.Name,
						FilterSortDetails = this.Children.Where(a => a.GetType() == typeof(ActivityLabourRequirementGroup)).ToList<object>()
					}
					);
				}
			}

			if (ResourceRequestList.Count()==0)
			{
				return null;
			}
			return ResourceRequestList;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void PerformActivity()
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
