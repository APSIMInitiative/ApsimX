using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>
	/// Comon WholeFarm types
	/// </summary>
	public static class Common
	{
		/// <summary>
		/// Reasons for a change in herd
		/// </summary>
		public enum HerdChangeReason
		{
			/// <summary>
			/// This individual remains in herd
			/// </summary>
			None,
			/// <summary>
			/// Individual died
			/// </summary>
			Died,
			/// <summary>
			/// Individual born
			/// </summary>
			Born,
			/// <summary>
			/// Trade individual sold weight/age
			/// </summary>
			TradeSale,
			/// <summary>
			/// Dry breeder sold
			/// </summary>
			DryBreederSale,
			/// <summary>
			/// Excess breeder sold
			/// </summary>
			ExcessBreederSale,
			/// <summary>
			/// Excess bull sold
			/// </summary>
			ExcessBullSale,
			/// <summary>
			/// Individual reached maximim age and sold
			/// </summary>
			MaxAgeSale,
			/// <summary>
			/// Individual reached sale weight or age
			/// </summary>
			AgeWeightSale,
			/// <summary>
			/// Trade individual purchased
			/// </summary>
			TradePurchase,
			/// <summary>
			/// Heifer purchased
			/// </summary>
			HeiferPurchase,
			/// <summary>
			/// Breeding sire purchased
			/// </summary>
			SirePurchase,
			/// <summary>
			/// Individual consumed by household
			/// </summary>
			Consumed,
			/// <summary>
			/// Destocking sale
			/// </summary>
			DestockSale,
			/// <summary>
			/// Restocking purchase
			/// </summary>
			RestockPurchase,
			/// <summary>
			/// Initial herd
			/// </summary>
			InitialHerd
		}

		/// <summary>
		/// Animal pricing style
		/// </summary>
		public enum PricingStyleType
		{
			/// <summary>
			/// Value per head
			/// </summary>
			perHead,
			/// <summary>
			/// Value per kg live weight
			/// </summary>
			perKg
		}

		/// <summary>
		/// Types of measures of area with value representing the number of hectares
		/// </summary>
		public enum UnitsOfAreaType
		{
			/// <summary>
			/// Hectares
			/// </summary>
			Hectares = 1,
			/// <summary>
			/// Square kilometres
			/// </summary>
			SquareKilometres = 100
		}


	}
}
