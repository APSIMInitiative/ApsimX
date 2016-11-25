using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>manage enterprise activity</summary>
	/// <summary>This activity undertakes the overheads of running the enterprise.</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Activities))]
	public class EnterpriseActivityManage : Model
	{
		/// <summary>
		/// Get the Clock.
		/// </summary>
		[Link]
		Clock Clock = null;

		[Link]
		private Resources Resources = null;

		/// <summary>An event handler to allow us to make all payments when needed</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("EndOfMonth")]
		private void OnEndOfMonth(object sender, EventArgs e)
		{
			Finance Accounts = Resources.FinanceResource() as Finance;
			FinanceType bankAccount = Accounts.GetFirst();

			foreach (var overhead in this.Children.Where(a => a.GetType() == typeof(EnterpriseOverhead)).Cast<EnterpriseOverhead>().Where(a => a.NextDueDate.Year == Clock.Today.Year & a.NextDueDate.Month == Clock.Today.Month))
			{
				// make payment
				bankAccount.Remove(overhead.Amount, this.Name, overhead.Name);
				// update next due date
				overhead.NextDueDate = overhead.NextDueDate.AddMonths(overhead.PaymentInterval);
			}

			// make interest payments on bank accounts
			foreach (var item in Resources.FinanceResource().Children)
			{
				if(item.GetType() == typeof(FinanceType))
				{
					FinanceType accnt = item as FinanceType;
					if(accnt.Balance >0)
					{
						bankAccount.Add(accnt.Balance*accnt.InterestRatePaid/1200, this.Name, "InterestPaid");
					}
					else
					{
						bankAccount.Remove(Math.Abs(accnt.Balance) * accnt.InterestRateCharged/1200, this.Name, "InterestCharged");
					}
				}
			}

		}



	}
}
