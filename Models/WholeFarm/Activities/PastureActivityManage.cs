using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>Pasture management activity</summary>
	/// <summary>This activity provides a pasture based on land unit, area and pasture type</summary>
	/// <summary>Ruminant mustering activities place individuals in the paddack after which they will graze pasture for the paddock stored in the PastureP Pools</summary>
	/// <version>1.0</version>
	/// <updates>First implementation of this activity using NABSA grazing processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public class PastureActivityManage: WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;
		[Link]
		Clock Clock = null;

		/// <summary>
		/// Name of land type where pasture is located
		/// </summary>
		[Description("Land type where pasture is located")]
		public string LandTypeNameToUse { get; set; }

		/// <summary>
		/// Name of the pasture type to use
		/// </summary>
		[Description("Name of pasture")]
		public string FeedTypeName { get; set; }

		/// <summary>
		/// Area of pasture
		/// </summary>
		[XmlIgnore]
		public double Area { get; set; }

		/// <summary>
		/// Current land condition index
		/// </summary>
		[XmlIgnore]
		public double LandConditionIndex { get; set; }

		/// <summary>
		/// Land condition index at start
		/// </summary>
		[Description("Land condition index at start")]
		public double LandConditionIndexAtStart { get; set; }

		/// <summary>
		/// Grass basal area
		/// </summary>
		[XmlIgnore]
		public double GrassBasalArea { get; set; }

		/// <summary>
		/// Stocking Rate (#/ha)
		/// </summary>
		[XmlIgnore]
		public double StockingRate { get; set; }

		/// <summary>
		/// Units of erea to use
		/// </summary>
		[Description("units of area")]
		public UnitsOfAreaTypes UnitsOfArea { get; set; }

		/// <summary>
		/// Area requested
		/// </summary>
		[Description("Area requested")]
		public double AreaRequested { get; set; }

		/// <summary>
		/// Feed type
		/// </summary>
		[XmlIgnore]
		public GrazeFoodStoreType LinkedNativeFoodType { get; set; }

		/// <summary>
		/// Feed at start of month before grazing after updating
		/// </summary>
		[XmlIgnore]
		private double PastureAtStartOfMonth { get; set; }

		/// <summary>
		/// Convert area type specified to hectares
		/// </summary>
		[XmlIgnore]
		public double ConvertToHectares { get
			{
				switch (UnitsOfArea)
				{
					case UnitsOfAreaTypes.Squarekm:
						return 100;
					case UnitsOfAreaTypes.Hectares:
						return 1;
					default:
						return 0;
				}
			}
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			LandConditionIndex = LandConditionIndexAtStart;

			// locate Pasture Type resource
			bool resourceAvailable = false;
			LinkedNativeFoodType = Resources.GetResourceItem("GrazeFoodStore", FeedTypeName, out resourceAvailable) as GrazeFoodStoreType;

			// TODO: Set up pasture pools to start run

		}

		/// <summary>An event handler to allow us to get next supply of pasture</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFUpdatePasture")]
		private void OnWFUpdatePasture(object sender, EventArgs e)
		{
			//TODO: Get pasture growth from pasture model or GRASP output

			//
			// temporary pasture input for testing
			//
			GrazeFoodStorePool newPasture = new GrazeFoodStorePool();
			newPasture.Age = 0;
			if (Clock.Today.Month >= 11 ^ Clock.Today.Month <= 3)
			{
				newPasture.Set(500 * Area);
				newPasture.DMD = this.LinkedNativeFoodType.DMD;
				newPasture.DryMatter = this.LinkedNativeFoodType.DryMatter;
				newPasture.Nitrogen = this.LinkedNativeFoodType.Nitrogen;
				this.LinkedNativeFoodType.Add(newPasture);
			}

			// store total pasture at start of month
			PastureAtStartOfMonth = this.LinkedNativeFoodType.Amount;
		}

		/// <summary>
		/// Function to age resource pools
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAgeResources")]
		private void OnWFAgeResources(object sender, EventArgs e)
		{
			//TODO: calculate stocking rate and Land Condition from numbers and amount consumed.



			// consumption needs to be calculated before decay and againg.

			// decay N and DMD of pools and age by 1 month
			foreach (var pool in LinkedNativeFoodType.Pools)
			{
				pool.Nitrogen = Math.Min(pool.Nitrogen * (1 - LinkedNativeFoodType.DecayNitrogen), LinkedNativeFoodType.MinimumNitrogen);
				pool.DMD = Math.Min(pool.DMD * (1 - LinkedNativeFoodType.DecayDMD), LinkedNativeFoodType.MinimumDMD);

				double detach = LinkedNativeFoodType.CarryoverDetachRate;
				if (pool.Age<12)
				{
					detach = LinkedNativeFoodType.DetachRate;
					pool.Age++;
				}
				pool.Set(pool.Amount * detach);
			}
			// remove all pools with less than 100g of food
			LinkedNativeFoodType.Pools.RemoveAll(a => a.Amount < 0.1);
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>A list of resource requests</returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			if(Area==0 & AreaRequested>0)
			{
				ResourceRequestList = new List<ResourceRequest>();
				ResourceRequestList.Add(new ResourceRequest()
				{
					AllowTransmutation = false,
					Required = AreaRequested*((UnitsOfArea == UnitsOfAreaTypes.Hectares)?1:100),
					ResourceName = "Land",
					ResourceTypeName = LandTypeNameToUse,
					ActivityName = this.Name,
					FilterDetails = null
				}
				);
				return ResourceRequestList;
			}
			return null;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void PerformActivity()
		{
			if(ResourceRequestList.Count() > 0)
			{
				Area = ResourceRequestList.FirstOrDefault().Available;
			}
			return;
		}
	}

	/// <summary>
	/// Types of units of erea to use.
	/// </summary>
	public enum UnitsOfAreaTypes
	{
		/// <summary>
		/// Square km
		/// </summary>
		Squarekm,
		/// <summary>
		/// Hectares
		/// </summary>
		Hectares
	}
}
