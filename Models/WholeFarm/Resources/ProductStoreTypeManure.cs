using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm.Resources
{
	///<summary>
	/// Store for manure
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(ProductStore))]
	public class ProductStoreTypeManure: ProductStoreType
	{
		/// <summary>
		/// List of all uncollected manure stores
		/// These present manure in the field and yards
		/// </summary>
		public List<ManureStoreUncollected> UncollectedStores = new List<ManureStoreUncollected>();

		/// <summary>
		/// Biomass decay rate each time step
		/// </summary>
		[Description("Biomass decay rate each time step")]
		public double DecayRate { get; set; }

		/// <summary>
		/// Moisture decay rate each time step
		/// </summary>
		[Description("Moisture decay rate each time step")]
		public double MoistureDecayRate { get; set; }

		/// <summary>
		/// Proportion moisture of fresh manure
		/// </summary>
		[Description("Proportion moisture of fresh manure")]
		public double ProportionMoistureFresh { get; set; }

		/// <summary>
		/// Maximum age manure lasts
		/// </summary>
		[Description("Maximum age (time steps) manure lasts")]
		public int MaximumAge { get; set; }

		/// <summary>
		/// Method to add uncollected manure to stores
		/// </summary>
		/// <param name="storeName">Name of store to add manure to</param>
		/// <param name="amount">Amount (dry weight) of manure to add</param>
		public void AddUncollectedManure(string storeName, double amount)
		{
			ManureStoreUncollected store = UncollectedStores.Where(a => a.Name.ToLower() == storeName.ToLower()).FirstOrDefault();
			if(store == null)
			{
				store = new ManureStoreUncollected() { Name = storeName };
				UncollectedStores.Add(store);
			}
			ManurePool pool = store.Pools.Where(a => a.Age == 0).FirstOrDefault();
			if(pool == null)
			{
				pool = new ManurePool() { Age = 0 };
				store.Pools.Add(pool);
			}
			pool.Amount += amount;
		}

		/// <summary>
		/// Function to age manure pools
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAgeResources")]
		private void OnWFAgeResources(object sender, EventArgs e)
		{
			// decay N and DMD of pools and age by 1 month
			foreach (ManureStoreUncollected store in UncollectedStores)
			{
				foreach (ManurePool pool in store.Pools)
				{
					pool.Age++;
					pool.Amount *= DecayRate; 
				}
				store.Pools.RemoveAll(a => a.Age > MaximumAge);
			}
		}

		/// <summary>
		/// Method to collect manure from uncollected manure stores
		/// Manure is collected from freshest to oldest
		/// </summary>
		/// <param name="storeName">Name of store to add manure to</param>
		/// <param name="resourceLimiter">Reduction due to limited resources</param>
		/// <param name="activityName">Name of activity performing collection</param>
		public void Collect(string storeName, double resourceLimiter, string activityName)
		{
			ManureStoreUncollected store = UncollectedStores.Where(a => a.Name.ToLower() == storeName.ToLower()).FirstOrDefault();
			if (store != null)
			{
				double limiter = Math.Min(Math.Max(resourceLimiter, 1.0), 0);
				double amountPossible = store.Pools.Sum(a => a.Amount) * limiter;
				double amountMoved = 0;

				while (store.Pools.Count > 0 && amountMoved<amountPossible)
				{
					// take needed
					double take = Math.Min(amountPossible - amountMoved, store.Pools[0].Amount);
					amountMoved += take;
					store.Pools[0].Amount -= take; 
					// if 0 delete
					store.Pools.RemoveAll(a => a.Amount == 0);
				}
				this.Add(amountMoved, activityName, ((storeName=="")?"Unknown":storeName));
			}
		}
	}

	/// <summary>
	/// Individual store of uncollected manure
	/// </summary>
	public class ManureStoreUncollected
	{
		/// <summary>
		/// Name of store (eg yards, paddock name etc)
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Pools of manure in this store
		/// </summary>
		public List<ManurePool> Pools = new List<ManurePool>();
	}

	/// <summary>
	/// Individual uncollected manure pool to track age and decomposition
	/// </summary>
	public class ManurePool
	{
		/// <summary>
		/// Age of pool (in timesteps)
		/// </summary>
		public int Age { get; set; }
		/// <summary>
		/// Amount (dry weight) in pool
		/// </summary>
		public double  Amount { get; set; }

		/// <summary>
		/// Acluclate wet weight of pool
		/// </summary>
		/// <param name="MoistureDecayRate"></param>
		/// <param name="ProportionMoistureFresh"></param>
		/// <returns></returns>
		public double WetWeight(double MoistureDecayRate, double ProportionMoistureFresh)
		{
			double moisture = ProportionMoistureFresh;
			for (int i = 0; i < Age; i++)
			{
				moisture *= MoistureDecayRate;
			}
			return Amount / moisture;
		}

	}
}
