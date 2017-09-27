using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm.Activities
{
	/// <summary>Tracking settings for Ruminant purchases and sales</summary>
	/// <summary>If this model is provided within RuminantActivityBuySell, trucking costs and loading rules will occur</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(RuminantActivityBuySell))]
	public class TruckingSettings : WFModel
	{
		/// <summary>
		/// Distance to market
		/// </summary>
		[Description("Distance to market (km)")]
        [Required]
        public double DistanceToMarket { get; set; }

		/// <summary>
		/// Cost of trucking ($/km/truck)
		/// </summary>
		[Description("Cost of trucking ($/km/truck)")]
        [Required]
        public double CostPerKmTrucking { get; set; }

		/// <summary>
		/// Number of 450kg animals per truck load
		/// </summary>
		[Description("Number of 450kg animals per truck load")]
        [Required]
        public double Number450kgPerTruck { get; set; }

		/// <summary>
		/// Minimum number of truck loads before selling (0 continuous sales)
		/// </summary>
		[Description("Minimum number of truck loads before selling (0 continuous sales)")]
        [Required]
        public double MinimumTrucksBeforeSelling { get; set; }

		/// <summary>
		/// Minimum proportion of truck load before selling (0 continuous sales)
		/// </summary>
		[Description("Minimum proportion of truck load before selling (0 continuous sales)")]
        [Required]
        public double MinimumLoadBeforeSelling { get; set; }

        //TODO: add emissions values per km
	}
}
