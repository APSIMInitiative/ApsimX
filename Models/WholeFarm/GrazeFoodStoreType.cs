using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm
{
	/// <summary>
	/// This stores the parameters for a GrazeFoodType and holds values in the store
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(GrazeFoodStore))]
	public class GrazeFoodStoreType : Model, IFeedType
	{
		/// <summary>
		/// List of pools available
		/// </summary>
		[XmlIgnore]
		public List<GrazeFoodStorePool> Pools = new List<GrazeFoodStorePool>();

		/// <summary>
		/// Dry Matter (%)
		/// </summary>
		[Description("Dry Matter (%) NOT USED")]
		public double DryMatter { get; set; }

		/// <summary>
		/// Dry Matter Digestibility (%)
		/// </summary>
		[Description("Dry Matter Digestibility (%) NOT USED")]
		public double DMD { get; set; }

		/// <summary>
		/// Nitrogen (%)
		/// </summary>
		[Description("Initial Nitrogen (%)")]
		public double Nitrogen { get; set; }

		/// <summary>
		/// Starting Amount (kg)
		/// </summary>
		[Description("Starting Amount (kg)")]
		public double StartingAmount { get; set; }

		/// <summary>
		/// Proportion Nitrogen loss each month from pools
		/// </summary>
		[Description("Proportion Nitrogen loss each month from pools")]
		public double DecayNitrogen { get; set; }

		/// <summary>
		/// Minimum Nitrogen %
		/// </summary>
		[Description("Minimum Nitrogen")]
		public double MinimumNitrogen { get; set; }

		/// <summary>
		/// Proportion Dry Matter Digestibility loss each month from pools
		/// </summary>
		[Description("Proportion DMD loss each month from pools")]
		public double DecayDMD { get; set; }

		/// <summary>
		/// Minimum Dry Matter Digestibility
		/// </summary>
		[Description("Minimum Dry Matter Digestibility")]
		public double MinimumDMD { get; set; }

		/// <summary>
		/// Monthly detachment rate
		/// </summary>
		[Description("Detachment rate")]
		public double DetachRate { get; set; }

		/// <summary>
		/// Detachment rate of 12 month or older plants
		/// </summary>
		[Description("Carryover detachment rate")]
		public double CarryoverDetachRate { get; set; }

		/// <summary>
		/// Amount (kg)
		/// </summary>
		[XmlIgnore]
		public double Amount {
			get
			{
				return Pools.Sum(a => a.Amount);
			}
		}

		/// <summary>
		/// Initialise the current state to the starting amount of fodder
		/// </summary>
		public void Initialise()
        {
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Initialise();
        }

		/// <summary>
		/// Add new pasture pool to the list of pools
		/// </summary>
		/// <param name="newpool"></param>
		public void Add(GrazeFoodStorePool newpool)
		{
			Pools.Insert(0, newpool);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="AddAmount"></param>
		/// <param name="ActivityName"></param>
		/// <param name="UserName"></param>
		public void Add(double AddAmount, string ActivityName, string UserName)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="RemoveAmount"></param>
		/// <param name="ActivityName"></param>
		/// <param name="UserName"></param>
		public void Remove(double RemoveAmount, string ActivityName, string UserName)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="RemoveRequest"></param>
		public void Remove(object RemoveRequest)
		{
			// Called if activity removes food directly from GrazeFoodStore (no Feed Arbitrator used)
			// Remove requested amount from all pools as proportion pool amount of whole biomass

			RuminantFeedRequest feedRequest = RemoveRequest as RuminantFeedRequest;
			double totalAvailable = Pools.Sum(a => a.Amount);
			double amountToTake = Math.Min(feedRequest.Amount, totalAvailable);
			foreach (var pool in Pools)
			{
				feedRequest.Amount = amountToTake * (pool.Amount/totalAvailable);
				pool.Remove(feedRequest);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="NewAmount"></param>
		public void Set(double NewAmount)
		{
			throw new NotImplementedException();
		}
	}

}