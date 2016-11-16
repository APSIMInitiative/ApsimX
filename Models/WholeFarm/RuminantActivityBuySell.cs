using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	/// <summary>Ruminant sales activity</summary>
	/// <summary>This activity undertakes the sale and transport of any individuals f;agged for sale.</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Activities))]
	public class RuminantActivityBuySell : Model
	{
		[Link]
		private Resources Resources = null;
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// Current value of individuals in the herd
		/// </summary>
		[XmlIgnore]
		public List<RuminantValue> PriceList;

		/// <summary>
		/// Name of breed to buy or sell
		/// </summary>
		[Description("Name of breed to buy or sell")]
		public string BreedName { get; set; }

		/// <summary>
		/// Pricing of breeding sire
		/// </summary>
		[Description("Pricing of breeding sire")]
		public double BreedingSirePrice { get; set; }

		/// <summary>
		/// Distance to market
		/// </summary>
		[Description("Distance to market (km)")]
		public double DistanceToMarket { get; set; }

		/// <summary>
		/// Cost of trucking ($/km/truck)
		/// </summary>
		[Description("Cost of trucking ($/km/truck)")]
		public double CostPerKmTrucking { get; set; }

		/// <summary>
		/// Number of 450kg animals per truck load
		/// </summary>
		[Description("Number of 450kg animals per truck load")]
		public double Number450kgPerTruck { get; set; }

		/// <summary>
		/// Yard fees when sold ($/head)
		/// </summary>
		[Description("Yard fees when sold ($/head)")]
		public double YardFees { get; set; }

		/// <summary>
		/// MLA fees when sold ($/head)
		/// </summary>
		[Description("MLA fees when sold ($/head)")]
		public double MLAFees { get; set; }

		/// <summary>
		/// Minimum truck loads before selling (0 continuous sales)
		/// </summary>
		[Description("Minimum truck loads before selling (0 continuous sales)")]
		public double MinimumTrucksBeforeSelling { get; set; }

		/// <summary>
		/// Minimum truck loads before selling (0 continuous sales)
		/// </summary>
		[Description("Minimum proportion of truck loads before selling (0 continuous sales)")]
		public double MinimumLoadBeforeSelling { get; set; }

		/// <summary>
		/// Sales commission to agent (%)
		/// </summary>
		[Description("Sales commission to agent (%)")]
		public double SalesCommission { get; set; }

		/// <summary>An event handler to allow us to initialise herd pricing.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("StartOfSimulation")]
		private void OnStartOfSimulation(object sender, EventArgs e)
		{
			// initialise herd price list
			PriceList = new List<RuminantValue>();

			foreach (RuminantPricing priceGroup in this.Children.Where(a => a.GetType() == typeof(RuminantPricing)))
			{
				foreach (RuminantPriceEntry price in priceGroup.Children.Where(a => a.GetType() == typeof(RuminantPriceEntry)))
				{
					RuminantValue val = new RuminantValue();
					val.Age = price.Age;
					val.PurchaseValue = price.PurchaseValue;
					val.Gender = price.Gender;
					val.Breed = this.BreedName;
					val.SellValue = price.SellValue;
					val.Style = priceGroup.PricingStyle;
					PriceList.Add(val);
				}
			}
		}

		/// <summary>An event handler to call for animal purchases</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalBuy")]
		private void OnWFAnimalBuy(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();

			Finance Accounts = Resources.FinanceResource() as Finance;
			FinanceType bankAccount = Accounts.GetFirst();

			var newRequests = ruminantHerd.PurchaseIndividuals.Where(a => a.BreedParams.Breed == BreedName).ToList();
			foreach (var newgroup in newRequests.GroupBy(a => a.SaleFlag))
			{
				double fundsAvailable = 100000000;
				if (bankAccount != null)
				{
					fundsAvailable = bankAccount.FundsAvailable;
				}
				double cost = 0;
				foreach (var newind in newgroup)
				{
					double value = 0;
					if (newgroup.Key == Common.HerdChangeReason.SirePurchase)
					{
						value = BreedingSirePrice;
					}
					else
					{
						RuminantValue getvalue = PriceList.Where(a => a.Age < newind.Age).OrderBy(a => a.Age).LastOrDefault();
						value = getvalue.PurchaseValue * ((getvalue.Style == Common.PricingStyleType.perKg) ? newind.Weight : 1.0);
					}
					if (cost + value <= fundsAvailable)
					{
						ruminantHerd.AddRuminant(newind);
						cost += value;
					}
					else
					{
						break;
					}
				}
				if (bankAccount != null)
				{
					bankAccount.Remove(cost, this.Name, newgroup.Key.ToString());
				}
			}
		}

		/// <summary>An event handler to call for animal sales</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalSell")]
		private void OnWFAnimalSell(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();

			Finance Accounts = Resources.FinanceResource() as Finance;
			FinanceType bankAccount = Accounts.GetFirst();

			int trucks = 0;
			double saleValue = 0;
			double saleWeight = 0;
			int head = 0;

			// get current untrucked list of animals flagged for sale
			List<Ruminant> herd = ruminantHerd.Herd.Where(a => a.SaleFlag != Common.HerdChangeReason.None & a.Breed == BreedName).OrderByDescending(a => a.Weight).ToList();

			// if sale herd > min loads before allowing sale
			if (herd.Select(a => a.Weight / 450.0).Sum() / Number450kgPerTruck >= MinimumTrucksBeforeSelling)
			{
				// while truck to fill
				while (herd.Select(a => a.Weight / 450.0).Sum() / Number450kgPerTruck > MinimumLoadBeforeSelling)
				{
					bool nonloaded = true;
					trucks++;
					double load450kgs = 0;
					// while truck below carrying capacity load individuals
					foreach (var ind in herd)
					{
						if(load450kgs + (ind.Weight / 450.0) <= Number450kgPerTruck)
						{
							nonloaded = false;
							head++;
							load450kgs += ind.Weight / 450.0;
							RuminantValue getvalue = PriceList.Where(a => a.Age < ind.Age).OrderBy(a => a.Age).LastOrDefault();
							saleValue += getvalue.SellValue * ((getvalue.Style == Common.PricingStyleType.perKg) ? ind.Weight : 1.0);
							saleWeight += ind.Weight;
							ruminantHerd.RemoveRuminant(ind);
						}
					}
					if(nonloaded)
					{
						Summary.WriteWarning(this, String.Format("There was a problem loading the sale truck as sale individuals did not meet the loading criteria for breed {0}", BreedName));
						break;
					}
					herd = ruminantHerd.Herd.Where(a => a.SaleFlag != Common.HerdChangeReason.None & a.Breed == BreedName).OrderByDescending(a => a.Weight).ToList();
				}

				if (trucks > 0 & bankAccount != null)
				{
					// calculate transport costs
					double transportCost = trucks * DistanceToMarket * CostPerKmTrucking;
					bankAccount.Remove(transportCost, this.Name, "Transport");
					// calculate MLA fees
					double mlaCost = head * MLAFees;
					bankAccount.Remove(mlaCost, this.Name, "R&DFee");
					// calculate yard fees
					double yardCost = head * YardFees;
					bankAccount.Remove(yardCost, this.Name, "YardCosts");
					// calculate commission
					double commissionCost = saleValue * SalesCommission;
					bankAccount.Remove(commissionCost, this.Name, "SalesCommission");

					// add and remove from bank
					bankAccount.Add(saleValue, this.Name, "Sales");
				}
			}
		}
	}
}