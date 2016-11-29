using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>
	/// Ruminant feeding arbitrator
	/// Applies IAT/NABSA logic to supply feed to herd based on requests from feeding activities.
	/// This arbitrator modifies the Intake and DietDryMatterDigestability properties of the Ruminant cohort/individual
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Arbitrators))]
	public class RuminantArbitratorFeed : Model, IResourceType
	{
		[Link]
		private Resources Resources = null;
		[Link]
		ISummary Summary = null;
		[Link]
		Clock Clock = null;

		//		private Money money;
		private AnimalFoodStore animalFoodStore;
		private GrazeFoodStore grazeStore;
		private List<RuminantFeedRequest> requests = new List<RuminantFeedRequest>();

		/// <summary>
		/// Initialise the current state and locate Resources needed
		/// </summary>
		public void Initialise()
		{
			animalFoodStore = Resources.AnimalFoodStore();
			grazeStore = Resources.GrazeFoodStore();
			//			money = Resources.Money();
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			Initialise();
		}

		/// <summary>An event handler to allow us to reset request list at start of the month</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("StartOfMonth")]
		private void OnStartOfMonth(object sender, EventArgs e)
		{
			requests.Clear();
		}

		/// <summary>An event handler to call for all feed requests to be arbitratoed and supplied to requestors</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFFeedAllocation")]
		private void OnWFFeedAllocation(object sender, EventArgs e)
		{
			// if no requests end
			if (requests.Count() == 0) return;

			// month needed for montly pasture limits.
			int month = Clock.Today.Month;

			// get list of unique priorities provided in requests
			List<int> priorityList = requests.OrderBy(b => b.FeedActivity.FeedPriority).Select(a => a.FeedActivity.FeedPriority).Distinct().ToList();

			foreach (int priority in priorityList)
			{
				// group by feed types requested with each priority
				var feedGrouping = requests.Where(a => a.FeedActivity.FeedPriority == priority).GroupBy(a => a.FeedActivity.FeedType);
				foreach (var feedTypeGroup in feedGrouping)
				{
					IFeedType feedType = feedTypeGroup.Key as IFeedType;

					// determine the total requested for each feedtype
					double amountRequested = feedTypeGroup.Sum(a => Math.Min(a.Amount, (a.Requestor.PotentialIntake - a.Requestor.Intake)) * a.Requestor.Number);

					// if something requested and something available
					if (amountRequested > 0 & feedType.Amount > 0)
					{
						// determine if shortfall
						double deficit = Math.Min(1.0, feedType.Amount / amountRequested);
						if (deficit < 1.0)
						{
							// TODO: work out what do do.
							// Buy fodder
							// Use common land
							// use Animal Food Stores.
						}
						// prepare to take available from resource store
						amountRequested *= deficit;

						// group requests by ruminant breed to allow calculations of limits
						var breedGrouping = feedTypeGroup.GroupBy(a => a.Requestor.BreedParams.Name);
						foreach (var breedGroup in breedGrouping)
						{
							// If feeding on pasture calculate limits based on proportion green
							// This needs to be done for pasture/breed combinations each month
							// So cannot be done for Ruminants or Pasture seperately at start of month
							if(feedType.GetType().Name == "PastureType")
							{
								GrazeFoodStoreType pasture = feedType as GrazeFoodStoreType;

								double total = 0;
								foreach (var pool in pasture.Pools)
								{
									pool.Limit = 1.0;
									total += pool.Amount;
								}

								// if Jan-March then user first three months otherwise use 2
								int greenage = 2;
								if (month <= 3) greenage = 3;

								double green = pasture.Pools.Where(a => (a.Age <= greenage)).Sum(b => b.Amount);
								double propgreen = green / total;
								double greenlimit = breedGroup.FirstOrDefault().Requestor.BreedParams.GreenDietMax * (1 - Math.Exp(-breedGroup.FirstOrDefault().Requestor.BreedParams.GreenDietCoefficient * ((propgreen * 100.0) - breedGroup.FirstOrDefault().Requestor.BreedParams.GreenDietZero)));
								greenlimit = Math.Max(0.0, greenlimit);
								if (propgreen>90)
								{
									greenlimit = 100;
								}

								foreach (var pool in pasture.Pools.Where(a => a.Age <= greenage))
								{
									pool.Limit = greenlimit/100.0;
								}
							}

							// update Ruminants Intake, ProteinConcentration and DietDryMatterDigestibility
							foreach (var request in feedTypeGroup)
							{
								switch (request.FeedActivity.FeedType.GetType().Name)
								{
									case "GrazeFoodStoreType":
										// take from pools as specified for the individual
										GrazeFoodStoreType pasture = request.FeedActivity.FeedType as GrazeFoodStoreType;
										double amountRequired = request.Requestor.PotentialIntake - request.Requestor.Intake;

										int index = 0;
										bool secondTakeFromPools = request.Requestor.BreedParams.StrictFeedingLimits;
										while (amountRequired > 0)
										{
											// limiter obtained from filter group or unlimited if second take of pools
											double limiter = 1.0;
											if (!secondTakeFromPools)
											{
												limiter = pasture.Pools[index].Limit;
											}

											double amountToRemove = Math.Min(pasture.Pools[index].Amount, amountRequired * limiter);
											amountRequired -= amountToRemove;

											request.Amount = amountToRemove;
											pasture.Pools[index].Remove(request);

											index++;
											if (index >= pasture.Pools.Count)
											{
												// if we've already given second chance to get food so finish without full satisfying individual
												// or strict feeding limits are enforced
												if (secondTakeFromPools) break;
												// if not strict limits allow a second request for food from previously limited pools.
												secondTakeFromPools = true;
												index = 0;
											}
										}
										break;
									case "AnimalFoodStoreType":
										// take directly from store if available
										request.Amount *= deficit;
										feedTypeGroup.FirstOrDefault().FeedActivity.FeedType.Remove(request);
										break;
									default:
										string error = String.Format("Unrecognised feed type {0} in {1} of name {2}", request.GetType().ToString(), this.GetType().ToString(), this.Name);
										Summary.WriteWarning(this, error);
										throw new Exception("Unrecognised Feedtype found in feed request");
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Add this Amount to the existing Amount.
		/// </summary>
		/// <param name="AddAmount">Amount to add</param>
		/// <param name="ActivityName">Name of activity requesting resource</param>
		/// <param name="UserName">Name of individual requesting resource</param>
		public void Add(double AddAmount, string ActivityName, string UserName)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Remove Food
		/// </summary>
		/// <param name="RemoveAmount">nb. This is a positive value not a negative value.</param>
		/// <param name="ActivityName">Name of activity requesting resource</param>
		/// <param name="UserName">Name of individual requesting resource</param>
		public void Remove(double RemoveAmount, string ActivityName, string UserName)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Remove this request by adding to list of requests and arbitrating
		/// </summary>
		/// <param name="RemoveRequest">A suitable request object with required details</param>
		public void Remove(object RemoveRequest)
		{
			requests.Add(RemoveRequest as RuminantFeedRequest);
		}

		/// <summary>
		/// Set the amount to this new value.
		/// </summary>
		/// <param name="NewAmount">Amount to set</param>
		public void Set(double NewAmount)
		{
			throw new NotImplementedException();
		}

	}
}