using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm.Activities
{
	/// <summary>manage enterprise activity</summary>
	/// <summary>This activity undertakes the overheads of running the enterprise.</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	public class FinanceActivityCalculateInterest : WFActivityBase
	{
		/// <summary>
		/// Get the resources.
		/// </summary>
		[Link]
		private ResourcesHolder Resources = null;

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns></returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			return null;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void PerformActivity()
		{
			return;
		}

		/// <summary>An event handler to allow us to make all payments when needed</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("EndOfMonth")]
		private void OnEndOfMonth(object sender, EventArgs e)
		{
			FinanceType bankAccount = Resources.FinanceResource().GetFirst() as FinanceType;
			// make interest payments on bank accounts
			foreach (FinanceType accnt in Resources.FinanceResource().Children.Where(a => a.GetType() == typeof(FinanceType)))
			{
				if(accnt.Balance >0)
				{
					bankAccount.Add(accnt.Balance*accnt.InterestRatePaid/1200, this.Name, "Interest earned");
				}
				else
				{
					ResourceRequest interestRequest = new ResourceRequest();
					interestRequest.ActivityName = this.Name;
					interestRequest.Required = Math.Abs(accnt.Balance) * accnt.InterestRateCharged / 1200;
					interestRequest.AllowTransmutation = false;
					interestRequest.Reason = "Interest charged";
					bankAccount.Remove(interestRequest);
				}
			}
		}

		/// <summary>
		/// res sh
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
