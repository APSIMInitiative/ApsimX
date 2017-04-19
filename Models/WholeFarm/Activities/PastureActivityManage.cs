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
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class PastureActivityManage: WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;
		[Link]
		Clock Clock = null;
		[Link]
		ISummary Summary = null;
		[Link]
		WholeFarm WholeFarm = null;

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
		/// Starting Amount (kg)
		/// </summary>
		[Description("Starting Amount (kg/ha)")]
		public double StartingAmount { get; set; }

		private double startingAmount = 0;

		/// <summary>
		/// Area of pasture
		/// </summary>
		[XmlIgnore]
		public double Area { get; set; }

		/// <summary>
		/// Current land condition index
		/// </summary>
		[XmlIgnore]
		public Relationship LandConditionIndex { get; set; }

		/// <summary>
		/// Grass basal area
		/// </summary>
		[XmlIgnore]
		public Relationship GrassBasalArea { get; set; }

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
		/// Month for ecological indicators calulation (end of month)
		/// </summary>
		[Description("Month for ecological indicators calulation (end of month)")]
		public int EcolCalculationMonth { get; set; }

		/// <summary>
		/// Ecological indicators calulation interval (months)
		/// </summary>
		[Description("Ecological indicators calulation interval (months)")]
		public int EcolCalculationInterval { get; set; }

		/// <summary>
		/// Feed type
		/// </summary>
		[XmlIgnore]
		public GrazeFoodStoreType LinkedNativeFoodType { get; set; }

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
			// Get Land condition relationship from children
			LandConditionIndex = this.Children.Where(a => a.GetType() == typeof(Relationship) & a.Name=="LandConditionIndex").FirstOrDefault() as Relationship; ;
			if (LandConditionIndex == null)
			{
				Summary.WriteWarning(this, String.Format("Unable to locate Land Condition Index relationship for {0}", this.Name));
			}
			// Get Grass basal area relationship fron children
			GrassBasalArea = this.Children.Where(a => a.GetType() == typeof(Relationship) & a.Name == "GrassBasalArea").FirstOrDefault() as Relationship; ;
			if (GrassBasalArea == null)
			{
				Summary.WriteWarning(this, String.Format("Unable to locate Grass Basal Area relationship for {0}", this.Name));
			}
			// locate Pasture Type resource
			bool resourceAvailable = false;
			LinkedNativeFoodType = Resources.GetResourceItem("GrazeFoodStore", FeedTypeName, out resourceAvailable) as GrazeFoodStoreType;
			if (LinkedNativeFoodType == null)
			{
				Summary.WriteWarning(this, String.Format("Unable to locate graze feed type {0} in GrazeFoodStore for {1}", this.FeedTypeName, this.Name));
			}
			startingAmount = StartingAmount;
		}

		/// <summary>An event handler to allow us to get next supply of pasture</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFUpdatePasture")]
		private void OnWFUpdatePasture(object sender, EventArgs e)
		{
			//TODO: Get pasture growth from pasture model or GRASP output

			double AmountToAdd = 0;
			//double AmountToAdd = FileGRASP.GetPasture(Clock.Today, LandConditionIndex, GrassBasalArea, Utilisation);

			if (AmountToAdd> 0)
			{
				GrazeFoodStorePool newPasture = new GrazeFoodStorePool();
				newPasture.Age = 0;
				newPasture.Set(AmountToAdd * Area);
				newPasture.DMD = this.LinkedNativeFoodType.DMD;
				newPasture.DryMatter = this.LinkedNativeFoodType.DryMatter;
				newPasture.Nitrogen = this.LinkedNativeFoodType.Nitrogen;
				this.LinkedNativeFoodType.Add(newPasture);
			}
		}

		/// <summary>
		/// Function to calculate ecological indicators
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalMilking")]
		private void OnWFAnimalMilking(object sender, EventArgs e)
		{
			// Using the milking event as it happens after growth and pasture consumption and animal death
			// But before any management, buying and selling of animals.
			
			// update monthly stocking rate total
			StockingRate += Resources.RuminantHerd().Herd.Where(a => a.Location == FeedTypeName).Sum(a => a.AdultEquivalent);

			if (WholeFarm.IsEcologicalIndicatorsCalculationMonth())
			{
				CalculateEcologicalIndicators();
			}
		}

		/// <summary>
		/// Function to age resource pools
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAgeResources")]
		private void OnWFAgeResources(object sender, EventArgs e)
		{
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
			// remove all pools with less than 10g of food
			LinkedNativeFoodType.Pools.RemoveAll(a => a.Amount < 0.01);
		}

		/// <summary>
		/// Method to perform calculation of all ecological indicators.
		/// </summary>
		private void CalculateEcologicalIndicators()
		{
			// Calculate change in Land Condition index and Grass basal area
			double utilisation = LinkedNativeFoodType.PercentUtilisation;

			LandConditionIndex.Modify(utilisation);
			GrassBasalArea.Modify(utilisation);

			// Calculate average monthly stocking rate
			StockingRate /= WholeFarm.EcologicalIndicatorsCalculationInterval;

			//erosion
			//tree basal area



			// Reset all stores
			// reset utilisation rate for native food store
//			LinkedNativeFoodType.ResetUtilisation;
			StockingRate = 0;
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
					Reason = "Assign",
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
				LinkedNativeFoodType.Area = Area;

				// Set up pasture pools to start run based on month and user defined pasture
				// the previous five months where growth occurred will be initialised.
				// This months growth will not be included.

				int month = Clock.Today.Month;
				int monthCount = 0;
				int includedMonthCount = 0;
				double propBiomass = 1.0;
				double currentN = LinkedNativeFoodType.Nitrogen;
				double currentDMD = LinkedNativeFoodType.DMD;

				LinkedNativeFoodType.Pools.Clear();

				while (includedMonthCount < 5)
				{
					if (month == 0) month = 12;
					if(month<=3 | month>=11)
					{
						// add new pool
						LinkedNativeFoodType.Pools.Add(new GrazeFoodStorePool()
						{
							Age = monthCount,
							Nitrogen = currentN,
							DMD = currentDMD,
							StartingAmount = propBiomass
						});
						includedMonthCount++;
					}
					propBiomass *= 1 - LinkedNativeFoodType.DetachRate;
					currentN *= 1 - LinkedNativeFoodType.DecayNitrogen;
					currentN = Math.Max(currentN, LinkedNativeFoodType.MinimumNitrogen);
					currentDMD *= 1 - LinkedNativeFoodType.DecayDMD;
					currentDMD = Math.Max(currentDMD, LinkedNativeFoodType.MinimumDMD);
					monthCount++;
					month--;
				}

				// assign pasture biomass to pools based on proportion of total
				double amountToAdd = Area * startingAmount;

				double total = LinkedNativeFoodType.Pools.Sum(a => a.Amount);
				foreach (var pool in LinkedNativeFoodType.Pools)
				{
					pool.Set(amountToAdd * (pool.Amount / total));
				}


				// remove this months growth from pool age 0 to keep biomass at approximately setup.
				// Get this months growth
				double thisMonthsGrowth = 0;
				if (thisMonthsGrowth> 0)
				{
					//TODO: remove from pool age 0
				}

			}
			return;
		}

		/// <summary>
		/// res sh
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
