using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Models.WholeFarm.Activities
{
	/// <summary>Ruminant herd management activity</summary>
	/// <summary>This activity will maintain a breeding herd at the desired levels of age/breeders etc</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class RuminantActivityManage: WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;

        /// <summary>
        /// Name of herd to breed
        /// </summary>
        [Description("Name of herd to manage")]
        [Required]
        public string HerdName { get; set; }

        /// <summary>
        /// Maximum number of breeders that can be kept
        /// </summary>
        [Description("Maximum number of breeders to be kept")]
        [Required]
        public int MaximumBreedersKept { get; set; }

		/// <summary>
		/// Minimum number of breeders that can be kept
		/// </summary>
		[Description("Minimum number of breeders to be kept")]
        [Required]
        public int MinimumBreedersKept { get; set; }

        /// <summary>
        /// Allow breeder purchases
        /// </summary>
        [Description("Allow breeder purchases")]
        [Required]
        public bool AllowBreederPurchases { get; set; }

        /// <summary>
        /// Maximum breeder age (months) for culling
        /// </summary>
        [Description("Maximum breeder age (months) for culling")]
        [Required]
        public double MaximumBreederAge { get; set; }

		/// <summary>
		/// Maximum number of breeding sires kept
		/// </summary>
		[Description("Maximum number of breeding sires kept")]
        [Required]
        public int MaximumSiresKept { get; set; }

		/// <summary>
		/// Maximum bull age (months) for culling
		/// </summary>
		[Description("Maximum bull age (months) for culling")]
        [Required]
        public double MaximumBullAge { get; set; }

		/// <summary>
		/// Allow natural herd replacement of sires
		/// </summary>
		[Description("Allow sire replacement from herd")]
        [Required]
        public bool AllowSireReplacement { get; set; }

		/// <summary>
		/// Male selling age (months)
		/// </summary>
		[Description("Male selling age (months)")]
        [Required]
        public double MaleSellingAge { get; set; }

		/// <summary>
		/// Male selling weight (kg)
		/// </summary>
		[Description("Male selling weight (kg)")]
        [Required]
        public double MaleSellingWeight { get; set; }

		///// <summary>
		///// Month to undertake management (1-12) and assign costs
		///// </summary>
		//[Description("Month to undertake management (1-12) and assign costs")]
		//[System.ComponentModel.DefaultValueAttribute(12)]
  //      [Required, Range(1, 12, ErrorMessage = "Value must represent a month from 1 (Jan) to 12 (Dec)")]
  //      public int ManagementMonth { get; set; }

		///// <summary>
		///// Manage every month
		///// </summary>
		//[Description("Manage every month")]
  //      [Required]
  //      public bool MonthlyManagement { get; set; }

		/// <summary>
		/// Weaning age (months)
		/// </summary>
		[Description("Weaning age (months)")]
        [Required]
        public double WeaningAge { get; set; }

		/// <summary>
		/// Weaning weight (kg)
		/// </summary>
		[Description("Weaning weight (kg)")]
        [Required]
        public double WeaningWeight { get; set; }

		/// <summary>
		/// Name of GrazeFoodStore (paddock) to place purchases in for grazing (leave blank for general yards)
		/// </summary>
		[Description("Name of GrazeFoodStore (paddock) to place purchases in (leave blank for general yards)")]
        [Required]
        public string GrazeFoodStoreName { get; set; }

		/// <summary>
		/// Minimum pasture (kg/ha) before restocking if placed in paddock
		/// </summary>
		[Description("Minimum pasture (kg/ha) before restocking if placed in paddock")]
        [Required]
        public double MinimumPastureBeforeRestock { get; set; }

		/// <summary>
		/// Perform selling of young females the same as males
		/// </summary>
		[Description("Perform selling of young females the same as males")]
        [Required]
        public bool SellFemalesLikeMales { get; set; }

		/// <summary>
		/// Store graze 
		/// </summary>
		private GrazeFoodStoreType foodStore;

		/// <summary>
		/// Constructor
		/// </summary>
		public RuminantActivityManage()
		{
			this.SetDefaults();
		}

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // check GrazeFoodStoreExists
            if (GrazeFoodStoreName == null) GrazeFoodStoreName = "";
			if(GrazeFoodStoreName!="")
			{
				foodStore = Resources.GetResourceItem(this, typeof(GrazeFoodStore), GrazeFoodStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
			}
		}

		/// <summary>An event handler to call for all herd management activities</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalManage")]
		private void OnWFAnimalManage(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			// clear store of individuals to try and purchase
			ruminantHerd.PurchaseIndividuals.Clear();

			List<Ruminant> herd = ruminantHerd.Herd.Where(a => a.HerdName == HerdName).ToList();

			RuminantType breedParams;
			// get breedParams when no herd remaining
			if (herd.Count() == 0)
			{
				breedParams = Resources.GetResourceItem(this, typeof(RuminantHerd), HerdName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as RuminantType;
			}
			else
			{
				breedParams = herd.FirstOrDefault().BreedParams;
			}

			// can sell off males any month as per NABSA
			// if we don't need this monthly, then it goes into next if statement with herd declaration
			// NABSA MALES - weaners, 1-2, 2-3 and 3-4 yo, we check for any male weaned and not a breeding sire.
			// check for sell age/weight of young males
			// if SellYoungFemalesLikeMales then all apply to both sexes else only males.
			foreach (var ind in herd.Where(a => a.Weaned & (SellFemalesLikeMales ? true : (a.Gender == Sex.Male)) & (a.Age >= MaleSellingAge || a.Weight >= MaleSellingWeight)))
			{
				bool sell = true;
				if (ind.GetType() == typeof(RuminantMale))
				{
					// don't sell breeding sires.
					sell = !((ind as RuminantMale).BreedingSire);
				}
				if (sell)
				{
					ind.SaleFlag = HerdChangeReason.AgeWeightSale;
				}
			}

			// if management month
			if (this.TimingOK)
			{
				bool sufficientFood = true;
				if(foodStore != null)
				{
					sufficientFood = (foodStore.TonnesPerHectare * 1000) > MinimumPastureBeforeRestock;
				}

				// Perform weaning
				foreach (var ind in herd.Where(a => a.Weaned == false))
				{
					if (ind.Age >= WeaningAge || ind.Weight >= WeaningWeight)
					{
						ind.Wean();
					}
				}

				// check for maximum age (females and males have different cutoffs)
				foreach (var ind in herd.Where(a => a.Age >= ((a.Gender == Sex.Female) ? MaximumBreederAge : MaximumBullAge)))
				{
					ind.SaleFlag = HerdChangeReason.MaxAgeSale;
				}

				// MALES
				// check for breeder bulls after sale of old individuals and buy/sell
				int numberMaleInHerd = herd.Where(a => a.Gender == Sex.Male & a.SaleFlag == HerdChangeReason.None).Cast<RuminantMale>().Where(a => a.BreedingSire).Count();

				// Number of females
				int numberFemaleInHerd = herd.Where(a => a.Gender == Sex.Female & a.Age >= a.BreedParams.MinimumAge1stMating & a.SaleFlag == HerdChangeReason.None).Count();

				if (numberMaleInHerd > MaximumSiresKept)
				{
					// sell bulls
					// What rule? oldest first as they may be lost soonest
					int numberToRemove = MaximumSiresKept - numberMaleInHerd;
					foreach (var male in herd.Where(a => a.Gender == Sex.Male).Cast<RuminantMale>().Where(a => a.BreedingSire).OrderByDescending(a => a.Age).Take(numberToRemove))
					{
						male.SaleFlag = HerdChangeReason.ExcessBullSale;
					}
				}
				else if(numberMaleInHerd < MaximumSiresKept)
				{
					if ((foodStore == null) || (sufficientFood))
					{
						if (AllowSireReplacement)
						{
							// remove young bulls from sale herd to replace breed bulls (not those sold because too old)
							foreach (RuminantMale male in herd.Where(a => a.Gender == Sex.Male & a.SaleFlag == HerdChangeReason.AgeWeightSale).OrderByDescending(a => a.Weight))
							{
								male.SaleFlag = HerdChangeReason.None;
								male.BreedingSire = true;
								numberMaleInHerd++;
								if (numberMaleInHerd >= MaximumSiresKept) break;
							}
						}

						// if still insufficient buy bulls.
						if (numberMaleInHerd < MaximumSiresKept)
						{
							// limit by breeders as proportion of max breeders so we don't spend alot on sires when building the herd and females more valuable
							double propOfBreeders = numberFemaleInHerd / MaximumBreedersKept;
							int sires = Convert.ToInt32(Math.Ceiling(MaximumSiresKept * propOfBreeders));
							int numberToBuy = Math.Max(0, sires - numberMaleInHerd);

							for (int i = 0; i < numberToBuy; i++)
							{
								RuminantMale newbull = new RuminantMale();
								newbull.Location = GrazeFoodStoreName;
								newbull.Age = 48;
								newbull.Breed = breedParams.Breed;
								newbull.HerdName = HerdName;
								newbull.BreedingSire = true;
								newbull.BreedParams = breedParams;
								newbull.Gender = Sex.Male;
								newbull.ID = 0; // ruminantHerd.NextUniqueID;
								newbull.Weight = 450;
								newbull.HighWeight = newbull.Weight;
								newbull.SaleFlag = HerdChangeReason.SirePurchase;

								// add to purchase request list and await purchase in Buy/ Sell
								ruminantHerd.PurchaseIndividuals.Add(newbull);
							}
						}
					}
				}

				// FEMALES
				// check for maximum number of breeders remaining after sale and buy/sell
				if (numberFemaleInHerd > MaximumBreedersKept)
				{
					// sell breeders
					// What rule? oldest first as they may be lost soonest
					// should keep pregnant females... and young...
					// this will currently remove pregnant females and females with suckling calf

					int numberToRemove = Convert.ToInt32((numberFemaleInHerd-MaximumBreedersKept));
					foreach (var female in herd.Where(a => a.Gender == Sex.Female & a.Age >= a.BreedParams.MinimumAge1stMating).OrderByDescending(a => a.Age).Take(numberToRemove))
					{
						female.SaleFlag = HerdChangeReason.ExcessBreederSale;
					}
				}
				else
				{
					if ((foodStore == null) || (sufficientFood))
					{
						// remove young females from sale herd to replace breeders (not those sold because too old)
						foreach (RuminantFemale female in herd.Where(a => a.Gender == Sex.Female & a.SaleFlag == HerdChangeReason.AgeWeightSale).OrderByDescending(a => a.Age))
						{
							female.SaleFlag = HerdChangeReason.None;
							numberFemaleInHerd++;
							if (numberFemaleInHerd > MaximumBreedersKept) break;
						}

						// if still insufficient buy breeders.
						if (numberFemaleInHerd < MinimumBreedersKept & sufficientFood & AllowBreederPurchases)
						{
							int ageOfHeifer = 12;
							double weightOfHeifer = 260;

							// buy 5% more to account for deaths before these individuals grow to breeding age
							int numberToBuy = Convert.ToInt32((MinimumBreedersKept - numberFemaleInHerd)*1.05);

							for (int i = 0; i < numberToBuy; i++)
							{
								RuminantFemale newheifer = new RuminantFemale();
								newheifer.Location = GrazeFoodStoreName;
								newheifer.Age = ageOfHeifer;
								newheifer.Breed = breedParams.Breed;
								newheifer.HerdName = HerdName;
								newheifer.BreedParams = breedParams;
								newheifer.Gender = Sex.Female;
								newheifer.ID = 0;// ruminantHerd.NextUniqueID;
								newheifer.Weight = weightOfHeifer;
								newheifer.HighWeight = newheifer.Weight;
								newheifer.SaleFlag = HerdChangeReason.HeiferPurchase;

								// add to purchase request list and await purchase in Buy/ Sell
								ruminantHerd.PurchaseIndividuals.Add(newheifer);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> GetResourcesNeededForActivity()
		{
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
