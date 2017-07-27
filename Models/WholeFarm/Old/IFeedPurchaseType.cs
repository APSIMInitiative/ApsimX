using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>
	/// Properties required for a purchaseable Food Type
	/// </summary>
	public interface IFeedPurchaseType
	{
		/// <summary>
		/// Determine if this feed is purchased as needed
		/// </summary>
		[Description("Purchase as needed")]
		bool PurchaseAsNeeded { get; set; }

		/// <summary>
		/// Weight (kg) per unit purchased
		/// </summary>
		[Description("Weight (kg) per unit purchased")]
		double KgPerUnitPurchased { get; set; }

		/// <summary>
		/// Cost per unit purchased
		/// </summary>
		[Description("Cost per unit purchased")]
		double CostPerUnitPurchased { get; set; }

		/// <summary>
		/// Labour required per unit purchase
		/// </summary>
		[Description("Labour required per unit purchase")]
		double LabourPerUnitPurchased { get; set; }

		/// <summary>
		/// Other costs per unit purchased
		/// </summary>
		[Description("Other costs per unit purchased")]
		double OtherCosts { get; set; }

	}
}
