using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>Ruminant herd management activity</summary>
	/// <summary>This activity will maintain a breeding herd at the desired levels of age/breeders etc</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Activities))]
	public class RuminantActivityManage: Model
	{
		[Link]
		private Resources Resources = null;
		[Link]
		Clock Clock = null;

		/// <summary>
		/// Name of herd to breed
		/// </summary>
		[Description("Name of herd to manage")]
		public string HerdName { get; set; }

		/// <summary>
		/// Maximum number of breeders that can be kept
		/// </summary>
		[Description("Maximum number of breeders that can be kept")]
		public int MaximumBreedersKept { get; set; }

		/// <summary>
		/// Maximum breeder age (months) for culling
		/// </summary>
		[Description("Maximum breeder age (months) for culling")]
		public double MaximumBreederAge { get; set; }

		/// <summary>
		/// Maximum number of breeding sires kept
		/// </summary>
		[Description("Maximum number of breeding sires kept")]
		public int MaximumSiresKept { get; set; }

		/// <summary>
		/// Maximum bull age (months) for culling
		/// </summary>
		[Description("Maximum bull age (months) for culling")]
		public double MaximumBullAge { get; set; }

		/// <summary>
		/// Selling age (months)
		/// </summary>
		[Description("Selling age (months)")]
		public double SellingAge { get; set; }

		/// <summary>
		/// Selling weight (kg)
		/// </summary>
		[Description("Selling weight (kg)")]
		public double SellingWeight { get; set; }

		/// <summary>
		/// Month to undertake management (1-12)
		/// </summary>
		[Description("Month to undertake management (1-12)")]
		public int ManagementMonth { get; set; }

		/// <summary>
		/// Manage every month
		/// </summary>
		[Description("Manage every month")]
		public bool MonthlyManagement { get; set; }

		/// <summary>
		/// Weaning age (months)
		/// </summary>
		[Description("Weaning age (months)")]
		public double WeaningAge { get; set; }

		/// <summary>
		/// Weaning weight (kg)
		/// </summary>
		[Description("Weaning weight (kg)")]
		public double WeaningWeight { get; set; }

		/// <summary>
		/// Vet costs (per head/year)
		/// </summary>
		[Description("Vet costs (per head/year)")]
		public double VetCosts { get; set; }

		/// <summary>
		/// Dip and spray costs (per head/year)
		/// </summary>
		[Description("Dip and spray costs (per head/year)")]
		public double DipCosts { get; set; }

		/// <summary>
		/// Vaccines and drenches costs (per head/year)
		/// </summary>
		[Description("Vaccines and drenches costs (per head/year)")]
		public double VaccineCosts { get; set; }

		/// <summary>
		/// Perform selling of young females the same as males
		/// </summary>
		[Description("Perform selling of young females the same as males")]
		public bool SellFemalesLikeMales { get; set; }

		/// <summary>An event handler to call for all herd management activities</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalManage")]
		private void OnWFAnimalManage(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd.Where(a => a.HerdName == HerdName).ToList();

			RuminantType breedParams;
			// get breedParams when no herd remaining
			if (herd.Count() == 0)
			{
				breedParams = Resources.GetResourceItem("Ruminants", HerdName) as RuminantType;
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
			foreach (var ind in herd.Where(a => a.Weaned & (SellFemalesLikeMales ? true : (a.Gender == Sex.Male)) & (a.Age >= SellingAge ^ a.Weight >= SellingWeight)))
			{
				bool sell = true;
				if (ind.GetType() == typeof(RuminantMale))
				{
					// don't sell breeding sires.
					sell = !((ind as RuminantMale).BreedingSire);
				}
				if (sell)
				{
					ind.SaleFlag = Common.HerdChangeReason.AgeWeightSale;
				}
			}

			// if management month
			if (Clock.Today.Month == ManagementMonth ^ MonthlyManagement)
			{
				// Perform weaning
				foreach (var ind in herd.Where(a => a.Weaned == false))
				{
					if (ind.Age >= WeaningAge ^ ind.Weight >= WeaningWeight)
					{
						ind.Wean();
					}
				}

				// check for maximum age (females and males have different cutoffs)
				foreach (var ind in herd.Where(a => a.Age >= ((a.Gender == Sex.Female) ? MaximumBreederAge : MaximumBullAge)))
				{
					ind.SaleFlag = Common.HerdChangeReason.MaxAgeSale;
				}

				// MALES
				// check for breeder bulls after sale of old individuals and buy/sell
				int numberinherd = herd.Where(a => a.Gender == Sex.Male & a.SaleFlag == Common.HerdChangeReason.None).Cast<RuminantMale>().Where(a => a.BreedingSire).Count();

				if (numberinherd > MaximumSiresKept)
				{
					// sell bulls
					// What rule? oldest first as they may be lost soonest
					int numberToRemove = MaximumSiresKept - numberinherd;
					foreach (var male in herd.Where(a => a.Gender == Sex.Male).Cast<RuminantMale>().Where(a => a.BreedingSire).OrderByDescending(a => a.Age).Take(numberToRemove))
					{
						male.SaleFlag = Common.HerdChangeReason.ExcessBullSale;
					}
				}
				else if(numberinherd < MaximumSiresKept)
				{
					// remove young bulls from sale herd to replace breed bulls (not those sold because too old)
					foreach (RuminantMale male in herd.Where(a => a.Gender == Sex.Male & a.SaleFlag == Common.HerdChangeReason.AgeWeightSale).OrderByDescending(a => a.Weight))
					{
						male.SaleFlag = Common.HerdChangeReason.None;
						male.BreedingSire = true;
						numberinherd++;
						if (numberinherd >= MaximumSiresKept) break;
					}

					// if still insufficient buy bulls.
					if (numberinherd < MaximumSiresKept)
					{
						int numberToBuy = Convert.ToInt32((MaximumSiresKept - numberinherd) * 0.05);

						for (int i = 0; i < numberToBuy; i++)
						{
							RuminantMale newbull = new RuminantMale();
							newbull.Age = 48;
							newbull.HerdName = HerdName;
							newbull.BreedingSire = true;
							newbull.BreedParams = breedParams;
							newbull.Gender = Sex.Male;
							newbull.ID = ruminantHerd.NextUniqueID;
							newbull.Weight = 450;
							newbull.HighWeight = newbull.Weight;
							newbull.SaleFlag = Common.HerdChangeReason.SirePurchase;

							// add to purchase request list and await purchase in Buy/ Sell
							ruminantHerd.PurchaseIndividuals.Add(newbull);
						}
					}
				}

				// FEMALES
				// check for maximum number of breeders remaining after sale and buy/sell
				numberinherd = herd.Where(a => a.Gender == Sex.Female & a.Age >= a.BreedParams.MinimumAge1stMating & a.SaleFlag == Common.HerdChangeReason.None).Count();

				if (numberinherd > MaximumBreedersKept)
				{
					// sell breeders
					// What rule? oldest first as they may be lost soonest
					// should keep pregnant females... and young...
					// this will currently remove pregnant females and females with suckling calf
					int numberToRemove = MaximumBreedersKept - numberinherd;
					foreach (var female in herd.Where(a => a.Gender == Sex.Female & a.Age >= a.BreedParams.MinimumAge1stMating).OrderByDescending(a => a.Age).Take(numberToRemove))
					{
						female.SaleFlag = Common.HerdChangeReason.ExcessBreederSale;
					}
				}
				else
				{
					// remove young females from sale herd to replace breeders (not those sold because too old)
					foreach (RuminantFemale female in herd.Where(a => a.Gender == Sex.Female & a.SaleFlag == Common.HerdChangeReason.AgeWeightSale).OrderByDescending(a => a.Weight))
					{
						female.SaleFlag = Common.HerdChangeReason.None;
						numberinherd++;
						if (numberinherd >= MaximumBreedersKept) break;
					}

					// if still insufficient buy breeders.
					if (numberinherd < MaximumBreedersKept)
					{
						int ageOfHeifer = 12;
						double weightOfHeifer = 260;

						int numberToBuy = Convert.ToInt32((MaximumBreedersKept - numberinherd) * 0.05);

						for (int i = 0; i < numberToBuy; i++)
						{
							RuminantFemale newheifer = new RuminantFemale();
							newheifer.Age = ageOfHeifer;
							newheifer.HerdName = HerdName;
							newheifer.BreedParams = breedParams;
							newheifer.Gender = Sex.Female;
							newheifer.ID = ruminantHerd.NextUniqueID;
							newheifer.Weight = weightOfHeifer;
							newheifer.HighWeight = newheifer.Weight;
							newheifer.SaleFlag = Common.HerdChangeReason.HeiferPurchase;

							// add to purchase request list and await purchase in Buy/ Sell
							ruminantHerd.PurchaseIndividuals.Add(newheifer);
						}
					}

				}
			}
		}
	}
}
