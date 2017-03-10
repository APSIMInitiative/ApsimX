using Models.Core;
using Models.WholeFarm.Groupings;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>Ruminant feed activity</summary>
	/// <summary>This activity provides food to specified ruminants based on a feeding style</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public class RuminantActivityFeed : WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;
		[Link]
		ISummary Summary = null;

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

		/// <summary>
		/// Labour required per x head
		/// </summary>
		[Description("Labour required per x head")]
		public double LabourRequired { get; set; }

		/// <summary>
		/// Number of head per labour unit required
		/// </summary>
		[Description("Number of head per labour unit required")]
		public double LabourHeadUnit { get; set; }

		/// <summary>
		/// Does labour shortfall limit feeding
		/// </summary>
		[Description("Does labour shortfall limit feeding")]
		public bool LabourShortfallLimitsFeeding { get; set; }

		private IResourceType FoodSource { get; set; }

		/// <summary>
		/// Labour grouping for breeding
		/// </summary>
		public List<object> LabourFilterList { get; set; }

		/// <summary>
		/// Feed type
		/// </summary>
		[XmlIgnore]
		public IFeedType FeedType { get; set; }

		/// <summary>
		/// Feeding style to use
		/// </summary>
		[Description("Feeding style to use")]
		public RuminantFeedActivityTypes FeedStyle { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			// locate FeedType resource
			bool resourceAvailable = false;
			FeedType = Resources.GetResourceItem("AnimalFoodStore",FeedTypeName, out resourceAvailable) as IFeedType;
			FoodSource = FeedType;

			if (LabourRequired > 0)
			{
				// check for and assign labour filter group
				LabourFilterList = this.Children.Where(a => a.GetType() == typeof(LabourFilterGroup)).Cast<object>().ToList();
				// if not present assume can use any labour and report
				if (LabourFilterList == null)
				{
					Summary.WriteWarning(this, String.Format("No labour filter details provided for feeding activity ({0}). Assuming any labour type can be used", this.Name));
					LabourFilterGroup lfg = new LabourFilterGroup();
					LabourFilter lf = new LabourFilter()
					{
						Operator = FilterOperators.GreaterThanOrEqual,
						Value = "0",
						Parameter = LabourFilterParameters.Age
					};
					lfg.Children.Add(lf);
					LabourFilterList = new List<object>();
					LabourFilterList.Add(lfg);
				}
			}
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			ResourceRequestList = null;
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd;
			if (herd != null && herd.Count > 0)
			{
				// labour
				if (LabourRequired > 0)
				{
					if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
					// determine head to be fed
					int head = 0;
					foreach (RuminantFilterGroup child in this.Children.Where(a => a.GetType() == typeof(RuminantFilterGroup)))
					{
						head += herd.Filter(child as RuminantFilterGroup).Count();
					}
					ResourceRequestList.Add(new ResourceRequest()
					{
						AllowTransmutation = true,
						Required = Math.Ceiling(head/this.LabourHeadUnit)*this.LabourRequired,
						ResourceName = "Labour",
						ResourceTypeName = "",
						ActivityName = this.Name,
						FilterDetails = LabourFilterList
					}
					);
				}

				// feed
				double feedRequired = 0;
				// get zero limited month from clock
				int month = Clock.Today.Month - 1;

				// get list from filters
				foreach (RuminantFilterGroup child in this.Children.Where(a => a.GetType() == typeof(RuminantFilterGroup)))
				{
					foreach (Ruminant ind in herd.Filter(child as RuminantFilterGroup))
					{
						switch (FeedStyle)
						{
							case RuminantFeedActivityTypes.SpecifiedDailyAmount:
								feedRequired += (child as RuminantFilterGroup).MonthlyValues[month] * 30.4;
								break;
							case RuminantFeedActivityTypes.ProportionOfWeight:
								feedRequired += (child as RuminantFilterGroup).MonthlyValues[month] * ind.Weight * 30.4;
								break;
							case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
								feedRequired += (child as RuminantFilterGroup).MonthlyValues[month] * ind.PotentialIntake;
								break;
							case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
								feedRequired += (child as RuminantFilterGroup).MonthlyValues[month] * (ind.PotentialIntake - ind.Intake);
								break;
							default:
								break;
						}
					}
				}
				if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
				ResourceRequestList.Add(new ResourceRequest()
				{
					AllowTransmutation = true,
					Required = feedRequired,
					ResourceName = "AnimalFoodStore",
					ResourceTypeName = this.FeedTypeName,
					ActivityName = this.Name
				}
				);
			}
			return ResourceRequestList;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void PerformActivity()
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd;
			if (herd != null && herd.Count > 0)
			{
				// calculate labour limit
				double labourLimit = 1.0;

				// can't limit food here by labour as it has already been taken from stores.

				//if (LabourShortfallLimitsFeeding)
				//{
				//	ResourceRequest labourRequest = ResourceRequestList.Where(a => a.ResourceName == "Labour").FirstOrDefault();
				//	if (labourRequest != null)
				//	{
				//		labourLimit = Math.Min(1.0, labourRequest.Provided / labourRequest.Required);
				//	}
				//}

				// calculate feed limit
				double feedLimit = 0.0;
				ResourceRequest feedRequest = ResourceRequestList.Where(a => a.ResourceName == "AnimalFoodStore").FirstOrDefault();
				AnimalFoodResourceRequestDetails details = new AnimalFoodResourceRequestDetails();
				if (feedRequest != null)
				{
					details = feedRequest.AdditionalDetails as AnimalFoodResourceRequestDetails;
					feedLimit = Math.Min(1.0, feedRequest.Provided / feedRequest.Required);
				}

				// feed animals
				int month = Clock.Today.Month - 1;

				// get list from filters
				foreach (RuminantFilterGroup child in this.Children.Where(a => a.GetType() == typeof(RuminantFilterGroup)))
				{
					foreach (Ruminant ind in herd.Filter(child as RuminantFilterGroup))
					{
//						double feedRequired = 0;
						switch (FeedStyle)
						{
							case RuminantFeedActivityTypes.SpecifiedDailyAmount:
								details.Supplied = (child as RuminantFilterGroup).MonthlyValues[month] * 30.4;
								details.Supplied *= feedLimit * labourLimit;
								ind.AddIntake(details);
//								feedRequired += (child as RuminantFilterGroup).MonthlyValues[month] * 30.4; // * ind.Number;
//								ind.Intake += feedRequired * feedLimit * labourLimit;
								break;
							case RuminantFeedActivityTypes.ProportionOfWeight:
								details.Supplied = (child as RuminantFilterGroup).MonthlyValues[month] * ind.Weight * 30.4; // * ind.Number;
								details.Supplied *= feedLimit * labourLimit;
								ind.AddIntake(details);
//								feedRequired += (child as RuminantFilterGroup).MonthlyValues[month] * ind.Weight * 30.4; // * ind.Number;
//								ind.Intake += feedRequired * feedLimit * labourLimit;
								break;
							case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
								details.Supplied = (child as RuminantFilterGroup).MonthlyValues[month] * ind.PotentialIntake; // * ind.Number;
								details.Supplied *= feedLimit * labourLimit;
								ind.AddIntake(details);
//								feedRequired += (child as RuminantFilterGroup).MonthlyValues[month] * ind.PotentialIntake; // * ind.Number;
//								ind.Intake += feedRequired * feedLimit * labourLimit;
								break;
							case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
								details.Supplied = (child as RuminantFilterGroup).MonthlyValues[month] * (ind.PotentialIntake - ind.Intake); // * ind.Number;
								details.Supplied *= feedLimit * labourLimit;
								ind.AddIntake(details);
//								feedRequired += (child as RuminantFilterGroup).MonthlyValues[month] * (ind.PotentialIntake - ind.Intake) ; // * ind.Number;
//								ind.Intake += feedRequired * feedLimit * labourLimit;
								break;
							default:
								break;
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Ruminant feeding styles
	/// </summary>
	public enum RuminantFeedActivityTypes
	{
		/// <summary>
		/// Feed specified amount daily in selected months
		/// </summary>
		SpecifiedDailyAmount,
		/// <summary>
		/// Feed proportion of animal weight in selected months
		/// </summary>
		ProportionOfWeight,
		/// <summary>
		/// Feed proportion of potential intake
		/// </summary>
		ProportionOfPotentialIntake,
		/// <summary>
		/// Feed proportion of remaining amount required
		/// </summary>
		ProportionOfRemainingIntakeRequired
	}

}
