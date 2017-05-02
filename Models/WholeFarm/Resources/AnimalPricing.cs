using Models.Core;
using Models.WholeFarm.Activities;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm.Resources
{
	/// <summary>
	/// User entry of Animal prices
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(RuminantActivityBuySell))]
	public class AnimalPricing: Model
	{
		/// <summary>
		/// Style of pricing animals
		/// </summary>
		[Description("Style of pricing animals")]
		public PricingStyleType PricingStyle { get; set; }
	}

	/// <summary>
	/// Individual price entry
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(AnimalPricing))]
	public class AnimalPriceEntry: Model
	{
		/// <summary>
		/// Gender
		/// </summary>
		[Description("Gender")]
		public Sex Gender { get; set; }

		/// <summary>
		/// Age in months
		/// </summary>
		[Description("Age in months")]
		public double Age { get; set; }

		/// <summary>
		/// Purchase value of individual
		/// </summary>
		[Description("Purchase value of individual")]
		public double PurchaseValue { get; set; }

		/// <summary>
		/// Sell value of individual
		/// </summary>
		[Description("Sell value of individual")]
		public double SellValue { get; set; }
	}


}
