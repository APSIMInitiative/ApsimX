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
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
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
		/// Proportion wastage through trampling (feed trough = 0)
		/// </summary>
		[Description("Proportion wastage through trampling (feed trough = 0)")]
		public double ProportionTramplingWastage { get; set; }

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
			bool resourceAvailable = false;
			FeedType = Resources.GetResourceItem(typeof(AnimalFoodStore),FeedTypeName, out resourceAvailable) as IFeedType;
			FoodSource = FeedType;
			if(FeedType==null)
			{
				Summary.WriteWarning(this, String.Format("Unable to locate feed type {0} in AnimalFoodStore for {1}", this.FeedTypeName, this.Name));
			}

			// get labour specifications
			labour = this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
			if (labour == null) labour = new List<LabourFilterGroupSpecified>();
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			ResourceRequestList = null;
			List<Ruminant> herd;
			int head = 0;
			double AE = 0;
			foreach (RuminantFeedGroup child in this.Children.Where(a => a.GetType() == typeof(RuminantFeedGroup)))
			{
				herd = Resources.RuminantHerd().Herd.Filter(child).ToList();
				head += herd.Count();
				AE += herd.Sum(a => a.AdultEquivalent);
			}

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
						daysNeeded = Math.Ceiling(head / item.UnitSize) * item.LabourPerUnit;
						break;
					case LabourUnitType.perAE:
						daysNeeded = Math.Ceiling(AE / item.UnitSize) * item.LabourPerUnit;
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
						ActivityName = this.Name,
						FilterDetails = new List<object>() { item }
					}
					);
				}
			}

			herd = Resources.RuminantHerd().Herd.ToList();

			// feed
			double feedRequired = 0;
			// get zero limited month from clock
			int month = Clock.Today.Month - 1;

			// get list from filters
			foreach (RuminantFeedGroup child in this.Children.Where(a => a.GetType() == typeof(RuminantFeedGroup)))
			{
				foreach (Ruminant ind in herd.Filter(child as RuminantFeedGroup))
				{
					switch (FeedStyle)
					{
						case RuminantFeedActivityTypes.SpecifiedDailyAmount:
							feedRequired += (child as RuminantFeedGroup).MonthlyValues[month] * 30.4;
							break;
						case RuminantFeedActivityTypes.ProportionOfWeight:
							feedRequired += (child as RuminantFeedGroup).MonthlyValues[month] * ind.Weight * 30.4;
							break;
						case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
							feedRequired += (child as RuminantFeedGroup).MonthlyValues[month] * ind.PotentialIntake;
							break;
						case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
							feedRequired += (child as RuminantFeedGroup).MonthlyValues[month] * (ind.PotentialIntake - ind.Intake);
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
				ResourceType = typeof(AnimalFoodStore),
				ResourceTypeName = this.FeedTypeName,
				ActivityName = this.Name
			}
			);

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
				double labourLimit = 1;
				double labourNeeded = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Required);
				double labourProvided = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Provided);
				if(labourNeeded>0)
				{
					labourLimit = labourProvided / labourNeeded;
				}

				// calculate feed limit
				double feedLimit = 0.0;
				double wastage = 1.0 - this.ProportionTramplingWastage;

				ResourceRequest feedRequest = ResourceRequestList.Where(a => a.ResourceType == typeof(AnimalFoodStore)).FirstOrDefault();
				AnimalFoodResourceRequestDetails details = new AnimalFoodResourceRequestDetails();
				if (feedRequest != null)
				{
					details = feedRequest.AdditionalDetails as AnimalFoodResourceRequestDetails;
					feedLimit = Math.Min(1.0, feedRequest.Provided / feedRequest.Required);
				}

				// feed animals
				int month = Clock.Today.Month - 1;

				// get list from filters
				foreach (RuminantFeedGroup child in this.Children.Where(a => a.GetType() == typeof(RuminantFeedGroup)))
				{
					foreach (Ruminant ind in herd.Filter(child as RuminantFeedGroup))
					{
						switch (FeedStyle)
						{
							case RuminantFeedActivityTypes.SpecifiedDailyAmount:
								details.Supplied = (child as RuminantFeedGroup).MonthlyValues[month] * 30.4;
								details.Supplied *= feedLimit * labourLimit * wastage;
								ind.AddIntake(details);
								break;
							case RuminantFeedActivityTypes.ProportionOfWeight:
								details.Supplied = (child as RuminantFeedGroup).MonthlyValues[month] * ind.Weight * 30.4; // * ind.Number;
								details.Supplied *= feedLimit * labourLimit * wastage;
								ind.AddIntake(details);
								break;
							case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
								details.Supplied = (child as RuminantFeedGroup).MonthlyValues[month] * ind.PotentialIntake; // * ind.Number;
								details.Supplied *= feedLimit * labourLimit * wastage;
								ind.AddIntake(details);
								break;
							case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
								details.Supplied = (child as RuminantFeedGroup).MonthlyValues[month] * (ind.PotentialIntake - ind.Intake); // * ind.Number;
								details.Supplied *= feedLimit * labourLimit * wastage;
								ind.AddIntake(details);
								break;
							default:
								break;
						}
					}
				}
			}

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

}
