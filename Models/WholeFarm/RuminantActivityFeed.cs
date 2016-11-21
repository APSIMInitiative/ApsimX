using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	/// <summary>Ruminant feed activity</summary>
	/// <summary>This activity provides food to specified ruminants based on a feeding style</summary>
	/// <summary>It is designed to request food via a food store arbitrator or from a AnimalFoodStoreType directly</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Activities))]
	public class RuminantActivityFeed : Model, IFeedActivity
	{
		[Link]
		private Resources Resources = null;
		[Link]
		private Arbitrators Arbitrators = null;
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// Get the Clock.
		/// </summary>
		[Link]
		Clock Clock = null;

		/// <summary>
		/// Feeding arbitrator to use
		/// </summary>
		[Description("Name of Feeding Arbitrator to use")]
		public string FeedArbitratorName { get; set; }

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
		public RuminantFeedActivityTypes FeedStyle { get; set; }

		/// <summary>
		/// Feeding priority (1 high, 10 low)
		/// </summary>
		[Description("Feeding priority")]
		public int FeedPriority { get; set; }

		/// <summary>
		/// Labour priority (1 high, 10 low)
		/// </summary>
		[Description("Labour priority")]
		public int LabourPriority { get; set; }

		private double labourLimiter = 1.0;
		private bool labourIncluded = false;

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			// set priority to 10 (lowest) if not supplied.
			if (FeedPriority == 0) FeedPriority = 10;
			if (LabourPriority == 0) LabourPriority = 10;

			// locate FeedType resource
			FeedType = Resources.GetResourceItem("AnimalFoodStore",FeedTypeName) as IFeedType;

			// Determine if named Arbitrator exists else work with feed type resource
			FoodSource = null;
			if (FeedArbitratorName!=null)
			{
				FoodSource = Arbitrators.GetByName(FeedArbitratorName) as IResourceType;
				if (FoodSource == null)
				{
					Summary.WriteWarning(this, String.Format("Invalid feed arbitrator ({0}) supplied for {1}.", FeedArbitratorName, this.Name));
					throw new Exception("Invalid feed arbitrator supplied!");
				}
			}
			if (FoodSource == null)
			{
				FoodSource = FeedType;
			}
		}

		/// <summary>An event handler to call for all resources other than food for feeding activity</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFRequestResources")]
		private void OnWFRequestResources(object sender, EventArgs e)
		{
			// if labour item(s) found labour will be requested for this activity.
			labourIncluded = false;
			// check labour required

			// request labour

		}

		/// <summary>An event handler to call for all resources other than food for feeding activity</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFResourcesAllocated")]
		private void OnWFResourcesAllocated(object sender, EventArgs e)
		{
			if (labourIncluded)
			{
				// work out if labour was limited
				//LabourLimiter = 1.0;
			}
		}


		/// <summary>An event handler to call for all feed requests prior to arbitration and growth</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFRequestFeed")]
		private void OnWFRequestFeed(object sender, EventArgs e)
		{
			if (labourLimiter > 0)
			{
				RuminantHerd ruminantHerd = Resources.RuminantHerd();
				List<Ruminant> herd = ruminantHerd.Herd;

				if (herd == null && herd.Count == 0) return;

				// get month from clock
				int month = Clock.Today.Month;

				// get list from filters
				foreach (var child in this.Children)
				{
					if (child.GetType() == typeof(RuminantFilterGroup))
					{
						foreach (Ruminant ind in herd.Filter(child as RuminantFilterGroup))
						{
							RuminantFeedRequest freqest = new RuminantFeedRequest();
							freqest.FeedActivity = this;
							freqest.Requestor = ind;
							freqest.Amount = 0;
							switch (FeedStyle)
							{
								case RuminantFeedActivityTypes.SpecifiedAmount:
									freqest.Amount = (child as RuminantFilterGroup).MonthlyValues[month - 1]; // * ind.Number;
									break;
								case RuminantFeedActivityTypes.ProportionOfWeight:
									freqest.Amount = (child as RuminantFilterGroup).MonthlyValues[month - 1] * ind.Weight; // * ind.Number;
									break;
								case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
									freqest.Amount = (child as RuminantFilterGroup).MonthlyValues[month - 1] * ind.PotentialIntake; // * ind.Number;
									break;
								case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
									freqest.Amount = (child as RuminantFilterGroup).MonthlyValues[month - 1] * (ind.PotentialIntake - ind.Intake); // * ind.Number;
									break;
								default:
									break;
							}
							freqest.Amount *= labourLimiter;
							FoodSource.Remove(freqest);
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
		/// Feed specified amount in selected months
		/// </summary>
		SpecifiedAmount,
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
