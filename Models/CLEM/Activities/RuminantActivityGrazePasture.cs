using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
	/// <summary>Ruminant graze activity</summary>
	/// <summary>This activity determines how a ruminant group will graze</summary>
	/// <summary>It is designed to request food via a food store arbitrator</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(CLEMActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs grazing of all herds within a specified pasture (paddock) in the simulation.")]
    public class RuminantActivityGrazePasture : CLEMActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;
		[Link]
		Clock Clock = null;

		/// <summary>
		/// Number of hours grazed
		/// Based on 8 hour grazing days
		/// Could be modified to account for rain/heat walking to water etc.
		/// </summary>
		[Description("Number of hours grazed (based on 8 hr grazing day)")]
        [Required, Range(0, 8, ErrorMessage = "Value based on maximum 8 hour grazing day")]
        public double HoursGrazed { get; set; }

		/// <summary>
		/// Name of paddock or pasture to graze
		/// </summary>
		[Description("Name of GrazeFoodStoreType to graze")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of Graze Food Store required")]
        public string GrazeFoodStoreTypeName { get; set; }

		/// <summary>
		/// paddock or pasture to graze
		/// </summary>
		[XmlIgnore]
		public GrazeFoodStoreType GrazeFoodStoreModel { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			// limit to 8 hours grazing max
			HoursGrazed = Math.Min(8.0, HoursGrazed);

			// If GrazeFoodStoreType model has not been set use name
			if (GrazeFoodStoreModel == null)
			{
				GrazeFoodStoreModel = Resources.GrazeFoodStore().GetByName("GrazeFoodStoreTypeName") as GrazeFoodStoreType;

				//Create list of children by breed
				foreach (RuminantType herdType in Resources.RuminantHerd().Children)
				{
					RuminantActivityGrazePastureBreed ragpb = new RuminantActivityGrazePastureBreed();
					ragpb.GrazeFoodStoreModel = GrazeFoodStoreModel;
					ragpb.RuminantTypeModel = herdType;
					ActivityList.Add(ragpb);
				}
			}
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> GetResourcesNeededForActivity()
		{
			// This method does not take any resources but is used to arbitrate resources for all breed grazing activities it contains

			// determine pasture quality from all pools (DMD) at start of grazing
			double pastureDMD = GrazeFoodStoreModel.DMD;

			// Reduce potential intake based on pasture quality for the proportion consumed.
			// TODO: check that this doesn't need to be performed for each breed based on how pasture taken
			double potentialIntakeLimiter = 1.0;
			if ((0.8 - GrazeFoodStoreModel.IntakeTropicalQualityCoefficient - pastureDMD / 100) >= 0)
			{
				potentialIntakeLimiter = GrazeFoodStoreModel.IntakeQualityCoefficient * (0.8 - GrazeFoodStoreModel.IntakeTropicalQualityCoefficient - pastureDMD / 100);
			}

			// check nested graze breed requirements for this pasture
			double totalNeeded = 0;
			foreach (RuminantActivityGrazePastureBreed item in ActivityList)
			{
				item.ResourceRequestList = null;
				item.PotentialIntakePastureQualityLimiter = potentialIntakeLimiter;
				item.GetResourcesNeededForActivity();
				if (item.ResourceRequestList != null && item.ResourceRequestList.Count > 0)
				{
					totalNeeded += item.ResourceRequestList[0].Required;
				}
			}

			// Check available resources
			// This determines the proportional amount available for competing breeds with different green diet proportions
			// It does not truly account for how the pasture is provided from pools but will suffice unless more detailed model developed
			double available = GrazeFoodStoreModel.Amount;
			double limit = 0;
			if(totalNeeded>0)
			{
				limit = Math.Min(1.0, available / totalNeeded);
			}

			// apply limits to children
			foreach (RuminantActivityGrazePastureBreed item in ActivityList)
			{
				item.GrazingCompetitionLimiter = limit;
				// store kg/ha available for consumption calculation
				item.BiomassPerHectare = GrazeFoodStoreModel.kgPerHa;

				// calculate breed feed limits
				if(item.PoolFeedLimits == null)
				{
					item.PoolFeedLimits = new List<GrazeBreedPoolLimit>();
				}
				else
				{
					item.PoolFeedLimits.Clear();
				}

				foreach (var pool in GrazeFoodStoreModel.Pools)
				{
					item.PoolFeedLimits.Add(new GrazeBreedPoolLimit() { Limit=1.0, Pool=pool });
				}

				// if Jan-March then user first three months otherwise use 2
				int greenage = (Clock.Today.Month <= 3) ? 3 : 2;

				double green = GrazeFoodStoreModel.Pools.Where(a => (a.Age <= greenage)).Sum(b => b.Amount);
				double propgreen = green / available;
				double greenlimit =  item.RuminantTypeModel.GreenDietMax * (1 - Math.Exp(-item.RuminantTypeModel.GreenDietCoefficient * ((propgreen * 100.0) - item.RuminantTypeModel.GreenDietZero)));
				greenlimit = Math.Max(0.0, greenlimit);
				if (propgreen > 90)
				{
					greenlimit = 100;
				}

				foreach (var pool in item.PoolFeedLimits.Where(a => a.Pool.Age <= greenage))
				{
					pool.Limit = greenlimit / 100.0;
				}
			}
			return null;
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
}
