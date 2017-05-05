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
		/// Perennials
		/// </summary>
		[XmlIgnore]
		public double Perennials { get; set; }

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
		/// Month for ecological indicators calculation (end of month)
		/// </summary>
		[Description("Month for ecological indicators calculation (end of month)")]
		public int EcolCalculationMonth { get; set; }

		/// <summary>
		/// Ecological indicators calculation interval (months)
		/// </summary>
		[Description("Ecological indicators calculation interval (months)")]
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
			LinkedNativeFoodType = Resources.GetResourceItem(typeof(GrazeFoodStore), FeedTypeName, out resourceAvailable) as GrazeFoodStoreType;
			if (LinkedNativeFoodType == null)
			{
				Summary.WriteWarning(this, String.Format("Unable to locate graze feed type {0} in GrazeFoodStore for {1}", this.FeedTypeName, this.Name));
			}
			startingAmount = StartingAmount;

			LinkedNativeFoodType.CurrentEcologicalIndicators.LandConditionIndex = LandConditionIndex.StartingValue;
			LinkedNativeFoodType.CurrentEcologicalIndicators.GrassBasalArea = GrassBasalArea.StartingValue;
		}

		/// <summary>An event handler to allow us to get next supply of pasture</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFUpdatePasture")]
		private void OnWFUpdatePasture(object sender, EventArgs e)
		{
			//TODO: Get pasture growth from pasture model or GRASP output
			double AmountToAdd = 0; 
			//double AmountToAdd = FileGRASP.GetPasture(LandConditionIndex.Value, GrassBasalArea.Value, Utilisation);

			if (AmountToAdd> 0)
			{
				GrazeFoodStorePool newPasture = new GrazeFoodStorePool();
				newPasture.Age = 0;
				newPasture.Set(AmountToAdd * Area);
				newPasture.DMD = this.LinkedNativeFoodType.DMD;
				newPasture.Nitrogen = this.LinkedNativeFoodType.GreenNitrogen;
				newPasture.DryMatter = newPasture.Nitrogen * LinkedNativeFoodType.NToDMDCoefficient + LinkedNativeFoodType.NToDMDIntercept;
				newPasture.DryMatter = Math.Max(LinkedNativeFoodType.MinimumDMD, newPasture.DryMatter);
				this.LinkedNativeFoodType.Add(newPasture);
			}
		}

		/// <summary>
		/// Function to calculate ecological indicators
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFCalculateEcologicalState")]
		private void OnWFCalculateEcologicalState(object sender, EventArgs e)
		{
			// This event happens after growth and pasture consumption and animal death
			// But before any management, buying and selling of animals.
			
			// update monthly stocking rate total
			StockingRate += Resources.RuminantHerd().Herd.Where(a => a.Location == FeedTypeName).Sum(a => a.AdultEquivalent)/Area;

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
				pool.Nitrogen = Math.Max(pool.Nitrogen * (1 - LinkedNativeFoodType.DecayNitrogen), LinkedNativeFoodType.MinimumNitrogen);
				pool.DMD = Math.Max(pool.DMD * (1 - LinkedNativeFoodType.DecayDMD), LinkedNativeFoodType.MinimumDMD);

				double detach = LinkedNativeFoodType.CarryoverDetachRate;
				if (pool.Age<12)
				{
					detach = LinkedNativeFoodType.DetachRate;
					pool.Age++;
				}
				pool.Set(pool.Amount * (1-detach));
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
			LinkedNativeFoodType.CurrentEcologicalIndicators.LandConditionIndex = LandConditionIndex.Value;
			GrassBasalArea.Modify(utilisation);
			LinkedNativeFoodType.CurrentEcologicalIndicators.LandConditionIndex = LandConditionIndex.Value;

			// Calculate average monthly stocking rate
			// Check number of months to use
			int monthdiff = ((WholeFarm.EcologicalIndicatorsNextDueDate.Year - Clock.StartDate.Year) * 12) + WholeFarm.EcologicalIndicatorsNextDueDate.Month - Clock.StartDate.Month;
			if(monthdiff>=WholeFarm.EcologicalIndicatorsCalculationInterval)
			{
				monthdiff = WholeFarm.EcologicalIndicatorsCalculationInterval;
			}
			StockingRate /= monthdiff;
			LinkedNativeFoodType.CurrentEcologicalIndicators.StockingRate = StockingRate;

			//erosion
			//tree basal area
			//perennials
			Perennials = 92.2 * (1 - Math.Pow(LandConditionIndex.Value,3.35) / Math.Pow(LandConditionIndex.Value,3.35 + 137.7)) - 2.2;
			//%runoff
			//methane
			//soilC
			//TreeC
			//Burnkg
			//methaneFire
			//NOxFire
			//%utilisation
			LinkedNativeFoodType.CurrentEcologicalIndicators.Utilisation = utilisation;

			// Reset all stores
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
					ResourceType = typeof(Land),
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

				// Initial biomass
				double amountToAdd = Area * startingAmount;
				if (amountToAdd <= 0) return;

				// Set up pasture pools to start run based on month and user defined pasture properties
				// Locates the previous five months where growth occurred (Nov-Mar) and applies decomposition to current month
				// This months growth will not be included.

				int month = Clock.Today.Month;
				int monthCount = 0;
				int includedMonthCount = 0;
				double propBiomass = 1.0;
				double currentN = LinkedNativeFoodType.GreenNitrogen;
				// NABSA changes N by 0.8 for particular months. Not needed here as decay included.
				double currentDMD = currentN * LinkedNativeFoodType.NToDMDCoefficient + LinkedNativeFoodType.NToDMDIntercept;
				currentDMD = Math.Max(LinkedNativeFoodType.MinimumDMD, currentDMD);
				LinkedNativeFoodType.Pools.Clear();

				List<GrazeFoodStorePool> newPools = new List<GrazeFoodStorePool>();

				while (includedMonthCount < 5)
				{
					if (month == 0) month = 12;
					if(month<=3 | month>=11)
					{
						// add new pool
						newPools.Add(new GrazeFoodStorePool()
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
				double total = newPools.Sum(a => a.StartingAmount);
				foreach (var pool in newPools)
				{
					pool.Set(amountToAdd * (pool.StartingAmount / total));
				}

				// remove this months growth from pool age 0 to keep biomass at approximately setup.
				// Get this months growth
				double growth = 0; // GRASPFile.Get(xxxxxxxxx)

				double thisMonthsGrowth = 0;
				if (thisMonthsGrowth> 0)
				{
					GrazeFoodStorePool thisMonth = newPools.Where(a => a.Age == 0).FirstOrDefault() as GrazeFoodStorePool;
					if(thisMonth!=null)
					{
						thisMonth.Set(Math.Max(0, thisMonth.Amount - growth));
					}
				}

				// Add to pasture. This will add pool to pasture available store.
				foreach (var pool in newPools)
				{
					LinkedNativeFoodType.Add(pool);
				}
			}
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
