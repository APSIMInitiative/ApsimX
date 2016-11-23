using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>
	/// Ruuminant valuation entry
	/// </summary>
	public class RuminantValue
	{
		/// <summary>
		/// Name of herd
		/// </summary>
		public string Breed { get; set; }

		/// <summary>
		/// Gender
		/// </summary>
		public Sex Gender { get; set; }

		/// <summary>
		/// Age in months
		/// </summary>
		public double Age { get; set; }

		/// <summary>
		/// Value of individual to buy
		/// </summary>
		public double PurchaseValue { get; set; }

		/// <summary>
		/// Value of individual to sell
		/// </summary>
		public double SellValue { get; set; }

		/// <summary>
		/// Type of Styling
		/// </summary>
		public Common.PricingStyleType Style { get; set; }
	}
}
