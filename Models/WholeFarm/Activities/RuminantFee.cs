using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm.Activities
{
	/// <summary>Ruminant herd cost </summary>
	/// <summary>This activity will arrange payment of a herd expense such as vet fees</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(RuminantActivityBuySell))]
	[ValidParent(ParentType = typeof(RuminantActivityBreed))]
	public class RuminantFee: WFModel
	{
		/// <summary>
		/// Payment style
		/// </summary>
		[System.ComponentModel.DefaultValueAttribute(AnimalPaymentStyleType.perHead)]
		[Description("Payment style")]
		public AnimalPaymentStyleType PaymentStyle { get; set; }

		/// <summary>
		/// Amount
		/// </summary>
		[Description("Amount")]
		public double Amount { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public RuminantFee()
		{
			this.SetDefaults();
		}

	}
}
