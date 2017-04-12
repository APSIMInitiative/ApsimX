using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm.Activities
{
	/// <summary>Ruminant herd management activity</summary>
	/// <summary>This activity will maintain a breeding herd at the desired levels of age/breeders etc</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class RuminantActivityTrade : WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;

		/// <summary>
		/// Name of herd to trade
		/// </summary>
		[Description("Name of herd to trade")]
		public string HerdName { get; set; }

		/// <summary>
		/// Weight of inividuals to buy
		/// </summary>
		[Description("Weight of inividuals to buy")]
		public double BuyWeight { get; set; }

		/// <summary>
		/// Animal age at purchase (months)
		/// </summary>
		[Description("Animal age at purchase (months)")]
		public int BuyAge { get; set; }

		/// <summary>
		/// Trade price (purchase/sell price /kg LWT)
		/// </summary>
		[Description("trade price (purchase/sell price /kg LWT)")]
		public double TradePrice { get; set; }

		/// <summary>
		/// Months kept before sale
		/// </summary>
		[Description("Months kept before sale")]
		public int MinMonthsKept { get; set; }

		/// <summary>
		/// Weight to achieve before sale
		/// </summary>
		[Description("Weight to achieve before sale")]
		public int TradeWeight { get; set; }

		/// <summary>
		/// Purchase month
		/// </summary>
		[Description("Purchase month")]
		public int PurchaseMonth { get; set; }

		//TODO: devide how many to stock.
		// stocking rate for paddock
		// fixed number




		/// <summary>An event handler to call for all herd management activities</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalManage")]
		private void OnWFAnimalManage(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd.Where(a => a.HerdName == HerdName).ToList();

		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			// check for labour

			return null;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void PerformActivity()
		{
			return; ;
		}

		/// <summary>
		/// Resource shortfall event handler
		/// </summary>
		public override event EventHandler ResourceShortfallOccurred;

		/// <summary>
		/// Shortfall occurred 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnShortfallOccurred(EventArgs e)
		{
			if (ResourceShortfallOccurred != null)
				ResourceShortfallOccurred(this, e);
		}

	}
}
