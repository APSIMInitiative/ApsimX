using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
	[ValidParent(ParentType = typeof(CropActivityTask))]
	public class CropActivityFee: WFModel
	{
		/// <summary>
		/// Payment style
		/// </summary>
		[System.ComponentModel.DefaultValueAttribute(CropPaymentStyleType.perHa)]
		[Description("Payment style")]
        [Required]
        public CropPaymentStyleType PaymentStyle { get; set; }

		/// <summary>
		/// Amount
		/// </summary>
		[Description("Amount")]
        [Required, Range(0, double.MaxValue, ErrorMessage = "Value must be a greter than or equal to 0")]
        public double Amount { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public CropActivityFee()
		{
			this.SetDefaults();
		}

	}
}
